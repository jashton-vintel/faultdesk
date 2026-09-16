using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FaultDesk.Infrastructure.Persistence.Seed;

/// <summary>Inserts the historical seed tickets once. Idempotent: skipped if any seed identity already exists.</summary>
internal sealed class TicketSeeder(
    IDbContextFactory<FaultDeskDbContext> factory,
    TimeProvider timeProvider,
    ILogger<TicketSeeder> logger)
{
    public async Task<int> SeedAsync(CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        var existingIds = (await db.Tickets.Select(t => t.Id).ToListAsync(cancellationToken)).ToHashSet();
        if (SeedTickets.All.Any(seed => existingIds.Contains(seed.Id)))
        {
            return 0;
        }

        var now = timeProvider.GetUtcNow();
        foreach (var seed in SeedTickets.All)
        {
            db.Tickets.Add(seed.Build(now));
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded {Count} historical tickets", SeedTickets.All.Count);
        return SeedTickets.All.Count;
    }
}
