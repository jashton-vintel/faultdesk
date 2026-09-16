using FaultDesk.Application.Abstractions;
using FaultDesk.Application.Diagnostics;
using FaultDesk.Application.Prompts;
using FaultDesk.Domain.Tickets;
using FaultDesk.Domain.Vehicles;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace FaultDesk.Infrastructure.Ai;

internal sealed class ChatTriageService(IChatClient chat, AiOptions options, ILogger<ChatTriageService> logger) : IFaultTriageService
{
    public async Task<TriageSummary?> TriageAsync(VehicleDetails vehicle, string description, CancellationToken cancellationToken)
    {
        var prompt = TriagePromptBuilder.Build(vehicle, description);
        var chatOptions = new ChatOptions { ModelId = options.TriageModel, MaxOutputTokens = 1500 };

        var result = await StructuredOutput.GetAsync<TriageResult>(chat, prompt, chatOptions, logger, cancellationToken);
        var summary = result?.ToSummary();

        logger.LogInformation("Triage ({PromptVersion}) via {Model}: {Outcome}", prompt.Version, options.TriageModel, summary?.Title ?? "no result");
        return summary;
    }
}
