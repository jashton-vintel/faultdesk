using FaultDesk.Domain.Common;
using FaultDesk.Domain.Tickets;

namespace FaultDesk.Domain.Diagnostics;

/// <summary>One run of "AI Investigate" for a ticket: the markdown report plus its provenance. Re-running creates a new one.</summary>
public sealed class Investigation
{
    private Investigation()
    {
    }

    public Guid Id { get; private set; }
    public TicketId TicketId { get; private set; }
    public string ReportMarkdown { get; private set; } = null!;
    public AiCallMetadata Metadata { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    public static Investigation Create(TicketId ticketId, string? reportMarkdown, AiCallMetadata metadata, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        if (string.IsNullOrWhiteSpace(reportMarkdown))
        {
            throw new DomainException("An investigation must contain a report.");
        }

        return new Investigation
        {
            Id = Guid.CreateVersion7(),
            TicketId = ticketId,
            ReportMarkdown = reportMarkdown.Trim(),
            Metadata = metadata,
            CreatedAt = now,
        };
    }
}
