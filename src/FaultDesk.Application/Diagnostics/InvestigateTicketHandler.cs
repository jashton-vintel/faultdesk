using System.Runtime.CompilerServices;
using System.Text;
using FaultDesk.Application.Abstractions;
using FaultDesk.Application.Prompts;
using FaultDesk.Application.Tickets;
using FaultDesk.Domain.Diagnostics;
using FaultDesk.Domain.Tickets;
using Microsoft.Extensions.Logging;

namespace FaultDesk.Application.Diagnostics;

/// <param name="TextDelta">A piece of the report as it streams in.</param>
/// <param name="Completed">Set on the final update once the investigation has been persisted.</param>
public sealed record InvestigationUpdate(string? TextDelta, Investigation? Completed);

/// <summary>
/// Builds the investigation prompt from everything we know about the ticket (vehicle, description, triage,
/// same-vehicle history and similar tickets), streams the assistant's report and persists it when complete.
/// </summary>
public sealed class InvestigateTicketHandler(
    GetTicketDetailHandler getTicketDetail,
    IDiagnosticsAssistant assistant,
    IInvestigationRepository investigations,
    TimeProvider timeProvider,
    ILogger<InvestigateTicketHandler> logger)
{
    public async IAsyncEnumerable<InvestigationUpdate> HandleAsync(
        TicketId ticketId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var detail = await getTicketDetail.HandleAsync(ticketId, cancellationToken)
            ?? throw new KeyNotFoundException($"Ticket {ticketId} was not found.");

        var related = detail.VehicleHistory
            .Concat(detail.SimilarTickets)
            .Take(InvestigationPromptBuilder.MaxRelatedTickets)
            .ToList();

        var prompt = InvestigationPromptBuilder.Build(detail.Ticket, related);
        logger.LogInformation("Investigating ticket {Reference} with prompt {PromptVersion}", detail.Ticket.Reference, prompt.Version);

        var report = new StringBuilder();
        AiCallMetadata? metadata = null;

        await foreach (var update in assistant.InvestigateAsync(prompt, cancellationToken))
        {
            if (update.TextDelta is { Length: > 0 } text)
            {
                report.Append(text);
                yield return new InvestigationUpdate(text, null);
            }

            if (update.Completed is not null)
            {
                metadata = update.Completed;
            }
        }

        metadata ??= new AiCallMetadata("unknown", "unknown", prompt.Version, null, null, TimeSpan.Zero, false);

        var investigation = Investigation.Create(ticketId, report.ToString(), metadata, timeProvider.GetUtcNow());
        await investigations.AddAsync(investigation, cancellationToken);

        logger.LogInformation(
            "Investigation for {Reference} complete: {Model}, {InputTokens} in / {OutputTokens} out, {Duration:F1}s",
            detail.Ticket.Reference, metadata.Model, metadata.InputTokens, metadata.OutputTokens, metadata.Duration.TotalSeconds);

        yield return new InvestigationUpdate(null, investigation);
    }
}
