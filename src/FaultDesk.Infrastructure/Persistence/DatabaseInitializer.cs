using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FaultDesk.Infrastructure.Persistence;

/// <summary>
/// Applies pending migrations at start-up, retrying while the SQL Server container is still coming up.
/// Runs before the web host starts accepting requests.
/// </summary>
internal sealed class DatabaseInitializer(
    IDbContextFactory<FaultDeskDbContext> factory,
    ILogger<DatabaseInitializer> logger) : IHostedService
{
    private const int MaxAttempts = 10;
    private static readonly TimeSpan Delay = TimeSpan.FromSeconds(3);

    public async Task StartAsync(CancellationToken cancellationToken)
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

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
