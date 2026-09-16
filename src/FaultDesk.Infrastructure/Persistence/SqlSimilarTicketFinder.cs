using FaultDesk.Application.Abstractions;
using FaultDesk.Domain.Tickets;
using Microsoft.EntityFrameworkCore;

namespace FaultDesk.Infrastructure.Persistence;

/// <summary>
/// Nearest-neighbour search done inside SQL Server 2025 with VECTOR_DISTANCE('cosine', ...), restricted to vectors
/// produced by the same embedding model as the source ticket.
/// </summary>
internal sealed class SqlSimilarTicketFinder(IDbContextFactory<FaultDeskDbContext> factory) : ISimilarTicketFinder
{
    public async Task<IReadOnlyList<SimilarTicket>> FindSimilarAsync(TicketId ticketId, int take, CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        var source = await db.TicketEmbeddings.AsNoTracking()
            .SingleOrDefaultAsync(e => e.TicketId == ticketId, cancellationToken);
        if (source is null)
        {
            return [];
        }

        var query = source.Vector;
        var modelId = source.ModelId;

        var nearest = await db.TicketEmbeddings.AsNoTracking()
            .Where(e => e.TicketId != ticketId && e.ModelId == modelId)
            .Select(e => new { e.Ticket, Distance = EF.Functions.VectorDistance("cosine", e.Vector, query) })
            .OrderBy(x => x.Distance)
            .Take(take)
            .ToListAsync(cancellationToken);

        return nearest.Select(x => new SimilarTicket(x.Ticket, x.Distance)).ToList();
    }
}
