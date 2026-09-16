using FaultDesk.Application.Abstractions;
using FaultDesk.Domain.Tickets;
using FaultDesk.Infrastructure.Persistence.Entities;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;

namespace FaultDesk.Infrastructure.Persistence.Repositories;

internal sealed class EfTicketEmbeddingStore(IDbContextFactory<FaultDeskDbContext> factory, TimeProvider timeProvider) : ITicketEmbeddingStore
{
    public async Task UpsertAsync(TicketId ticketId, string modelId, ReadOnlyMemory<float> vector, CancellationToken cancellationToken)
    {
        if (vector.Length != TicketEmbedding.Dimensions)
        {
            throw new InvalidOperationException(
                $"Embedding for ticket {ticketId} has {vector.Length} dimensions; the TicketEmbeddings column holds {TicketEmbedding.Dimensions}.");
        }

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var existing = await db.TicketEmbeddings.SingleOrDefaultAsync(e => e.TicketId == ticketId, cancellationToken);

        if (existing is null)
        {
            db.TicketEmbeddings.Add(new TicketEmbedding
            {
                TicketId = ticketId,
                ModelId = modelId,
                Vector = new SqlVector<float>(vector),
                CreatedAt = timeProvider.GetUtcNow(),
            });
        }
        else
        {
            existing.ModelId = modelId;
            existing.Vector = new SqlVector<float>(vector);
            existing.CreatedAt = timeProvider.GetUtcNow();
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
