using FaultDesk.Application.Abstractions;
using FaultDesk.Domain.Diagnostics;
using FaultDesk.Domain.Tickets;
using Microsoft.Extensions.Logging;

namespace FaultDesk.Application.Tickets;

/// <param name="Similarity">0..1 for tickets found by vector search; null for same-vehicle history.</param>
public sealed record RelatedTicket(
    TicketId Id,
    string Reference,
    DateTimeOffset CreatedAt,
    string Registration,
    string Vehicle,
    string Headline,
    TicketStatus Status,
    string? ResolutionNotes,
    double? Similarity)
{
    public static RelatedTicket From(FaultTicket ticket, double? similarity) => new(
        ticket.Id,
        ticket.Reference,
        ticket.CreatedAt,
        ticket.Registration.Display,
        ticket.Vehicle.Summary,
        ticket.Headline,
        ticket.Status,
        ticket.Resolution?.Notes,
        similarity);
}

public sealed record TicketDetail(
    FaultTicket Ticket,
    IReadOnlyList<RelatedTicket> VehicleHistory,
    IReadOnlyList<RelatedTicket> SimilarTickets,
    IReadOnlyList<Investigation> Investigations);

/// <summary>
/// Everything the garage screen needs: the ticket, other tickets for the same vehicle, semantically similar tickets
/// (excluding anything already in the vehicle history) and previous AI investigations.
/// </summary>
public sealed class GetTicketDetailHandler(
    IFaultTicketRepository tickets,
    ISimilarTicketFinder similarTickets,
    IInvestigationRepository investigations,
    ILogger<GetTicketDetailHandler> logger)
{
    public const int SimilarTicketCount = 5;

    public async Task<TicketDetail?> HandleAsync(TicketId id, CancellationToken cancellationToken)
    {
        var ticket = await tickets.GetAsync(id, cancellationToken);
        if (ticket is null)
        {
            return null;
        }

        var history = (await tickets.ListByRegistrationAsync(ticket.Registration, cancellationToken))
            .Where(t => t.Id != id)
            .Select(t => RelatedTicket.From(t, null))
            .ToList();

        var seen = history.Select(h => h.Id).Append(id).ToHashSet();
        var similar = (await TryFindSimilarAsync(id, cancellationToken))
            .Where(s => seen.Add(s.Ticket.Id))
            .Select(s => RelatedTicket.From(s.Ticket, s.Similarity))
            .ToList();

        var previousInvestigations = await investigations.ListForTicketAsync(id, cancellationToken);

        return new TicketDetail(ticket, history, similar, previousInvestigations);
    }

    private async Task<IReadOnlyList<SimilarTicket>> TryFindSimilarAsync(TicketId id, CancellationToken cancellationToken)
    {
        try
        {
            return await similarTickets.FindSimilarAsync(id, SimilarTicketCount, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Similar-ticket search failed for {TicketId}; showing vehicle history only", id);
            return [];
        }
    }
}
