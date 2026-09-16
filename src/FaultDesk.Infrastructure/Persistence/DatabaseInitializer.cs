using FaultDesk.Infrastructure.Persistence.Seed;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FaultDesk.Infrastructure.Persistence;

/// <summary>
/// At start-up: apply pending migrations (retrying while the SQL Server container is still coming up), seed the
/// historical tickets once, then embed anything that lacks a vector for the configured model.
/// Runs before the web host starts accepting requests. Seeding and embedding are best-effort.
/// </summary>
internal sealed class DatabaseInitializer(
    IDbContextFactory<FaultDeskDbContext> factory,
    TicketSeeder seeder,
    EmbeddingBackfiller backfiller,
    ILogger<DatabaseInitializer> logger) : IHostedService
{
    private const int MaxAttempts = 10;
    private static readonly TimeSpan Delay = TimeSpan.FromSeconds(3);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await MigrateWithRetryAsync(cancellationToken);

        try
        {
            await seeder.SeedAsync(cancellationToken);
            await backfiller.BackfillAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Seeding or embedding backfill failed; the application will still start");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task MigrateWithRetryAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var db = await factory.CreateDbContextAsync(cancellationToken);
                await db.Database.MigrateAsync(cancellationToken);
                logger.LogInformation("Database is up to date");
                return;
            }
            catch (SqlException ex) when (attempt < MaxAttempts)
            {
                logger.LogWarning("Database not ready (attempt {Attempt}/{Max}): {Message}", attempt, MaxAttempts, ex.Message);
                await Task.Delay(Delay, cancellationToken);
            }
        }
    }
}
