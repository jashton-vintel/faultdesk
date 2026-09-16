using FaultDesk.Infrastructure.Ai.Mock;

namespace FaultDesk.Infrastructure.Ai;

public static class AiProviders
{
    public const string Mock = "Mock";
    public const string Anthropic = "Anthropic";
    public const string OpenAI = "OpenAI";
}

/// <summary>
/// Bound from the "Ai" configuration section. API keys are never placed in appsettings.json; they come from
/// user-secrets (dev), environment variables or the git-ignored .env used by docker compose.
/// </summary>
public sealed class AiOptions
{
    public const string SectionName = "Ai";

    /// <summary>Mock, Anthropic or OpenAI. Mock needs no key and is the default.</summary>
    public string ChatProvider { get; set; } = AiProviders.Mock;

    /// <summary>Mock or OpenAI (Anthropic has no embeddings API).</summary>
    public string EmbeddingProvider { get; set; } = AiProviders.Mock;

    public AnthropicSettings Anthropic { get; set; } = new();

    public OpenAISettings OpenAI { get; set; } = new();

    public int TimeoutSeconds { get; set; } = 300;

    public int InvestigateMaxOutputTokens { get; set; } = 4000;

    public string ChatModel => ChatProvider switch
    {
        AiProviders.Anthropic => Anthropic.Model,
        AiProviders.OpenAI => OpenAI.ChatModel,
        _ => MockChatClient.ModelId,
    };

    public string TriageModel => ChatProvider switch
    {
        AiProviders.Anthropic => Anthropic.TriageModel,
        AiProviders.OpenAI => OpenAI.ChatModel,
        _ => MockChatClient.ModelId,
    };

    public bool IsMockChat => string.Equals(ChatProvider, AiProviders.Mock, StringComparison.OrdinalIgnoreCase);

    public sealed class AnthropicSettings
    {
        public string? ApiKey { get; set; }

        public string Model { get; set; } = "claude-opus-5";

        /// <summary>Triage runs on every submission; point this at a faster model if submit latency matters more than depth.</summary>
        public string TriageModel { get; set; } = "claude-opus-5";
    }

    public sealed class OpenAISettings
    {
        public string? ApiKey { get; set; }

        public string ChatModel { get; set; } = "gpt-5";

        public string EmbeddingModel { get; set; } = "text-embedding-3-small";
    }
}
