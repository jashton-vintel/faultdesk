using FaultDesk.Domain.Tickets;
using FaultDesk.Domain.Vehicles;

namespace FaultDesk.Web;

/// <summary>Human-friendly labels and pill styles for domain enums, shared by both portals.</summary>
public static class Display
{
    public static string Category(FaultCategory category) => category switch
    {
        FaultCategory.Unknown => "Not yet categorised",
        FaultCategory.SteeringSuspension => "Steering & suspension",
        FaultCategory.ExhaustEmissions => "Exhaust & emissions",
        _ => category.ToString(),
    };

    public static string Status(TicketStatus status) => status switch
    {
        TicketStatus.InProgress => "In progress",
        _ => status.ToString(),
    };

    public static string StatusPill(TicketStatus status) => status switch
    {
        TicketStatus.New => "pill-blue",
        TicketStatus.InProgress => "pill-amber",
        TicketStatus.Resolved => "pill-green",
        _ => "pill-grey",
    };

    public static string Severity(Severity severity) =>
        severity == Domain.Tickets.Severity.Unknown ? "Unrated" : $"{severity} severity";

    public static string SeverityPill(Severity severity) => severity switch
    {
        Domain.Tickets.Severity.High => "pill-red",
        Domain.Tickets.Severity.Medium => "pill-amber",
        Domain.Tickets.Severity.Low => "pill-green",
        _ => "pill-grey",
    };

    public static string SafeToDrive(SafeToDrive safeToDrive) => safeToDrive switch
    {
        Domain.Tickets.SafeToDrive.Yes => "Safe to drive",
        Domain.Tickets.SafeToDrive.Caution => "Drive with caution",
        Domain.Tickets.SafeToDrive.No => "Do not drive",
        _ => "Not assessed",
    };

    public static string SafeToDrivePill(SafeToDrive safeToDrive) => safeToDrive switch
    {
        Domain.Tickets.SafeToDrive.Yes => "pill-green",
        Domain.Tickets.SafeToDrive.Caution => "pill-amber",
        Domain.Tickets.SafeToDrive.No => "pill-red",
        _ => "pill-grey",
    };

    public static string Fuel(FuelType fuel) => fuel switch
    {
        FuelType.Unknown => "Not sure",
        FuelType.PlugInHybrid => "Plug-in hybrid",
        FuelType.Lpg => "LPG",
        _ => fuel.ToString(),
    };

    public static string LocalTime(DateTimeOffset value) => value.ToLocalTime().ToString("d MMM yyyy HH:mm");

    public static string LocalDate(DateTimeOffset value) => value.ToLocalTime().ToString("d MMM yyyy");
}
