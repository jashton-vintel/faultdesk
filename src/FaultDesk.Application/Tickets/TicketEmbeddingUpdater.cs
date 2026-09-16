using FaultDesk.Application.Abstractions;
using FaultDesk.Domain.Tickets;
using Microsoft.Extensions.Logging;

namespace FaultDesk.Application.Tickets;

/// <summary>Embeds a ticket and stores the vector. Best-effort: failures are logged, never thrown, and can be backfilled later.</summary>
public sealed class TicketEmbeddingUpdater(
    IEmbeddingService embeddings,
    ITicketEmbeddingStore store,
    ILogger<TicketEmbeddingUpdater> logger)
{
    public async Task<bool> TryEmbedAsync(FaultTicket ticket, CancellationToken cancellationToken)
    {
        try
        {
            var vector = await embeddings.EmbedAsync(TicketEmbeddingText.Build(ticket), cancellationToken);
            await store.UpsertAsync(ticket.Id, embeddings.ModelId, vector, cancellationToken);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Embedding failed for ticket {Reference}; it will be backfilled on the next start-up", ticket.Reference);
            return false;
        }
    }
}
