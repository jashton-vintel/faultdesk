using FaultDesk.Domain.Tickets;
using FaultDesk.Domain.Vehicles;

namespace FaultDesk.Web;

/// <summary>Human-friendly labels and Bootstrap colours for domain enums, shared by both portals.</summary>
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

    public static string StatusColour(TicketStatus status) => status switch
    {
        TicketStatus.New => "primary",
        TicketStatus.InProgress => "warning text-dark",
        TicketStatus.Resolved => "success",
        _ => "secondary",
    };

    public static string Severity(Severity severity) => severity == Domain.Tickets.Severity.Unknown ? "Unrated" : severity.ToString();

    public static string SeverityColour(Severity severity) => severity switch
    {
        Domain.Tickets.Severity.High => "danger",
        Domain.Tickets.Severity.Medium => "warning text-dark",
        Domain.Tickets.Severity.Low => "success",
        _ => "secondary",
    };

    public static string SafeToDrive(SafeToDrive safeToDrive) => safeToDrive switch
    {
        Domain.Tickets.SafeToDrive.Yes => "Should be safe to drive",
        Domain.Tickets.SafeToDrive.Caution => "Drive with caution",
        Domain.Tickets.SafeToDrive.No => "Do not drive",
        _ => "Not assessed",
    };

    public static string SafeToDriveColour(SafeToDrive safeToDrive) => safeToDrive switch
    {
        Domain.Tickets.SafeToDrive.Yes => "success",
        Domain.Tickets.SafeToDrive.Caution => "warning text-dark",
        Domain.Tickets.SafeToDrive.No => "danger",
        _ => "secondary",
    };

    public static string Fuel(FuelType fuel) => fuel switch
    {
        FuelType.Unknown => "Not sure",
        FuelType.PlugInHybrid => "Plug-in hybrid",
        FuelType.Lpg => "LPG",
        _ => fuel.ToString(),
    };

    public static string LocalTime(DateTimeOffset value) => value.ToLocalTime().ToString("d MMM yyyy HH:mm");
}
