#pragma warning disable OPENAI001 // Responses API is marked experimental in the OpenAI library.

using Anthropic;
using FaultDesk.Application.Abstractions;
using FaultDesk.Infrastructure.Ai.Mock;
using FaultDesk.Infrastructure.Persistence.Entities;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAI;

namespace FaultDesk.Infrastructure.Ai;

/// <summary>
/// The provider switch (ADR-0002). Application ports are implemented once against Microsoft.Extensions.AI's
/// <see cref="IChatClient"/>; which client sits behind it is configuration, not code.
/// </summary>
public static class AiServiceCollectionExtensions
{
    public static IServiceCollection AddFaultDeskAi(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(AiOptions.SectionName).Get<AiOptions>() ?? new AiOptions();
        services.AddSingleton(options);

        services.AddSingleton<IChatClient>(sp => CreateChatClient(options, sp.GetRequiredService<ILoggerFactory>()));
        services.AddSingleton<IEmbeddingService>(_ => CreateEmbeddingService(options));

        services.AddSingleton<IFaultTriageService, ChatTriageService>();
        services.AddSingleton<IClarifyingQuestionService, ChatClarifyingQuestionService>();
        services.AddSingleton<IDiagnosticsAssistant, ChatDiagnosticsAssistant>();

        return services;
    }

    private static IChatClient CreateChatClient(AiOptions options, ILoggerFactory loggerFactory)
    {
        IChatClient client = options.ChatProvider switch
        {
            AiProviders.Anthropic => new AnthropicClient
                {
                    ApiKey = RequireKey(options.Anthropic.ApiKey, "ANTHROPIC_API_KEY", "Ai:Anthropic:ApiKey"),
                    Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds),
                }
                .AsIChatClient(options.Anthropic.Model),

            AiProviders.OpenAI => new OpenAIClient(RequireKey(options.OpenAI.ApiKey, "OPENAI_API_KEY", "Ai:OpenAI:ApiKey"))
                .GetResponsesClient()
                .AsIChatClient(options.OpenAI.ChatModel),

            AiProviders.Mock => new MockChatClient(),

            _ => throw new InvalidOperationException(
                $"Unknown Ai:ChatProvider '{options.ChatProvider}'. Use {AiProviders.Mock}, {AiProviders.Anthropic} or {AiProviders.OpenAI}."),
        };

        return client.AsBuilder()
            .UseLogging(loggerFactory)
            .Build();
    }

    private static IEmbeddingService CreateEmbeddingService(AiOptions options)
    {
        return options.EmbeddingProvider switch
        {
            AiProviders.OpenAI => new MeaiEmbeddingService(
                new OpenAIClient(RequireKey(options.OpenAI.ApiKey, "OPENAI_API_KEY", "Ai:OpenAI:ApiKey"))
                    .GetEmbeddingClient(options.OpenAI.EmbeddingModel)
                    .AsIEmbeddingGenerator(),
                options.OpenAI.EmbeddingModel,
                TicketEmbedding.Dimensions),

            AiProviders.Mock => new HashingEmbeddingService(),

            _ => throw new InvalidOperationException(
                $"Unknown Ai:EmbeddingProvider '{options.EmbeddingProvider}'. Use {AiProviders.Mock} or {AiProviders.OpenAI}."),
        };
    }

    private static string RequireKey(string? configured, string environmentVariable, string configurationKey)
    {
        var key = string.IsNullOrWhiteSpace(configured)
            ? Environment.GetEnvironmentVariable(environmentVariable)
            : configured;

        return string.IsNullOrWhiteSpace(key)
            ? throw new InvalidOperationException(
                $"No API key found. Set '{configurationKey}' with `dotnet user-secrets` or the {environmentVariable} environment variable (see README).")
            : key.Trim();
    }
}
