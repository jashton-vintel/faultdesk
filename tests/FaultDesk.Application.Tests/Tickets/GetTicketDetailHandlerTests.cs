using FaultDesk.Application.Abstractions;
using FaultDesk.Application.Tickets;
using FaultDesk.Domain.Tickets;
using Microsoft.Extensions.Logging.Abstractions;

namespace FaultDesk.Application.Tests.Tickets;

public class GetTicketDetailHandlerTests
{
    private readonly FakeTicketRepository _tickets = new();
    private readonly FakeSimilarTicketFinder _similar = new();
    private readonly FakeInvestigationRepository _investigations = new();

    private GetTicketDetailHandler CreateHandler() =>
        new(_tickets, _similar, _investigations, NullLogger<GetTicketDetailHandler>.Instance);

    [Fact]
    public async Task Returns_null_for_an_unknown_ticket()
    {
        Assert.Null(await CreateHandler().HandleAsync(TicketId.New(), CancellationToken.None));
    }

    [Fact]
    public async Task Merges_vehicle_history_and_similar_tickets_without_duplicates()
    {
        var ticket = TestData.Ticket();
        var earlierSameCar = TestData.Ticket(description: "Squeal from the front brakes when stopping.", createdAt: TestData.Now.AddMonths(-3));
        earlierSameCar.Resolve("Front pads and discs replaced.", TestData.Now.AddMonths(-3).AddDays(1));
        var otherCar = TestData.Ticket(registration: "XY19 ZZZ", description: "Knocking noise over speed bumps at the front.", createdAt: TestData.Now.AddMonths(-1));
        _tickets.Tickets.AddRange([ticket, earlierSameCar, otherCar]);

        _similar.Results.AddRange(
        [
            new SimilarTicket(ticket, 0.0),
            new SimilarTicket(otherCar, 0.15),
            new SimilarTicket(earlierSameCar, 0.40),
        ]);

        var detail = await CreateHandler().HandleAsync(ticket.Id, CancellationToken.None);

        Assert.NotNull(detail);
        Assert.Same(ticket, detail.Ticket);

        var history = Assert.Single(detail.VehicleHistory);
        Assert.Equal(earlierSameCar.Id, history.Id);
        Assert.Equal("Front pads and discs replaced.", history.ResolutionNotes);
        Assert.Null(history.Similarity);

        var similar = Assert.Single(detail.SimilarTickets);
        Assert.Equal(otherCar.Id, similar.Id);
        Assert.Equal(0.85, similar.Similarity!.Value, precision: 6);
    }

    [Fact]
    public async Task Similarity_search_failure_still_returns_the_ticket_and_history()
    {
        var ticket = TestData.Ticket();
        _tickets.Tickets.Add(ticket);
        _similar.Throw = new InvalidOperationException("vector column missing");

        var detail = await CreateHandler().HandleAsync(ticket.Id, CancellationToken.None);

        Assert.NotNull(detail);
        Assert.Empty(detail.SimilarTickets);
        Assert.Empty(detail.VehicleHistory);
    }
}
