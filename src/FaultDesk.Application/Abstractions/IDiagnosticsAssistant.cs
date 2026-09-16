using FaultDesk.Application.Prompts;
using FaultDesk.Domain.Diagnostics;

namespace FaultDesk.Application.Abstractions;

/// <summary>Runs an investigation prompt against an LLM (with web search where available) and streams the report back.</summary>
public interface IDiagnosticsAssistant
{
    /// <summary>Yields text deltas as they arrive and finally a single update carrying the call metadata.</summary>
    IAsyncEnumerable<DiagnosticsUpdate> InvestigateAsync(AiPrompt prompt, CancellationToken cancellationToken);
}

public sealed record DiagnosticsUpdate(string? TextDelta, AiCallMetadata? Completed)
{
    public static DiagnosticsUpdate Text(string delta) => new(delta, null);

    public static DiagnosticsUpdate Done(AiCallMetadata metadata) => new(null, metadata);
}
