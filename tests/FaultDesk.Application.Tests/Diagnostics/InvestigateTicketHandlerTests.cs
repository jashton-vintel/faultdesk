using FaultDesk.Application.Diagnostics;
using FaultDesk.Application.Tickets;
using FaultDesk.Domain.Tickets;
using Microsoft.Extensions.Logging.Abstractions;

namespace FaultDesk.Application.Tests.Diagnostics;

public class InvestigateTicketHandlerTests
{
    private readonly FakeTicketRepository _tickets = new();
    private readonly FakeSimilarTicketFinder _similar = new();
    private readonly FakeInvestigationRepository _investigations = new();

    private InvestigateTicketHandler CreateHandler(FakeDiagnosticsAssistant assistant) => new(
        new GetTicketDetailHandler(_tickets, _similar, _investigations, NullLogger<GetTicketDetailHandler>.Instance),
        assistant,
        _investigations,
        new FixedTimeProvider(TestData.Now),
        NullLogger<InvestigateTicketHandler>.Instance);

    [Fact]
    public async Task Streams_the_report_then_persists_it_with_metadata()
    {
        var ticket = TestData.Ticket();
        _tickets.Tickets.Add(ticket);
        var assistant = new FakeDiagnosticsAssistant("## Summary\n", "Likely drop links.\n");

        var updates = await CreateHandler(assistant).HandleAsync(ticket.Id, CancellationToken.None).ToListAsync();

        Assert.Equal(["## Summary\n", "Likely drop links.\n"], updates.Take(2).Select(u => u.TextDelta));
        var final = updates.Last();
        Assert.Null(final.TextDelta);
        Assert.NotNull(final.Completed);
        Assert.Equal("## Summary\nLikely drop links.", final.Completed.ReportMarkdown);
        Assert.Equal("investigate-v1", final.Completed.Metadata.PromptVersion);
        Assert.Equal(TestData.Now, final.Completed.CreatedAt);
        Assert.Same(final.Completed, Assert.Single(_investigations.Investigations));
        Assert.Contains("Rattling noise from the front over bumps.", assistant.LastPrompt!.User);
    }

    [Fact]
    public async Task Unknown_ticket_fails_fast()
    {
        var handler = CreateHandler(new FakeDiagnosticsAssistant("x"));

        await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
        {
            await foreach (var _ in handler.HandleAsync(TicketId.New(), CancellationToken.None))
            {
            }
        });
    }
}
