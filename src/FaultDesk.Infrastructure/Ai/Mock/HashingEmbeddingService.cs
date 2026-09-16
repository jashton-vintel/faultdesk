using FaultDesk.Application.Abstractions;
using FaultDesk.Infrastructure.Persistence.Entities;

namespace FaultDesk.Infrastructure.Ai.Mock;

/// <summary>
/// Zero-key embeddings for demo mode: a hashed bag of words and bigrams, L2-normalised into the same 1536-dimension
/// space the real column uses. Tickets that share vocabulary end up close together, which is enough to demonstrate
/// vector search end-to-end. Not semantic: "car won't start" and "engine does not turn over" are far apart here.
/// </summary>
internal sealed class HashingEmbeddingService : IEmbeddingService
{
    public const string Model = "mock-hashing-v1";

    private static readonly HashSet<string> StopWords =
    [
        "the", "and", "but", "for", "with", "from", "this", "that", "there", "then", "when", "what", "have", "has",
        "had", "been", "was", "were", "are", "its", "into", "onto", "also", "just", "very", "really", "car", "vehicle",
        "seems", "still", "some", "any", "get", "got", "gets", "like", "out", "over", "under", "about", "after", "before",
    ];

    public string ModelId => Model;

    public int Dimensions => TicketEmbedding.Dimensions;

    public Task<ReadOnlyMemory<float>> EmbedAsync(string text, CancellationToken cancellationToken) =>
        Task.FromResult(Embed(text));

    public Task<IReadOnlyList<ReadOnlyMemory<float>>> EmbedManyAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ReadOnlyMemory<float>>>(texts.Select(Embed).ToList());

    public static ReadOnlyMemory<float> Embed(string text)
    {
        var vector = new float[TicketEmbedding.Dimensions];
        var tokens = Tokenise(text);

        foreach (var token in tokens)
        {
            Add(vector, token, 1f);
        }

        for (var i = 0; i < tokens.Count - 1; i++)
        {
            Add(vector, tokens[i] + " " + tokens[i + 1], 0.5f);
        }

        var norm = MathF.Sqrt(vector.Sum(v => v * v));
        if (norm == 0)
        {
            vector[0] = 1f;
            return vector;
        }

        for (var i = 0; i < vector.Length; i++)
        {
            vector[i] /= norm;
        }

        return vector;
    }

    private static void Add(float[] vector, string token, float weight)
    {
        var hash = Fnv1a(token);
        var bucket = (int)(hash % (uint)vector.Length);
        var sign = (hash & 0x8000_0000u) == 0 ? 1f : -1f;
        vector[bucket] += sign * weight;
    }

    internal static List<string> Tokenise(string text)
    {
        var tokens = new List<string>();
        foreach (var raw in text.ToLowerInvariant().Split(' ', '\n', '\r', '\t', ',', '.', ';', ':', '!', '?', '(', ')', '"', '/', '-'))
        {
            var word = new string(raw.Where(char.IsLetterOrDigit).ToArray());
            if (word.Length < 3 || StopWords.Contains(word))
            {
                continue;
            }

            tokens.Add(Stem(word));
        }

        return tokens;
    }

    private static string Stem(string word)
    {
        if (word.Length > 5 && word.EndsWith("ing", StringComparison.Ordinal))
        {
            return word[..^3];
        }

        if (word.Length > 4 && word.EndsWith("es", StringComparison.Ordinal))
        {
            return word[..^2];
        }

        if (word.Length > 3 && word.EndsWith('s'))
        {
            return word[..^1];
        }

        return word;
    }

    private static uint Fnv1a(string value)
    {
        var hash = 2166136261u;
        foreach (var c in value)
        {
            hash ^= c;
            hash *= 16777619u;
        }

        return hash;
    }
}
