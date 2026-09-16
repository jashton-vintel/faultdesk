namespace FaultDesk.Domain.Tickets;

public readonly record struct TicketId(Guid Value)
{
    public static TicketId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}
