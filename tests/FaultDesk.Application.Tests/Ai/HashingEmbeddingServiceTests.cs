using FaultDesk.Infrastructure.Ai.Mock;

namespace FaultDesk.Application.Tests.Ai;

public class HashingEmbeddingServiceTests
{
    [Fact]
    public void Produces_a_deterministic_unit_vector_of_the_column_size()
    {
        var first = HashingEmbeddingService.Embed("Knocking noise from the front suspension over bumps");
        var second = HashingEmbeddingService.Embed("Knocking noise from the front suspension over bumps");

        Assert.Equal(1536, first.Length);
        Assert.Equal(first.ToArray(), second.ToArray());
        Assert.Equal(1.0, Math.Sqrt(first.ToArray().Sum(v => (double)v * v)), precision: 4);
    }

    [Fact]
    public void Texts_that_share_vocabulary_are_closer_than_unrelated_texts()
    {
        var source = HashingEmbeddingService.Embed("Knocking noise from the front suspension when driving over speed bumps");
        var related = HashingEmbeddingService.Embed("Front suspension knocks over bumps and rough roads");
        var unrelated = HashingEmbeddingService.Embed("Air conditioning blows warm air and the compressor does not engage");

        Assert.True(Cosine(source, related) > Cosine(source, unrelated));
        Assert.True(Cosine(source, related) > 0.2);
    }

    [Fact]
    public void Empty_text_still_yields_a_valid_vector()
    {
        var vector = HashingEmbeddingService.Embed("   ");

        Assert.Equal(1536, vector.Length);
        Assert.Equal(1f, vector.Span[0]);
    }

    private static double Cosine(ReadOnlyMemory<float> a, ReadOnlyMemory<float> b)
    {
        var x = a.Span;
        var y = b.Span;
        double dot = 0;
        for (var i = 0; i < x.Length; i++)
        {
            dot += x[i] * y[i];
        }

        return dot;
    }
}
