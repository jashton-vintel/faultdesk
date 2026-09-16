using FaultDesk.Application.Tickets;
using FaultDesk.Domain.Common;
using FaultDesk.Domain.Tickets;
using FaultDesk.Domain.Vehicles;
using Microsoft.Extensions.Logging.Abstractions;

namespace FaultDesk.Application.Tests.Tickets;

public class SubmitFaultTicketHandlerTests
{
    private readonly FakeTicketRepository _tickets = new();
    private readonly FakeTriageService _triage = new();
    private readonly FakeEmbeddingService _embeddings = new();
    private readonly FakeEmbeddingStore _embeddingStore = new();

    private SubmitFaultTicketHandler CreateHandler() => new(
        _tickets,
        _triage,
        new TicketEmbeddingUpdater(_embeddings, _embeddingStore, NullLogger<TicketEmbeddingUpdater>.Instance),
        new FixedTimeProvider(TestData.Now),
        NullLogger<SubmitFaultTicketHandler>.Instance);

    private static SubmitFaultTicketCommand Command(string registration = "ab12 cde", string description = "Rattling noise from the front over bumps.") =>
        new(registration, new VehicleInput("Ford", "Focus", "1.0 EcoBoost", 2018, 999, FuelType.Petrol), description, "Sam Patel", "07700 900123");

    [Fact]
    public async Task Submits_a_triaged_ticket_and_embeds_it()
    {
        _triage.Result = TestData.Triage();

        var result = await CreateHandler().HandleAsync(Command(), CancellationToken.None);

        var stored = Assert.Single(_tickets.Tickets);
        Assert.Equal(stored.Id, result.TicketId);
        Assert.Equal("AB12CDE", stored.Registration.Value);
        Assert.Equal(TicketStatus.New, stored.Status);
        Assert.Equal(TestData.Now, stored.CreatedAt);
        Assert.Same(_triage.Result, stored.Triage);
        Assert.Same(_triage.Result, result.Triage);
        Assert.StartsWith("FD-260916-", result.Reference);

        var upsert = Assert.Single(_embeddingStore.Upserts);
        Assert.Equal(stored.Id, upsert.TicketId);
        Assert.Equal("fake-embedding-v1", upsert.ModelId);
        var embeddedText = Assert.Single(_embeddings.Texts);
        Assert.Contains("Front suspension knock over bumps", embeddedText);
        Assert.Contains("Rattling noise from the front over bumps.", embeddedText);
    }

    [Fact]
    public async Task Triage_failure_does_not_block_the_submission()
    {
        _triage.Throw = new HttpRequestException("model unavailable");

        var result = await CreateHandler().HandleAsync(Command(), CancellationToken.None);

        var stored = Assert.Single(_tickets.Tickets);
        Assert.Null(stored.Triage);
        Assert.Null(result.Triage);
        Assert.Single(_embeddingStore.Upserts);
    }

    [Fact]
    public async Task Embedding_failure_does_not_block_the_submission()
    {
        _embeddings.Throw = new HttpRequestException("embedding unavailable");

        await CreateHandler().HandleAsync(Command(), CancellationToken.None);

        Assert.Single(_tickets.Tickets);
        Assert.Empty(_embeddingStore.Upserts);
    }

    [Theory]
    [InlineData("", "Rattling noise from the front over bumps.")]
    [InlineData("AB12 CDE", "short")]
    public async Task Invalid_input_is_rejected_before_anything_is_stored(string registration, string description)
    {
        await Assert.ThrowsAsync<DomainException>(() => CreateHandler().HandleAsync(Command(registration, description), CancellationToken.None));

        Assert.Empty(_tickets.Tickets);
        Assert.Equal(0, _triage.Calls);
        Assert.Empty(_embeddingStore.Upserts);
    }
}
