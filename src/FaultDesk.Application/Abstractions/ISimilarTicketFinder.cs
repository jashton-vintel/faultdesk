using FaultDesk.Domain.Tickets;

namespace FaultDesk.Application.Abstractions;

public interface ISimilarTicketFinder
{
    /// <summary>Tickets whose embedding is closest to the given ticket's, nearest first. Never includes the ticket itself.</summary>
    Task<IReadOnlyList<SimilarTicket>> FindSimilarAsync(TicketId ticketId, int take, CancellationToken cancellationToken);
}

/// <param name="Distance">Cosine distance in [0, 2]; 0 means identical.</param>
public sealed record SimilarTicket(FaultTicket Ticket, double Distance)
{
    public double Similarity => Math.Clamp(1 - Distance, 0, 1);
}
