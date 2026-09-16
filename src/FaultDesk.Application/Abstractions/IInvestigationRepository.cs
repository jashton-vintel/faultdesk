using FaultDesk.Domain.Diagnostics;
using FaultDesk.Domain.Tickets;

namespace FaultDesk.Application.Abstractions;

public interface IInvestigationRepository
{
    Task AddAsync(Investigation investigation, CancellationToken cancellationToken);

    /// <summary>Investigations for a ticket, newest first.</summary>
    Task<IReadOnlyList<Investigation>> ListForTicketAsync(TicketId ticketId, CancellationToken cancellationToken);
}
