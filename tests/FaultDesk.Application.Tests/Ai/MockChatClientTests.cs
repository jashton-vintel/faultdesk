using FaultDesk.Application.Diagnostics;
using FaultDesk.Application.Prompts;
using FaultDesk.Application.Tickets;
using FaultDesk.Infrastructure.Ai.Mock;
using Microsoft.Extensions.AI;

namespace FaultDesk.Application.Tests.Ai;

public class MockChatClientTests
{
    private static List<ChatMessage> Messages(AiPrompt prompt) =>
        [new(ChatRole.System, prompt.System), new(ChatRole.User, prompt.User)];

    [Fact]
    public async Task Triage_prompt_gets_parseable_structured_output_that_reacts_to_the_description()
    {
        using var client = new MockChatClient();
        var prompt = TriagePromptBuilder.Build(TestData.Focus(), "Grinding noise when braking and the pedal feels soft.");

        var response = await client.GetResponseAsync<TriageResult>(Messages(prompt));

        Assert.True(response.TryGetResult(out var result));
        var summary = result.ToSummary();
        Assert.NotNull(summary);
        Assert.Equal(Domain.Tickets.FaultCategory.Brakes, summary.Category);
        Assert.Equal(Domain.Tickets.Severity.High, summary.Severity);
        Assert.NotEmpty(summary.AdviserQuestions);
    }

    [Fact]
    public async Task Clarify_prompt_gets_two_or_three_questions()
    {
        using var client = new MockChatClient();
        var prompt = ClarifyPromptBuilder.Build(TestData.Focus(), "The engine cuts out at idle when warm.");

        var response = await client.GetResponseAsync<ClarifyingQuestions>(Messages(prompt));

        Assert.True(response.TryGetResult(out var result));
        Assert.InRange(result.Clean().Count, 2, 3);
    }

    [Fact]
    public async Task Investigate_prompt_streams_a_report_with_every_required_heading_and_usage()
    {
        using var client = new MockChatClient();
        var ticket = TestData.Ticket(description: "Temperature gauge climbs into the red in traffic and there is a sweet smell.");
        var prompt = InvestigationPromptBuilder.Build(ticket, []);

        var updates = await client.GetStreamingResponseAsync(Messages(prompt)).ToListAsync();
        var text = string.Concat(updates.Select(u => u.Text));

        Assert.All(InvestigationPromptBuilder.RequiredHeadings, heading => Assert.Contains(heading, text));
        Assert.Contains("cooling system", text);
        Assert.Contains(updates, u => u.Contents.OfType<UsageContent>().Any());
        Assert.True(updates.Count > 10, "the mock should stream in many small chunks");
    }
}
