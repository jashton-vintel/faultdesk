namespace FaultDesk.Application.Diagnostics;

/// <summary>The JSON shape we ask the model for when suggesting follow-up questions.</summary>
public sealed record ClarifyingQuestions
{
    public string[]? Questions { get; init; }

    public IReadOnlyList<string> Clean() =>
        (Questions ?? [])
            .Where(q => !string.IsNullOrWhiteSpace(q))
            .Select(q => q.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToList();
}
