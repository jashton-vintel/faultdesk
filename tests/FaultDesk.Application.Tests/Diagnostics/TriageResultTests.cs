using FaultDesk.Application.Diagnostics;
using FaultDesk.Domain.Tickets;

namespace FaultDesk.Application.Tests.Diagnostics;

public class TriageResultTests
{
    [Fact]
    public void Maps_well_formed_output_to_the_domain_summary()
    {
        var result = new TriageResult
        {
            Title = "Front suspension knock over bumps",
            Category = "SteeringSuspension",
            Severity = "medium",
            SafeToDrive = "CAUTION",
            Symptoms = ["knocking over bumps", " knocking over bumps "],
            LikelySystems = ["drop links"],
            AdviserQuestions = ["Does it change when turning?"],
            CustomerSummary = "A knocking noise from the front suspension.",
        };

        var summary = result.ToSummary();

        Assert.NotNull(summary);
        Assert.Equal(FaultCategory.SteeringSuspension, summary.Category);
        Assert.Equal(Severity.Medium, summary.Severity);
        Assert.Equal(SafeToDrive.Caution, summary.SafeToDrive);
        Assert.Equal(["knocking over bumps"], summary.Symptoms);
    }

    [Theory]
    [InlineData("Steering / Suspension", FaultCategory.SteeringSuspension)]
    [InlineData("Exhaust & Emissions", FaultCategory.ExhaustEmissions)]
    [InlineData("Gearbox", FaultCategory.Unknown)]
    [InlineData(null, FaultCategory.Unknown)]
    public void Unrecognised_categories_degrade_to_Unknown(string? category, FaultCategory expected)
    {
        var summary = new TriageResult { Title = "Something", Category = category }.ToSummary();

        Assert.NotNull(summary);
        Assert.Equal(expected, summary.Category);
        Assert.Equal(Severity.Unknown, summary.Severity);
    }

    [Fact]
    public void Missing_title_means_no_summary()
    {
        Assert.Null(new TriageResult { Category = "Engine" }.ToSummary());
    }

    [Fact]
    public void Over_long_text_is_truncated_rather_than_rejected()
    {
        var summary = new TriageResult { Title = new string('t', 300), CustomerSummary = new string('s', 900) }.ToSummary();

        Assert.NotNull(summary);
        Assert.Equal(120, summary.Title.Length);
        Assert.Equal(500, summary.CustomerSummary.Length);
    }
}
