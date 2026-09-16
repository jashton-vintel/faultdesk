using System.Security.Cryptography;
using FaultDesk.Domain.Common;
using FaultDesk.Domain.Vehicles;

namespace FaultDesk.Domain.Tickets;

/// <summary>
/// Aggregate root for a customer-reported fault. Owns the lifecycle New -> InProgress -> Resolved,
/// the optional AI triage summary and the workshop's resolution notes.
/// </summary>
public sealed class FaultTicket
{
    public const int MinDescriptionLength = 10;
    public const int MaxDescriptionLength = 4000;

    private const string ReferenceAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private FaultTicket()
    {
    }

    public TicketId Id { get; private set; }

    /// <summary>Customer-facing reference, e.g. FD-260916-K7PQ.</summary>
    public string Reference { get; private set; } = null!;

    public VehicleRegistration Registration { get; private set; } = null!;
    public VehicleDetails Vehicle { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public string? CustomerName { get; private set; }
    public string? CustomerContact { get; private set; }
    public TicketStatus Status { get; private set; }
    public TriageSummary? Triage { get; private set; }
    public Resolution? Resolution { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static FaultTicket Create(
        VehicleRegistration registration,
        VehicleDetails vehicle,
        string? description,
        string? customerName,
        string? customerContact,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(registration);
        ArgumentNullException.ThrowIfNull(vehicle);

        var cleanDescription = description?.Trim() ?? string.Empty;
        if (cleanDescription.Length < MinDescriptionLength)
        {
            throw new DomainException($"Please describe the problem in at least {MinDescriptionLength} characters.");
        }

        if (cleanDescription.Length > MaxDescriptionLength)
        {
            throw new DomainException($"Description must be {MaxDescriptionLength} characters or fewer.");
        }

        return new FaultTicket
        {
            Id = TicketId.New(),
            Reference = GenerateReference(now),
            Registration = registration,
            Vehicle = vehicle,
            Description = cleanDescription,
            CustomerName = Guard.Optional(customerName, "Customer name", 100),
            CustomerContact = Guard.Optional(customerContact, "Customer contact", 200),
            Status = TicketStatus.New,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void ApplyTriage(TriageSummary triage, DateTimeOffset now)
    {
        Triage = triage ?? throw new ArgumentNullException(nameof(triage));
        UpdatedAt = now;
    }

    public void Start(DateTimeOffset now)
    {
        if (Status != TicketStatus.New)
        {
            throw new DomainException($"Only new tickets can be started (ticket is {Status}).");
        }

        Status = TicketStatus.InProgress;
        UpdatedAt = now;
    }

    public void Resolve(string? notes, DateTimeOffset now)
    {
        if (Status == TicketStatus.Resolved)
        {
            throw new DomainException("Ticket is already resolved.");
        }

        Resolution = Resolution.Create(notes, now);
        Status = TicketStatus.Resolved;
        UpdatedAt = now;
    }

    public void Reopen(DateTimeOffset now)
    {
        if (Status != TicketStatus.Resolved)
        {
            throw new DomainException("Only resolved tickets can be reopened.");
        }

        // The previous resolution is kept as history: it tells the workshop what was already tried.
        Status = TicketStatus.InProgress;
        UpdatedAt = now;
    }

    /// <summary>Adviser-facing headline: the triage title when we have one, otherwise the first line of the description.</summary>
    public string Headline
    {
        get
        {
            if (Triage is not null)
            {
                return Triage.Title;
            }

            var firstLine = Description.Split('\n', 2)[0].Trim();
            return firstLine.Length <= 80 ? firstLine : firstLine[..77] + "...";
        }
    }

    private static string GenerateReference(DateTimeOffset now)
    {
        Span<char> suffix = stackalloc char[4];
        for (var i = 0; i < suffix.Length; i++)
        {
            suffix[i] = ReferenceAlphabet[RandomNumberGenerator.GetInt32(ReferenceAlphabet.Length)];
        }

        return $"FD-{now:yyMMdd}-{suffix}";
    }
}
