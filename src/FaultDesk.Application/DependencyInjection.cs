using FaultDesk.Application.Diagnostics;
using FaultDesk.Application.Tickets;
using FaultDesk.Application.Vehicles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FaultDesk.Application;

public static class DependencyInjection
{
    /// <summary>Registers use-case handlers. Ports are implemented and registered by Infrastructure.</summary>
    public static IServiceCollection AddFaultDeskApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddTransient<LookupVehicleHandler>();
        services.AddTransient<SubmitFaultTicketHandler>();
        services.AddTransient<ListTicketsHandler>();
        services.AddTransient<GetTicketDetailHandler>();
        services.AddTransient<TicketWorkflowHandler>();
        services.AddTransient<TicketEmbeddingUpdater>();
        services.AddTransient<InvestigateTicketHandler>();

        return services;
    }
}
