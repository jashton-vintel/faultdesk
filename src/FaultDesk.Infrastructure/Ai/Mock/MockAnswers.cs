using System.Text.Json;
using FaultDesk.Application.Diagnostics;

namespace FaultDesk.Infrastructure.Ai.Mock;

/// <summary>
/// Canned-but-plausible answers for demo mode. A handful of keyword rules pick a fault area so the demo reacts to
/// what the customer typed, and every answer is shaped exactly like the real model's output.
/// </summary>
internal static class MockAnswers
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private sealed record Rule(string[] Keywords, string Category, string System, string Cause, string Severity, string SafeToDrive);

    private static readonly Rule[] Rules =
    [
        new(["brake", "braking", "pedal", "stopping", "handbrake"], "Brakes", "front brakes", "worn front brake pads and discs", "High", "Caution"),
        new(["engine light", "management light", "cuts out", "cutting out", "cut out", "misfire", "stall"], "Engine", "engine management and ignition", "a misfire from a failing ignition coil or injector", "High", "Caution"),
        new(["steer", "wobble", "pull", "knock", "rattle", "clunk", "bump", "suspension", "creak"], "SteeringSuspension", "front suspension", "worn anti-roll bar drop links or bushes", "Medium", "Caution"),
        new(["overheat", "coolant", "temperature", "steam", "fan"], "Cooling", "cooling system", "a coolant leak or a stuck thermostat", "High", "No"),
        new(["smoke", "exhaust", "emission", "dpf", "adblue", "smell"], "ExhaustEmissions", "exhaust and after-treatment system", "a blocked DPF or an exhaust leak", "Medium", "Caution"),
        new(["battery", "start", "crank", "electric", "light", "dash", "fuse", "flat"], "Electrical", "charging and starting system", "a weak 12V battery or a failing alternator", "Medium", "Yes"),
        new(["clutch", "gear", "gearbox", "transmission", "slip", "judder"], "Transmission", "clutch and gearbox", "a worn clutch or low gearbox oil", "Medium", "Caution"),
        new(["tyre", "tire", "puncture", "vibration", "tread"], "Tyres", "tyres and wheels", "uneven tyre wear or wheel imbalance", "Medium", "Caution"),
        new(["dent", "scratch", "rust", "panel", "bumper", "door", "paint"], "Bodywork", "bodywork", "cosmetic damage needing panel repair", "Low", "Yes"),
        new(["engine", "misfire", "idle", "stall", "power", "noise", "oil", "rough"], "Engine", "engine", "an ignition or fuel-delivery fault", "Medium", "Caution"),
    ];

    private static readonly Rule Fallback = new([], "Unknown", "vehicle", "a fault that needs an inspection to narrow down", "Unknown", "Unknown");

    public static string Triage(string userPrompt)
    {
        var (rule, vehicle, description) = Analyse(userPrompt);
        var firstSentence = FirstSentence(description);

        var result = new TriageResult
        {
            Title = Truncate($"{Pretty(rule.Category)}: {firstSentence}", 80),
            Category = rule.Category,
            Severity = rule.Severity,
            SafeToDrive = rule.SafeToDrive,
            Symptoms = [firstSentence],
            LikelySystems = [rule.System],
            AdviserQuestions =
            [
                "When did the problem start, and has it got worse?",
                "Does it happen from cold, when warm, or all the time?",
                "Are there any warning lights on the dashboard?",
                "Has the car had any recent work done?",
            ],
            CustomerSummary = $"You reported {LowerFirst(firstSentence.TrimEnd('.'))} on your {vehicle}.",
        };

        return JsonSerializer.Serialize(result, Json);
    }

    public static string Clarify(string userPrompt)
    {
        var (rule, _, _) = Analyse(userPrompt);
        var questions = new ClarifyingQuestions
        {
            Questions =
            [
                "When does it happen: from cold, once warmed up, or all the time?",
                rule.Category switch
                {
                    "Brakes" => "Does the car pull to one side or does the pedal feel different when it happens?",
                    "SteeringSuspension" => "Does the noise change when you turn, brake or go over bumps?",
                    "Electrical" => "Do any dashboard warning lights come on, and does it happen after the car has been parked for a while?",
                    "Cooling" => "Has the temperature gauge gone into the red or have you noticed any coolant on the ground?",
                    _ => "Are there any warning lights on the dashboard when it happens?",
                },
                "Has the car had any work done recently, and roughly how long has this been going on?",
            ],
        };

        return JsonSerializer.Serialize(questions, Json);
    }

    public static string Investigate(string userPrompt)
    {
        var (rule, vehicle, description) = Analyse(userPrompt);
        var safety = rule.Category is "Brakes" or "SteeringSuspension" or "Cooling"
            ? $"- Treat the {rule.System} as safety-critical: do not return the car to the customer until it has been inspected and road tested."
            : "- No immediate safety concern is obvious from the description; confirm on inspection before release.";

        return $"""
            ## Summary
            Demo mode: no AI provider is configured, so this is a canned report in the same shape as the real one. For the {vehicle} reporting "{Truncate(FirstSentence(description), 90)}", the most likely area is the {rule.System}. Configure `Ai:ChatProvider` to get a real, web-searched investigation.

            ## Probable causes (ranked)
            1. **{Capitalise(rule.Cause)}** - likelihood High - matches the symptoms described and is a common fault on vehicles of this age.
            2. **Loose, worn or perished ancillary components** - likelihood Medium - often found alongside the primary fault during inspection.
            3. **Intermittent electrical connection or sensor fault** - likelihood Low - only if the symptom comes and goes with no pattern.

            ## Diagnostic steps
            1. Road test to confirm the symptom and note when it occurs (speed, load, temperature, road surface).
            2. Visual inspection of the {rule.System} for leaks, play, damage or recent repairs.
            3. Read stored fault codes with a diagnostic tool and review live data for anything out of range.
            4. Compare with the related tickets shown on this screen; a previous resolution on a similar car is the strongest lead.

            ## Parts likely needed
            - Depends on findings; budget for the components involved in {rule.Cause}.

            ## Safety warnings
            {safety}

            ## Sources
            None used (demo mode). With a real provider the assistant searches the web and lists the pages it used here.
            """;
    }

    private static (Rule Rule, string Vehicle, string Description) Analyse(string userPrompt)
    {
        var vehicle = userPrompt.Split('\n')
            .Select(l => l.Trim())
            .FirstOrDefault(l => l.StartsWith("Vehicle:", StringComparison.Ordinal))?["Vehicle:".Length..].Trim().TrimEnd('.')
            ?? "vehicle";

        var description = ExtractDescription(userPrompt);
        var lower = description.ToLowerInvariant();
        var rule = Rules.FirstOrDefault(r => r.Keywords.Any(lower.Contains)) ?? Fallback;
        return (rule, vehicle, description);
    }

    private static string ExtractDescription(string userPrompt)
    {
        var start = userPrompt.IndexOf("```", StringComparison.Ordinal);
        if (start < 0)
        {
            return userPrompt;
        }

        var end = userPrompt.IndexOf("```", start + 3, StringComparison.Ordinal);
        return (end < 0 ? userPrompt[(start + 3)..] : userPrompt[(start + 3)..end]).Trim();
    }

    private static string FirstSentence(string text)
    {
        var trimmed = text.Replace('\n', ' ').Trim();
        var end = trimmed.IndexOfAny(['.', '!', '?']);
        var sentence = end > 0 ? trimmed[..(end + 1)] : trimmed;
        return Truncate(sentence, 120);
    }

    private static string Pretty(string category) => category switch
    {
        "SteeringSuspension" => "Steering/suspension",
        "ExhaustEmissions" => "Exhaust/emissions",
        _ => category,
    };

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..(max - 3)].TrimEnd() + "...";

    private static string LowerFirst(string value) => value.Length == 0 ? value : char.ToLowerInvariant(value[0]) + value[1..];

    private static string Capitalise(string value) => value.Length == 0 ? value : char.ToUpperInvariant(value[0]) + value[1..];
}
