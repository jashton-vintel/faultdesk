using System.Text;
using FaultDesk.Application.Abstractions;
using FaultDesk.Domain.Vehicles;
using Microsoft.Extensions.Logging;

namespace FaultDesk.Application.Tickets;

/// <summary>
/// Before the customer submits, ask the AI for two or three follow-up questions a service adviser would ask.
/// Best-effort: any failure means no questions and the customer submits as normal.
/// </summary>
public sealed class SuggestClarifyingQuestionsHandler(
    IClarifyingQuestionService questions,
    ILogger<SuggestClarifyingQuestionsHandler> logger)
{
    public async Task<IReadOnlyList<string>> HandleAsync(VehicleInput vehicle, string? description, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(vehicle);

        var details = VehicleDetails.Create(vehicle.Make, vehicle.Model, vehicle.Variant, vehicle.Year, vehicle.EngineSizeCc, vehicle.FuelType);
        if (string.IsNullOrWhiteSpace(description))
        {
            return [];
        }

        try
        {
            return await questions.SuggestQuestionsAsync(details, description.Trim(), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Clarifying questions failed; the customer will submit without them");
            return [];
        }
    }
}

/// <summary>Folds the customer's answers into the description so everything downstream (triage, embedding, investigation) sees them.</summary>
public static class ClarifiedDescription
{
    public const string Heading = "Follow-up answers:";

    public static string Compose(string description, IReadOnlyList<string> questions, IReadOnlyList<string?> answers)
    {
        ArgumentNullException.ThrowIfNull(questions);
        ArgumentNullException.ThrowIfNull(answers);

        var text = new StringBuilder(description.Trim());
        var any = false;

        for (var i = 0; i < questions.Count && i < answers.Count; i++)
        {
            var answer = answers[i]?.Trim();
            if (string.IsNullOrEmpty(answer))
            {
                continue;
            }

            if (!any)
            {
                text.AppendLine().AppendLine().AppendLine(Heading);
                any = true;
            }

            text.AppendLine($"- {questions[i].Trim()} {answer}");
        }

        return text.ToString().TrimEnd();
    }
}
