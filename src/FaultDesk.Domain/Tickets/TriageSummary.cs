using FaultDesk.Domain.Common;

namespace FaultDesk.Domain.Tickets;

/// <summary>Structured reading of the customer's free text, produced by AI triage for the service adviser.</summary>
public sealed record TriageSummary
{
    public string Title { get; }
    public FaultCategory Category { get; }
    public Severity Severity { get; }
    public SafeToDrive SafeToDrive { get; }
    public string[] Symptoms { get; }
    public string[] LikelySystems { get; }
    public string[] AdviserQuestions { get; }
    public string CustomerSummary { get; }

    private TriageSummary(
        string title,
        FaultCategory category,
        Severity severity,
        SafeToDrive safeToDrive,
        string[] symptoms,
        string[] likelySystems,
        string[] adviserQuestions,
        string customerSummary)
    {
        Title = title;
        Category = category;
        Severity = severity;
        SafeToDrive = safeToDrive;
        Symptoms = symptoms;
        LikelySystems = likelySystems;
        AdviserQuestions = adviserQuestions;
        CustomerSummary = customerSummary;
    }

    public static TriageSummary Create(
        string? title,
        FaultCategory category,
        Severity severity,
        SafeToDrive safeToDrive,
        IEnumerable<string>? symptoms,
        IEnumerable<string>? likelySystems,
        IEnumerable<string>? adviserQuestions,
        string? customerSummary)
    {
        return new TriageSummary(
            Guard.Required(title, "Title", 120),
            category,
            severity,
            safeToDrive,
            Clean(symptoms),
            Clean(likelySystems),
            Clean(adviserQuestions),
            Guard.Optional(customerSummary, "Customer summary", 500) ?? string.Empty);
    }

    private static string[] Clean(IEnumerable<string>? items) =>
        (items ?? [])
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToArray();
}
