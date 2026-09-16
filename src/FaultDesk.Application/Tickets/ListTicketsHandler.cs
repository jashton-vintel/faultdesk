using FaultDesk.Application.Abstractions;
using FaultDesk.Domain.Tickets;

namespace FaultDesk.Application.Tickets;

public sealed record TicketSummary(
    TicketId Id,
    string Reference,
    DateTimeOffset CreatedAt,
    string Registration,
    string Vehicle,
    string Headline,
    FaultCategory Category,
    Severity Severity,
    SafeToDrive SafeToDrive,
    TicketStatus Status)
{
    public static TicketSummary From(FaultTicket ticket) => new(
        ticket.Id,
        ticket.Reference,
        ticket.CreatedAt,
        ticket.Registration.Display,
        ticket.Vehicle.Summary,
        ticket.Headline,
        ticket.Triage?.Category ?? FaultCategory.Unknown,
        ticket.Triage?.Severity ?? Severity.Unknown,
        ticket.Triage?.SafeToDrive ?? SafeToDrive.Unknown,
        ticket.Status);
}

public sealed class ListTicketsHandler(IFaultTicketRepository tickets)
{
    public async Task<IReadOnlyList<TicketSummary>> HandleAsync(CancellationToken cancellationToken)
    {
        var all = await tickets.ListAsync(cancellationToken);
        return all.Select(TicketSummary.From).ToList();
    }
}
