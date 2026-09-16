using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace FaultDesk.Infrastructure.Ai.Mock;

/// <summary>
/// Zero-key <see cref="IChatClient"/> for demo mode. Recognises which FaultDesk prompt it has been given from the
/// system message and answers with canned content in the same shape as the real model, streamed in small chunks
/// so the UI behaves as it would with a real provider.
/// </summary>
internal sealed class MockChatClient : IChatClient
{
    public const string ModelId = "mock-garage-assistant";

    private static readonly ChatClientMetadata Metadata = new("Mock", defaultModelId: ModelId);
    private static readonly TimeSpan ChunkDelay = TimeSpan.FromMilliseconds(30);

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var text = Answer(messages);
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, text))
        {
            ModelId = ModelId,
            Usage = Usage(messages, text),
        };

        return Task.FromResult(response);
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var text = Answer(messages);

        foreach (var chunk in Chunk(text))
        {
            await Task.Delay(ChunkDelay, cancellationToken);
            yield return new ChatResponseUpdate(ChatRole.Assistant, chunk) { ModelId = ModelId };
        }

        yield return new ChatResponseUpdate(ChatRole.Assistant, [new UsageContent(Usage(messages, text))]) { ModelId = ModelId };
    }

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceKey is null && serviceType == typeof(ChatClientMetadata) ? Metadata
        : serviceKey is null && serviceType.IsInstanceOfType(this) ? this
        : null;

    public void Dispose()
    {
    }

    private static string Answer(IEnumerable<ChatMessage> messages)
    {
        var list = messages.ToList();
        var system = string.Join("\n", list.Where(m => m.Role == ChatRole.System).Select(m => m.Text));
        var user = string.Join("\n", list.Where(m => m.Role == ChatRole.User).Select(m => m.Text));

        if (system.Contains("triage assistant", StringComparison.OrdinalIgnoreCase))
        {
            return MockAnswers.Triage(user);
        }

        if (system.Contains("booking assistant", StringComparison.OrdinalIgnoreCase))
        {
            return MockAnswers.Clarify(user);
        }

        return MockAnswers.Investigate(user);
    }

    private static IEnumerable<string> Chunk(string text)
    {
        var words = text.Split(' ');
        for (var i = 0; i < words.Length; i += 4)
        {
            var slice = words.Skip(i).Take(4);
            yield return string.Join(' ', slice) + (i + 4 < words.Length ? " " : string.Empty);
        }
    }

    private static UsageDetails Usage(IEnumerable<ChatMessage> messages, string answer) => new()
    {
        InputTokenCount = messages.Sum(m => m.Text.Length) / 4,
        OutputTokenCount = answer.Length / 4,
    };
}
