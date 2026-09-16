using FaultDesk.Domain.Tickets;
using FaultDesk.Domain.Vehicles;

namespace FaultDesk.Application.Abstractions;

public interface IFaultTicketRepository
{
    Task AddAsync(FaultTicket ticket, CancellationToken cancellationToken);

    Task UpdateAsync(FaultTicket ticket, CancellationToken cancellationToken);

    Task<FaultTicket?> GetAsync(TicketId id, CancellationToken cancellationToken);

    /// <summary>All tickets, newest first.</summary>
    Task<IReadOnlyList<FaultTicket>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Tickets for the same registration, newest first.</summary>
    Task<IReadOnlyList<FaultTicket>> ListByRegistrationAsync(VehicleRegistration registration, CancellationToken cancellationToken);
}
