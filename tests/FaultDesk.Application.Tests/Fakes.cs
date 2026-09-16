using FaultDesk.Application.Abstractions;
using FaultDesk.Application.Prompts;
using FaultDesk.Domain.Diagnostics;
using FaultDesk.Domain.Tickets;
using FaultDesk.Domain.Vehicles;

namespace FaultDesk.Application.Tests;

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

internal sealed class FakeTicketRepository : IFaultTicketRepository
{
    public List<FaultTicket> Tickets { get; } = [];
    public int Updates { get; private set; }

    public Task AddAsync(FaultTicket ticket, CancellationToken cancellationToken)
    {
        Tickets.Add(ticket);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(FaultTicket ticket, CancellationToken cancellationToken)
    {
        Updates++;
        return Task.CompletedTask;
    }

    public Task<FaultTicket?> GetAsync(TicketId id, CancellationToken cancellationToken) =>
        Task.FromResult(Tickets.FirstOrDefault(t => t.Id == id));

    public Task<IReadOnlyList<FaultTicket>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<FaultTicket>>(Tickets.OrderByDescending(t => t.CreatedAt).ToList());

    public Task<IReadOnlyList<FaultTicket>> ListByRegistrationAsync(VehicleRegistration registration, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<FaultTicket>>(
            Tickets.Where(t => t.Registration == registration).OrderByDescending(t => t.CreatedAt).ToList());
}

internal sealed class FakeTriageService : IFaultTriageService
{
    public TriageSummary? Result { get; set; }
    public Exception? Throw { get; set; }
    public int Calls { get; private set; }

    public Task<TriageSummary?> TriageAsync(VehicleDetails vehicle, string description, CancellationToken cancellationToken)
    {
        Calls++;
        return Throw is null ? Task.FromResult(Result) : Task.FromException<TriageSummary?>(Throw);
    }
}

internal sealed class FakeEmbeddingService : IEmbeddingService
{
    public string ModelId => "fake-embedding-v1";
    public int Dimensions => 4;
    public List<string> Texts { get; } = [];
    public Exception? Throw { get; set; }

    public Task<ReadOnlyMemory<float>> EmbedAsync(string text, CancellationToken cancellationToken)
    {
        if (Throw is not null)
        {
            return Task.FromException<ReadOnlyMemory<float>>(Throw);
        }

        Texts.Add(text);
        return Task.FromResult<ReadOnlyMemory<float>>(new[] { 1f, 0f, 0f, 0f });
    }

    public async Task<IReadOnlyList<ReadOnlyMemory<float>>> EmbedManyAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken)
    {
        var results = new List<ReadOnlyMemory<float>>();
        foreach (var text in texts)
        {
            results.Add(await EmbedAsync(text, cancellationToken));
        }

        return results;
    }
}

internal sealed class FakeEmbeddingStore : ITicketEmbeddingStore
{
    public List<(TicketId TicketId, string ModelId, ReadOnlyMemory<float> Vector)> Upserts { get; } = [];

    public Task UpsertAsync(TicketId ticketId, string modelId, ReadOnlyMemory<float> vector, CancellationToken cancellationToken)
    {
        Upserts.Add((ticketId, modelId, vector));
        return Task.CompletedTask;
    }
}

internal sealed class FakeSimilarTicketFinder : ISimilarTicketFinder
{
    public List<SimilarTicket> Results { get; } = [];
    public Exception? Throw { get; set; }

    public Task<IReadOnlyList<SimilarTicket>> FindSimilarAsync(TicketId ticketId, int take, CancellationToken cancellationToken) =>
        Throw is null
            ? Task.FromResult<IReadOnlyList<SimilarTicket>>(Results.Take(take).ToList())
            : Task.FromException<IReadOnlyList<SimilarTicket>>(Throw);
}

internal sealed class FakeInvestigationRepository : IInvestigationRepository
{
    public List<Investigation> Investigations { get; } = [];

    public Task AddAsync(Investigation investigation, CancellationToken cancellationToken)
    {
        Investigations.Add(investigation);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Investigation>> ListForTicketAsync(TicketId ticketId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Investigation>>(
            Investigations.Where(i => i.TicketId == ticketId).OrderByDescending(i => i.CreatedAt).ToList());
}

internal sealed class FakeDiagnosticsAssistant(params string[] chunks) : IDiagnosticsAssistant
{
    public AiPrompt? LastPrompt { get; private set; }

    public async IAsyncEnumerable<DiagnosticsUpdate> InvestigateAsync(
        AiPrompt prompt,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        LastPrompt = prompt;
        foreach (var chunk in chunks)
        {
            await Task.Yield();
            yield return DiagnosticsUpdate.Text(chunk);
        }

        yield return DiagnosticsUpdate.Done(new AiCallMetadata("fake", "fake-model", prompt.Version, 10, 20, TimeSpan.FromSeconds(1), false));
    }
}

internal static class TestData
{
    public static readonly DateTimeOffset Now = new(2026, 9, 16, 10, 0, 0, TimeSpan.Zero);

    public static VehicleDetails Focus() =>
        VehicleDetails.Create("Ford", "Focus", "1.0 EcoBoost", 2018, 999, FuelType.Petrol, currentYear: 2026);

    public static FaultTicket Ticket(string registration = "AB12 CDE", string description = "Rattling noise from the front over bumps.", DateTimeOffset? createdAt = null) =>
        FaultTicket.Create(VehicleRegistration.Parse(registration), Focus(), description, null, null, createdAt ?? Now);

    public static TriageSummary Triage() => TriageSummary.Create(
        "Front suspension knock over bumps",
        FaultCategory.SteeringSuspension,
        Severity.Medium,
        SafeToDrive.Caution,
        ["knocking over bumps"],
        ["drop links"],
        ["Does it change when turning?"],
        "A knocking noise from the front when driving over bumps.");
}
