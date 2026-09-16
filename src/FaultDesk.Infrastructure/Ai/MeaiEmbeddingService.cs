using FaultDesk.Application.Abstractions;
using Microsoft.Extensions.AI;

namespace FaultDesk.Infrastructure.Ai;

/// <summary>Adapts any Microsoft.Extensions.AI embedding generator to the application's port and pins the vector size.</summary>
internal sealed class MeaiEmbeddingService(
    IEmbeddingGenerator<string, Embedding<float>> generator,
    string modelId,
    int dimensions) : IEmbeddingService
{
    public string ModelId => modelId;

    public int Dimensions => dimensions;

    public async Task<ReadOnlyMemory<float>> EmbedAsync(string text, CancellationToken cancellationToken)
    {
        var vectors = await EmbedManyAsync([text], cancellationToken);
        return vectors[0];
    }

    public async Task<IReadOnlyList<ReadOnlyMemory<float>>> EmbedManyAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken)
    {
        if (texts.Count == 0)
        {
            return [];
        }

        var embeddings = await generator.GenerateAsync(
            texts,
            new EmbeddingGenerationOptions { Dimensions = dimensions },
            cancellationToken);

        var vectors = embeddings.Select(e => e.Vector).ToList();
        foreach (var vector in vectors)
        {
            if (vector.Length != dimensions)
            {
                throw new InvalidOperationException(
                    $"Embedding model '{modelId}' returned {vector.Length} dimensions but the database column holds {dimensions}.");
            }
        }

        return vectors;
    }
}
