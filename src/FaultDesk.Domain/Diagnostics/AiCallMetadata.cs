namespace FaultDesk.Domain.Diagnostics;

/// <summary>Provenance of an AI-generated artefact: which provider, model and prompt produced it and what it cost.</summary>
public sealed record AiCallMetadata(
    string Provider,
    string Model,
    string PromptVersion,
    int? InputTokens,
    int? OutputTokens,
    TimeSpan Duration,
    bool WebSearchUsed);
