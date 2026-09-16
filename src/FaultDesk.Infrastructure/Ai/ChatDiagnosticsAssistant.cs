using System.Diagnostics;
using System.Runtime.CompilerServices;
using FaultDesk.Application.Abstractions;
using FaultDesk.Application.Prompts;
using FaultDesk.Domain.Diagnostics;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace FaultDesk.Infrastructure.Ai;

/// <summary>
/// Streams the "AI Investigate" report. Asks the provider to use its hosted web search so the plan is grounded in
/// faults other people have reported for the same vehicle; if the provider rejects hosted tools the call is retried
/// without them and the result is flagged so the UI can say so.
/// </summary>
internal sealed class ChatDiagnosticsAssistant(IChatClient chat, AiOptions options, ILogger<ChatDiagnosticsAssistant> logger) : IDiagnosticsAssistant
{
    public async IAsyncEnumerable<DiagnosticsUpdate> InvestigateAsync(
        AiPrompt prompt,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var messages = StructuredOutput.ToMessages(prompt);
        var webSearch = !options.IsMockChat;
        var chatOptions = new ChatOptions
        {
            ModelId = options.ChatModel,
            MaxOutputTokens = options.InvestigateMaxOutputTokens,
            Tools = webSearch ? [new HostedWebSearchTool()] : null,
        };

        var stopwatch = Stopwatch.StartNew();
        IAsyncEnumerator<ChatResponseUpdate> updates;
        bool hasNext;

        // The first MoveNext is where an adapter that does not support hosted tools fails, so retry without them there.
        try
        {
            updates = chat.GetStreamingResponseAsync(messages, chatOptions, cancellationToken).GetAsyncEnumerator(cancellationToken);
            hasNext = await updates.MoveNextAsync();
        }
        catch (Exception ex) when (webSearch && ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Provider rejected the hosted web search tool; investigating without web search");
            webSearch = false;
            chatOptions.Tools = null;
            updates = chat.GetStreamingResponseAsync(messages, chatOptions, cancellationToken).GetAsyncEnumerator(cancellationToken);
            hasNext = await updates.MoveNextAsync();
        }

        long? inputTokens = null;
        long? outputTokens = null;
        string? modelId = null;

        try
        {
            while (hasNext)
            {
                var update = updates.Current;
                modelId ??= update.ModelId;

                foreach (var content in update.Contents)
                {
                    switch (content)
                    {
                        case TextContent { Text.Length: > 0 } text:
                            yield return DiagnosticsUpdate.Text(text.Text);
                            break;
                        case UsageContent usage:
                            // Providers report usage incrementally or repeatedly; keep the largest figure seen.
                            inputTokens = Max(inputTokens, usage.Details.InputTokenCount);
                            outputTokens = Max(outputTokens, usage.Details.OutputTokenCount);
                            break;
                    }
                }

                hasNext = await updates.MoveNextAsync();
            }
        }
        finally
        {
            await updates.DisposeAsync();
        }

        stopwatch.Stop();
        yield return DiagnosticsUpdate.Done(new AiCallMetadata(
            options.ChatProvider,
            modelId ?? options.ChatModel,
            prompt.Version,
            (int?)inputTokens,
            (int?)outputTokens,
            stopwatch.Elapsed,
            webSearch));
    }

    private static long? Max(long? current, long? candidate) =>
        candidate is null ? current : current is null ? candidate : Math.Max(current.Value, candidate.Value);
}
