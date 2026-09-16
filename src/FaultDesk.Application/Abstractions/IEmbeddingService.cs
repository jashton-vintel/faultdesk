namespace FaultDesk.Application.Abstractions;

/// <summary>Turns text into a vector so tickets can be compared by meaning.</summary>
public interface IEmbeddingService
{
    /// <summary>Identifies the model (and therefore the vector space) the vectors belong to.</summary>
    string ModelId { get; }

    int Dimensions { get; }

    Task<ReadOnlyMemory<float>> EmbedAsync(string text, CancellationToken cancellationToken);

    Task<IReadOnlyList<ReadOnlyMemory<float>>> EmbedManyAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken);
}
