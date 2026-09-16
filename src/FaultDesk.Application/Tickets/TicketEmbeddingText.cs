using System.Text;
using FaultDesk.Domain.Tickets;

namespace FaultDesk.Application.Tickets;

/// <summary>
/// The text we embed for a ticket. Includes the triage reading and the workshop's resolution when present,
/// so a resolved ticket is found by what fixed it as well as by what the customer said.
/// </summary>
public static class TicketEmbeddingText
{
    public static string Build(FaultTicket ticket)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        var text = new StringBuilder();
        text.AppendLine($"Vehicle: {ticket.Vehicle.Summary}");

        if (ticket.Triage is { } triage)
        {
            text.AppendLine($"Fault: {triage.Title}");
            if (triage.Category != FaultCategory.Unknown)
            {
                text.AppendLine($"Category: {triage.Category}");
            }

            if (triage.Symptoms.Length > 0)
            {
                text.AppendLine($"Symptoms: {string.Join("; ", triage.Symptoms)}");
            }
        }

        text.AppendLine($"Description: {ticket.Description}");

        if (ticket.Resolution is { } resolution)
        {
            text.AppendLine($"Resolution: {resolution.Notes}");
        }

        return text.ToString().TrimEnd();
    }
}
