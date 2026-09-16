using FaultDesk.Domain.Vehicles;

namespace FaultDesk.Application.Prompts;

/// <summary>Prompt for turning a customer's free text into a structured triage summary. Mirrored in docs/prompts/triage-v1.md.</summary>
public static class TriagePromptBuilder
{
    public const string Version = "triage-v1";

    public const string SystemPrompt = """
        You are FaultDesk's triage assistant for a UK garage service adviser.
        Read the customer's description of a vehicle fault and extract what they reported.

        Rules:
        - Extract, do not diagnose. Never invent facts that are not in the description.
        - Use "Unknown" when you are unsure of a category, severity or whether the car is safe to drive.
        - Use UK terminology (MOT, DPF, AdBlue, handbrake, bonnet, tyre).
        - Keep the customer's own words for symptoms where possible.
        - Category must be one of: Engine, Electrical, Brakes, SteeringSuspension, Transmission, ExhaustEmissions, Cooling, Tyres, Bodywork, Unknown.
        - Severity must be one of: Low, Medium, High, Unknown. Treat anything affecting brakes, steering, smoke or fire as High.
        - SafeToDrive must be one of: Yes, Caution, No, Unknown.
        - Title: at most 80 characters, written for the workshop job card.
        - AdviserQuestions: 3 to 5 short questions the adviser should ask the customer to narrow the fault.
        - CustomerSummary: one plain-English sentence restating the problem back to the customer.

        Respond with JSON only, matching the requested schema.
        """;

    public static AiPrompt Build(VehicleDetails vehicle, string description)
    {
        ArgumentNullException.ThrowIfNull(vehicle);

        var user = $"""
            Vehicle: {vehicle.Summary}

            Customer description:
            ```
            {description}
            ```
            """;

        return new AiPrompt(Version, SystemPrompt, user);
    }
}
