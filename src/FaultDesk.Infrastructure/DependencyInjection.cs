using FaultDesk.Application.Abstractions;
using FaultDesk.Infrastructure.Ai;
using FaultDesk.Infrastructure.Persistence;
using FaultDesk.Infrastructure.Persistence.Repositories;
using FaultDesk.Infrastructure.Persistence.Seed;
using FaultDesk.Infrastructure.Vehicles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FaultDesk.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddFaultDeskInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);

        var connectionString = configuration.GetConnectionString("FaultDesk")
            ?? throw new InvalidOperationException("Connection string 'FaultDesk' is not configured.");

        services.AddDbContextFactory<FaultDeskDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(maxRetryCount: 3)));

        services.AddSingleton<IFaultTicketRepository, EfFaultTicketRepository>();
        services.AddSingleton<IInvestigationRepository, EfInvestigationRepository>();
        services.AddSingleton<ITicketEmbeddingStore, EfTicketEmbeddingStore>();
        services.AddSingleton<ISimilarTicketFinder, SqlSimilarTicketFinder>();
        services.AddSingleton<IVehicleLookupService, MockVehicleLookupService>();

        services.AddFaultDeskAi(configuration);

        services.AddSingleton<TicketSeeder>();
        services.AddSingleton<EmbeddingBackfiller>();
        services.AddHostedService<DatabaseInitializer>();

        return services;
    }
}
