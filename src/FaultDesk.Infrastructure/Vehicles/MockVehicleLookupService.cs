using FaultDesk.Application.Abstractions;
using FaultDesk.Domain.Vehicles;

namespace FaultDesk.Infrastructure.Vehicles;

/// <summary>
/// Stand-in for a registration lookup such as the DVLA Vehicle Enquiry Service. Knows a handful of registrations;
/// anything else is "not found" so the manual-entry path is exercised. Adds a little latency to feel like a real call.
/// </summary>
internal sealed class MockVehicleLookupService : IVehicleLookupService
{
    private static readonly TimeSpan SimulatedLatency = TimeSpan.FromMilliseconds(300);

    private static readonly Dictionary<VehicleRegistration, VehicleDetails> Vehicles = new (string Registration, string Make, string Model, string? Variant, int Year, int? EngineCc, FuelType Fuel)[]
    {
        ("AB12 CDE", "Ford", "Focus", "1.6 TDCi Zetec", 2012, 1560, FuelType.Diesel),
        ("KX19 HWL", "Volkswagen", "Golf", "1.5 TSI Evo Match", 2019, 1498, FuelType.Petrol),
        ("LM68 RTO", "Vauxhall", "Corsa", "1.4i SRi", 2018, 1398, FuelType.Petrol),
        ("BN17 PLX", "BMW", "320d", "M Sport", 2017, 1995, FuelType.Diesel),
        ("WR21 ZKD", "Toyota", "Yaris", "1.5 Hybrid Icon", 2021, 1490, FuelType.Hybrid),
        ("YF15 VNP", "Nissan", "Qashqai", "1.5 dCi Acenta", 2015, 1461, FuelType.Diesel),
        ("DG66 UJT", "Mercedes-Benz", "A180d", "Sport", 2016, 1461, FuelType.Diesel),
        ("PO70 EVQ", "Tesla", "Model 3", "Standard Range Plus", 2020, null, FuelType.Electric),
        ("SA09 MKW", "Honda", "Jazz", "1.4 i-VTEC ES", 2009, 1339, FuelType.Petrol),
        ("HJ13 XRC", "Land Rover", "Freelander 2", "2.2 SD4 HSE", 2013, 2179, FuelType.Diesel),
    }.ToDictionary(
        v => VehicleRegistration.Parse(v.Registration),
        v => VehicleDetails.Create(v.Make, v.Model, v.Variant, v.Year, v.EngineCc, v.Fuel));

    public IReadOnlyList<VehicleRegistration> SampleRegistrations { get; } = Vehicles.Keys.ToList();

    public async Task<VehicleDetails?> LookupAsync(VehicleRegistration registration, CancellationToken cancellationToken)
    {
        await Task.Delay(SimulatedLatency, cancellationToken);
        return Vehicles.GetValueOrDefault(registration);
    }
}
