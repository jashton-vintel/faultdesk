using FaultDesk.Domain.Vehicles;

namespace FaultDesk.Application.Abstractions;

public interface IVehicleLookupService
{
    /// <summary>Returns the vehicle for a registration, or null when it is not known so the customer can enter details manually.</summary>
    Task<VehicleDetails?> LookupAsync(VehicleRegistration registration, CancellationToken cancellationToken);

    /// <summary>Registrations this provider can resolve, shown as hints in demo mode. Empty for real providers.</summary>
    IReadOnlyList<VehicleRegistration> SampleRegistrations { get; }
}
