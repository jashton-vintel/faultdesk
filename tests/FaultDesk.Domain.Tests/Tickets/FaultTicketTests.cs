using FaultDesk.Domain.Common;
using FaultDesk.Domain.Tickets;
using FaultDesk.Domain.Vehicles;

namespace FaultDesk.Domain.Tests.Tickets;

public class FaultTicketTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 16, 9, 0, 0, TimeSpan.Zero);
    private static readonly VehicleRegistration Registration = VehicleRegistration.Parse("AB12 CDE");
    private static readonly VehicleDetails Vehicle = VehicleDetails.Create("Ford", "Focus", "1.0 EcoBoost", 2018, 999, FuelType.Petrol, currentYear: 2026);

    private static FaultTicket NewTicket() =>
        FaultTicket.Create(Registration, Vehicle, "Rattling noise from the front when going over bumps.", " Sam Patel ", null, Now);

    [Fact]
    public void Create_starts_a_new_ticket_with_a_reference()
    {
        var ticket = NewTicket();

        Assert.NotEqual(default, ticket.Id);
        Assert.StartsWith("FD-260916-", ticket.Reference);
        Assert.Equal(14, ticket.Reference.Length);
        Assert.Equal(TicketStatus.New, ticket.Status);
        Assert.Equal("Sam Patel", ticket.CustomerName);
        Assert.Null(ticket.CustomerContact);
        Assert.Null(ticket.Triage);
        Assert.Equal(Now, ticket.CreatedAt);
        Assert.Equal("Rattling noise from the front when going over bumps.", ticket.Headline);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("too short")]
    public void Create_rejects_descriptions_that_are_too_short(string? description)
    {
        Assert.Throws<DomainException>(() => FaultTicket.Create(Registration, Vehicle, description, null, null, Now));
    }

    [Fact]
    public void Create_rejects_descriptions_that_are_too_long()
    {
        var description = new string('x', FaultTicket.MaxDescriptionLength + 1);

        Assert.Throws<DomainException>(() => FaultTicket.Create(Registration, Vehicle, description, null, null, Now));
    }

    [Fact]
    public void ApplyTriage_records_the_summary_and_changes_the_headline()
    {
        var ticket = NewTicket();
        var triage = TriageSummary.Create(
            "Front suspension knock over bumps",
            FaultCategory.SteeringSuspension,
            Severity.Medium,
            SafeToDrive.Caution,
            ["rattle over bumps", " rattle over bumps ", "front"],
            ["anti-roll bar drop links"],
            ["Does it happen when turning?"],
            "A knocking noise from the front suspension.");

        ticket.ApplyTriage(triage, Now.AddSeconds(5));

        Assert.Same(triage, ticket.Triage);
        Assert.Equal("Front suspension knock over bumps", ticket.Headline);
        Assert.Equal(["rattle over bumps", "front"], triage.Symptoms);
        Assert.Equal(Now.AddSeconds(5), ticket.UpdatedAt);
    }

    [Fact]
    public void Start_moves_a_new_ticket_into_progress()
    {
        var ticket = NewTicket();

        ticket.Start(Now.AddHours(1));

        Assert.Equal(TicketStatus.InProgress, ticket.Status);
        Assert.Throws<DomainException>(() => ticket.Start(Now.AddHours(2)));
    }

    [Fact]
    public void Resolve_requires_notes_and_records_the_resolution()
    {
        var ticket = NewTicket();
        ticket.Start(Now);

        Assert.Throws<DomainException>(() => ticket.Resolve("  ", Now.AddDays(1)));

        ticket.Resolve("Replaced both front drop links; road tested, noise gone.", Now.AddDays(1));

        Assert.Equal(TicketStatus.Resolved, ticket.Status);
        Assert.NotNull(ticket.Resolution);
        Assert.Equal("Replaced both front drop links; road tested, noise gone.", ticket.Resolution.Notes);
        Assert.Equal(Now.AddDays(1), ticket.Resolution.ResolvedAt);
    }

    [Fact]
    public void Resolve_can_skip_in_progress_but_not_run_twice()
    {
        var ticket = NewTicket();

        ticket.Resolve("Loose heat shield, re-secured.", Now);

        Assert.Equal(TicketStatus.Resolved, ticket.Status);
        Assert.Throws<DomainException>(() => ticket.Resolve("Again", Now.AddDays(1)));
    }

    [Fact]
    public void Reopen_returns_a_resolved_ticket_to_in_progress()
    {
        var ticket = NewTicket();
        ticket.Resolve("Loose heat shield, re-secured.", Now);

        ticket.Reopen(Now.AddDays(2));

        Assert.Equal(TicketStatus.InProgress, ticket.Status);
        Assert.Null(ticket.Resolution);
        Assert.Throws<DomainException>(() => ticket.Reopen(Now.AddDays(3)));
    }
}
