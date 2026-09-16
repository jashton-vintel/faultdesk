using FaultDesk.Domain.Tickets;

namespace FaultDesk.Application.Diagnostics;

/// <summary>
/// The JSON shape we ask the model for. Enums are strings so a slightly-off answer degrades to Unknown
/// instead of failing deserialisation.
/// </summary>
public sealed record TriageResult
{
    public string? Title { get; init; }
    public string? Category { get; init; }
    public string? Severity { get; init; }
    public string? SafeToDrive { get; init; }
    public string[]? Symptoms { get; init; }
    public string[]? LikelySystems { get; init; }
    public string[]? AdviserQuestions { get; init; }
    public string? CustomerSummary { get; init; }

    /// <summary>Maps to the domain value object; returns null when the model gave us nothing usable.</summary>
    public TriageSummary? ToSummary()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            return null;
        }

        return TriageSummary.Create(
            Truncate(Title, 120),
            ParseEnum<FaultCategory>(Category),
            ParseEnum<Domain.Tickets.Severity>(Severity),
            ParseEnum<Domain.Tickets.SafeToDrive>(SafeToDrive),
            Symptoms,
            LikelySystems,
            AdviserQuestions,
            Truncate(CustomerSummary, 500));
    }

    private static TEnum ParseEnum<TEnum>(string? value)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return default;
        }

        var compact = value.Replace(" ", string.Empty).Replace("/", string.Empty).Replace("&", string.Empty).Replace("-", string.Empty);
        return Enum.TryParse<TEnum>(compact, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed) ? parsed : default;
    }

    private static string? Truncate(string? value, int maxLength) =>
        value is { Length: > 0 } && value.Length > maxLength ? value[..maxLength].TrimEnd() : value;
}
