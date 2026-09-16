using FaultDesk.Application.Abstractions;
using FaultDesk.Domain.Tickets;

namespace FaultDesk.Application.Tickets;

/// <summary>Garage-side status changes. Resolving re-embeds the ticket so the fix becomes searchable for future similar faults.</summary>
public sealed class TicketWorkflowHandler(
    IFaultTicketRepository tickets,
    TicketEmbeddingUpdater embeddingUpdater,
    TimeProvider timeProvider)
{
    public Task<FaultTicket> StartAsync(TicketId id, CancellationToken cancellationToken) =>
        ChangeAsync(id, ticket => ticket.Start(timeProvider.GetUtcNow()), reembed: false, cancellationToken);

    public Task<FaultTicket> ResolveAsync(TicketId id, string? notes, CancellationToken cancellationToken) =>
        ChangeAsync(id, ticket => ticket.Resolve(notes, timeProvider.GetUtcNow()), reembed: true, cancellationToken);

    public Task<FaultTicket> ReopenAsync(TicketId id, CancellationToken cancellationToken) =>
        ChangeAsync(id, ticket => ticket.Reopen(timeProvider.GetUtcNow()), reembed: true, cancellationToken);

    private async Task<FaultTicket> ChangeAsync(TicketId id, Action<FaultTicket> change, bool reembed, CancellationToken cancellationToken)
    {
        var ticket = await tickets.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Ticket {id} was not found.");

        change(ticket);
        await tickets.UpdateAsync(ticket, cancellationToken);

        if (reembed)
        {
            await embeddingUpdater.TryEmbedAsync(ticket, cancellationToken);
        }

        return ticket;
    }
}
