using FaultDesk.Application.Prompts;
using FaultDesk.Application.Tickets;
using FaultDesk.Domain.Tickets;

namespace FaultDesk.Application.Tests.Prompts;

public class PromptBuilderTests
{
    [Fact]
    public void Triage_prompt_carries_vehicle_description_and_version()
    {
        var prompt = TriagePromptBuilder.Build(TestData.Focus(), "Rattling noise from the front over bumps.");

        Assert.Equal("triage-v1", prompt.Version);
        Assert.Contains("Extract, do not diagnose", prompt.System);
        Assert.Contains("2018 Ford Focus 1.0 EcoBoost, 999cc petrol", prompt.User);
        Assert.Contains("Rattling noise from the front over bumps.", prompt.User);
    }

    [Fact]
    public void Clarify_prompt_asks_for_two_or_three_questions()
    {
        var prompt = ClarifyPromptBuilder.Build(TestData.Focus(), "Car pulls to the left when braking.");

        Assert.Equal("clarify-v1", prompt.Version);
        Assert.Contains("between 2 and 3 questions", prompt.System);
        Assert.Contains("Car pulls to the left when braking.", prompt.User);
    }

    [Fact]
    public void Investigation_prompt_includes_triage_related_tickets_and_citation_rules()
    {
        var ticket = TestData.Ticket();
        ticket.ApplyTriage(TestData.Triage(), TestData.Now);
        var related = new List<RelatedTicket>
        {
            new(TicketId.New(), "FD-260101-AAAA", TestData.Now.AddMonths(-8), "AB12 CDE", "2018 Ford Focus", "Brake squeal", TicketStatus.Resolved, "Front pads replaced.", null),
            new(TicketId.New(), "FD-260201-BBBB", TestData.Now.AddMonths(-7), "XY19 ZZZ", "2017 Ford Focus", "Front knock over bumps", TicketStatus.Resolved, "Drop links replaced.", 0.91),
        };

        var prompt = InvestigationPromptBuilder.Build(ticket, related);

        Assert.Equal("investigate-v1", prompt.Version);
        Assert.Contains("Cite every web-sourced claim with its page URL", prompt.System);
        Assert.All(InvestigationPromptBuilder.RequiredHeadings, heading => Assert.Contains(heading, prompt.System));
        Assert.Contains("registration AB12 CDE", prompt.User);
        Assert.Contains("Title: Front suspension knock over bumps", prompt.User);
        Assert.Contains("FD-260101-AAAA", prompt.User);
        Assert.Contains("Resolution: Front pads replaced.", prompt.User);
        Assert.Contains("91% similar", prompt.User);
        Assert.Contains("same vehicle", prompt.User);
    }

    [Fact]
    public void Investigation_prompt_caps_related_tickets_and_copes_without_triage()
    {
        var ticket = TestData.Ticket();
        var related = Enumerable.Range(0, 8)
            .Select(i => new RelatedTicket(TicketId.New(), $"FD-2601{i:00}-CCCC", TestData.Now, "AB12 CDE", "2018 Ford Focus", $"Related {i}", TicketStatus.New, null, 0.5))
            .ToList();

        var prompt = InvestigationPromptBuilder.Build(ticket, related);

        Assert.DoesNotContain("Triage summary", prompt.User);
        Assert.Equal(InvestigationPromptBuilder.MaxRelatedTickets, related.Count(r => prompt.User.Contains(r.Reference)));
    }
}
