using FaultDesk.Application.Tickets;

namespace FaultDesk.Application.Tests.Tickets;

public class TicketEmbeddingTextTests
{
    [Fact]
    public void Includes_vehicle_and_description_only_when_there_is_nothing_else()
    {
        var ticket = TestData.Ticket();

        var text = TicketEmbeddingText.Build(ticket);

        Assert.Equal(
            "Vehicle: 2018 Ford Focus 1.0 EcoBoost, 999cc petrol\nDescription: Rattling noise from the front over bumps.",
            text.Replace("\r\n", "\n"));
    }

    [Fact]
    public void Includes_triage_and_resolution_when_present()
    {
        var ticket = TestData.Ticket();
        ticket.ApplyTriage(TestData.Triage(), TestData.Now);
        ticket.Resolve("Replaced both front drop links.", TestData.Now.AddDays(1));

        var text = TicketEmbeddingText.Build(ticket);

        Assert.Contains("Fault: Front suspension knock over bumps", text);
        Assert.Contains("Category: SteeringSuspension", text);
        Assert.Contains("Symptoms: knocking over bumps", text);
        Assert.Contains("Resolution: Replaced both front drop links.", text);
    }
}
