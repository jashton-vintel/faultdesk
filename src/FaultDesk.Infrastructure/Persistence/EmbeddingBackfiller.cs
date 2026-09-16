using FaultDesk.Application.Abstractions;
using FaultDesk.Application.Tickets;
using FaultDesk.Domain.Tickets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FaultDesk.Infrastructure.Persistence;

/// <summary>
/// Embeds any ticket that has no vector for the configured embedding model: freshly seeded tickets, tickets whose
/// embedding failed at submission, and every ticket after switching provider (Mock to OpenAI, for example).
/// Bounded per start-up so a large backlog cannot delay the app indefinitely.
/// </summary>
internal sealed class EmbeddingBackfiller(
    IDbContextFactory<FaultDeskDbContext> factory,
    IEmbeddingService embeddings,
    ITicketEmbeddingStore store,
    ILogger<EmbeddingBackfiller> logger)
{
    public const int BatchSize = 50;

    public async Task<int> BackfillAsync(CancellationToken cancellationToken)
    {
        var modelId = embeddings.ModelId;
        List<FaultTicket> missing;

        await using (var db = await factory.CreateDbContextAsync(cancellationToken))
        {
            missing = await db.Tickets.AsNoTracking()
                .Where(t => !db.TicketEmbeddings.Any(e => e.TicketId == t.Id && e.ModelId == modelId))
                .OrderBy(t => t.CreatedAt)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);
        }

        if (missing.Count == 0)
        {
            return 0;
        }

        var vectors = await embeddings.EmbedManyAsync(missing.Select(TicketEmbeddingText.Build).ToList(), cancellationToken);
        for (var i = 0; i < missing.Count; i++)
        {
            await store.UpsertAsync(missing[i].Id, modelId, vectors[i], cancellationToken);
        }

        logger.LogInformation("Embedded {Count} ticket(s) with {Model}", missing.Count, modelId);
        return missing.Count;
    }
}
