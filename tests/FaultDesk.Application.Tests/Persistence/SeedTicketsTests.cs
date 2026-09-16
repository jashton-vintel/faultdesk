using FaultDesk.Domain.Tickets;
using FaultDesk.Infrastructure.Persistence.Seed;

namespace FaultDesk.Application.Tests.Persistence;

public class SeedTicketsTests
{
    [Fact]
    public void Every_seed_builds_a_valid_ticket_with_a_unique_identity()
    {
        var now = TestData.Now;

        var tickets = SeedTickets.All.Select(seed => seed.Build(now)).ToList();

        Assert.Equal(20, tickets.Count);
        Assert.Equal(tickets.Count, tickets.Select(t => t.Id).Distinct().Count());
        Assert.Equal(tickets.Count, tickets.Select(t => t.Reference).Distinct().Count());
        Assert.All(tickets, t => Assert.NotNull(t.Triage));
        Assert.All(tickets, t => Assert.True(t.CreatedAt < now));
        Assert.All(tickets.Where(t => t.Status == TicketStatus.Resolved), t => Assert.NotNull(t.Resolution));
        Assert.All(tickets.Where(t => t.Status != TicketStatus.Resolved), t => Assert.Null(t.Resolution));
        Assert.Contains(tickets, t => t.Status == TicketStatus.Resolved);
        Assert.Contains(tickets, t => t.Status == TicketStatus.InProgress);
        Assert.Contains(tickets, t => t.Status == TicketStatus.New);
    }

    [Fact]
    public void Seed_identities_are_stable_between_runs()
    {
        var first = SeedTickets.All[0].Build(TestData.Now);
        var second = SeedTickets.All[0].Build(TestData.Now.AddDays(3));

        Assert.Equal(first.Id, second.Id);
        Assert.NotEqual(first.Reference, second.Reference);
    }
}
