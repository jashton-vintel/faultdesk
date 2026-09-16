using FaultDesk.Domain.Vehicles;

namespace FaultDesk.Application.Prompts;

/// <summary>Prompt for the customer-facing follow-up questions step. Mirrored in docs/prompts/clarify-v1.md.</summary>
public static class ClarifyPromptBuilder
{
    public const string Version = "clarify-v1";

    public const string SystemPrompt = """
        You are FaultDesk's booking assistant for a UK garage, talking to a customer who has just described a problem with their car.
        Suggest the 2 or 3 most useful follow-up questions a service adviser would ask before booking the car in, so the workshop
        can prepare. Good questions narrow down when the fault happens (cold start, speed, braking, turning, bumps), what the
        customer sees or hears (noises, smells, warning lights, leaks), how long it has been happening and any recent work.

        Rules:
        - Plain English, one sentence each, no jargon, no diagnosis and no advice.
        - Do not ask for anything already stated in the description.
        - Return between 2 and 3 questions.

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
