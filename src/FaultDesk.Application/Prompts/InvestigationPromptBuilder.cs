using System.Text;
using FaultDesk.Application.Tickets;
using FaultDesk.Domain.Tickets;

namespace FaultDesk.Application.Prompts;

/// <summary>Prompt behind the "AI Investigate" button. Mirrored in docs/prompts/investigate-v1.md.</summary>
public static class InvestigationPromptBuilder
{
    public const string Version = "investigate-v1";
    public const int MaxRelatedTickets = 5;

    public static readonly string[] RequiredHeadings =
    [
        "## Summary",
        "## Probable causes (ranked)",
        "## Diagnostic steps",
        "## Parts likely needed",
        "## Safety warnings",
        "## Sources",
    ];

    public const string SystemPrompt = """
        You are FaultDesk's diagnostics assistant: a senior diagnostic technician writing for the mechanics of a UK garage.
        Given a customer's fault report, search the web for reported faults that match this make, model, engine and symptoms,
        then write a practical investigation plan the workshop can follow.

        Rules:
        - Rank probable causes by likelihood and give the evidence: the customer's words, known issues for this vehicle, and
          related tickets from this garage (a previous resolution on the same or a similar car is strong evidence).
        - Be specific about checks: which tool, what to measure, the expected reading and what a fail looks like.
        - Put safety first when the fault could affect braking, steering, tyres, fire risk or leave the driver stranded.
        - Cite every web-sourced claim with its page URL. Never invent technical service bulletins, recalls or part numbers.
        - If web search is unavailable, say so in the Summary and rely on general knowledge, clearly marked as such.
        - Use UK terminology and metric units. Be concise; mechanics will read this on a tablet.

        Write in markdown using exactly these headings, in this order, and nothing before the first heading:
        ## Summary
        ## Probable causes (ranked)
        ## Diagnostic steps
        ## Parts likely needed
        ## Safety warnings
        ## Sources

        Under "Probable causes (ranked)" use a numbered list in the form: **cause** - likelihood High/Medium/Low - evidence.
        Under "Diagnostic steps" use a numbered list ordered cheapest and quickest first.
        Under "Sources" list each source as "- [title](url)". Write "None used" if you did not use the web.
        """;

    public static AiPrompt Build(FaultTicket ticket, IReadOnlyList<RelatedTicket> relatedTickets)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        ArgumentNullException.ThrowIfNull(relatedTickets);

        var user = new StringBuilder();
        user.AppendLine($"Ticket {ticket.Reference}, reported {ticket.CreatedAt:yyyy-MM-dd}.");
        user.AppendLine($"Vehicle: {ticket.Vehicle.Summary} (registration {ticket.Registration.Display}).");
        user.AppendLine();
        user.AppendLine("Customer description:");
        user.AppendLine("```");
        user.AppendLine(ticket.Description);
        user.AppendLine("```");

        if (ticket.Triage is { } triage)
        {
            user.AppendLine();
            user.AppendLine("Triage summary (extracted from the description, not verified):");
            user.AppendLine($"- Title: {triage.Title}");
            user.AppendLine($"- Category: {triage.Category}; severity: {triage.Severity}; safe to drive: {triage.SafeToDrive}");
            if (triage.Symptoms.Length > 0)
            {
                user.AppendLine($"- Symptoms: {string.Join("; ", triage.Symptoms)}");
            }

            if (triage.LikelySystems.Length > 0)
            {
                user.AppendLine($"- Systems mentioned: {string.Join("; ", triage.LikelySystems)}");
            }
        }

        var related = relatedTickets.Take(MaxRelatedTickets).ToList();
        if (related.Count > 0)
        {
            user.AppendLine();
            user.AppendLine("Related tickets from this garage, most relevant first:");
            foreach (var item in related)
            {
                var similarity = item.Similarity is { } s ? $", {s:P0} similar" : ", same vehicle";
                user.AppendLine($"- {item.Reference} ({item.CreatedAt:yyyy-MM-dd}, {item.Vehicle}{similarity}, {item.Status}): {item.Headline}");
                if (item.ResolutionNotes is not null)
                {
                    user.AppendLine($"  Resolution: {item.ResolutionNotes}");
                }
            }
        }

        user.AppendLine();
        user.AppendLine("Write the investigation plan now.");

        return new AiPrompt(Version, SystemPrompt, user.ToString());
    }
}
