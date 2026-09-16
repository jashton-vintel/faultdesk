using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FaultDesk.Infrastructure.Persistence;

/// <summary>
/// Lets `dotnet ef migrations add` run against this project alone, without booting the web host or needing AI keys.
/// The connection string is only used to build the model; migrations are applied at runtime by <see cref="DatabaseInitializer"/>.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<FaultDeskDbContext>
{
    public FaultDeskDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__FaultDesk")
            ?? "Server=localhost,1433;Database=FaultDesk;User Id=sa;Password=design-time-only;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<FaultDeskDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new FaultDeskDbContext(options);
    }
}
