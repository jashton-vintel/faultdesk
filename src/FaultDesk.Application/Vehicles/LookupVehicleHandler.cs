using FaultDesk.Application.Abstractions;
using FaultDesk.Domain.Vehicles;

namespace FaultDesk.Application.Vehicles;

public sealed record VehicleLookupResult(VehicleRegistration Registration, VehicleDetails? Vehicle)
{
    public bool Found => Vehicle is not null;
}

/// <summary>Customer types a registration; we normalise it and try to find the vehicle. Not found is a normal outcome.</summary>
public sealed class LookupVehicleHandler(IVehicleLookupService lookup)
{
    public async Task<VehicleLookupResult> HandleAsync(string? registrationInput, CancellationToken cancellationToken)
    {
        var registration = VehicleRegistration.Parse(registrationInput);
        var vehicle = await lookup.LookupAsync(registration, cancellationToken);
        return new VehicleLookupResult(registration, vehicle);
    }

    public IReadOnlyList<VehicleRegistration> SampleRegistrations => lookup.SampleRegistrations;
}
