using FaultDesk.Application.Prompts;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace FaultDesk.Infrastructure.Ai;

internal static class StructuredOutput
{
    /// <summary>
    /// Asks for a JSON object matching <typeparamref name="T"/>. Tries the provider's native JSON-schema mode first and
    /// falls back to describing the schema in the prompt for providers that reject it. Returns null if the model's
    /// answer could not be parsed; callers treat that as "no result", never as an error the user sees.
    /// </summary>
    public static async Task<T?> GetAsync<T>(
        IChatClient chat,
        AiPrompt prompt,
        ChatOptions options,
        ILogger logger,
        CancellationToken cancellationToken)
        where T : class
    {
        var messages = ToMessages(prompt);

        try
        {
            var response = await chat.GetResponseAsync<T>(messages, options, useJsonSchemaResponseFormat: true, cancellationToken: cancellationToken);
            return response.TryGetResult(out var result) ? result : null;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Native JSON-schema output failed for prompt {PromptVersion}; retrying with the schema in the prompt", prompt.Version);

            var response = await chat.GetResponseAsync<T>(messages, options, useJsonSchemaResponseFormat: false, cancellationToken: cancellationToken);
            return response.TryGetResult(out var result) ? result : null;
        }
    }

    public static List<ChatMessage> ToMessages(AiPrompt prompt) =>
    [
        new(ChatRole.System, prompt.System),
        new(ChatRole.User, prompt.User),
    ];
}
