using FaultDesk.Application.Abstractions;
using FaultDesk.Application.Diagnostics;
using FaultDesk.Application.Prompts;
using FaultDesk.Domain.Vehicles;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace FaultDesk.Infrastructure.Ai;

internal sealed class ChatClarifyingQuestionService(IChatClient chat, AiOptions options, ILogger<ChatClarifyingQuestionService> logger) : IClarifyingQuestionService
{
    public async Task<IReadOnlyList<string>> SuggestQuestionsAsync(VehicleDetails vehicle, string description, CancellationToken cancellationToken)
    {
        var prompt = ClarifyPromptBuilder.Build(vehicle, description);
        var chatOptions = new ChatOptions { ModelId = options.TriageModel, MaxOutputTokens = 600 };

        var result = await StructuredOutput.GetAsync<ClarifyingQuestions>(chat, prompt, chatOptions, logger, cancellationToken);
        var questions = result?.Clean() ?? [];

        logger.LogInformation("Clarifying questions ({PromptVersion}) via {Model}: {Count} question(s)", prompt.Version, options.TriageModel, questions.Count);
        return questions;
    }
}
