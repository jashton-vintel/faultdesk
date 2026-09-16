using FaultDesk.Domain.Tickets;
using FaultDesk.Domain.Vehicles;

namespace FaultDesk.Application.Abstractions;

/// <summary>Reads a customer's free text and produces a structured summary for the service adviser.</summary>
public interface IFaultTriageService
{
    /// <summary>Returns null when the text could not be triaged. Implementations may throw; callers treat triage as best-effort.</summary>
    Task<TriageSummary?> TriageAsync(VehicleDetails vehicle, string description, CancellationToken cancellationToken);
}
