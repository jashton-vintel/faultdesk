using FaultDesk.Domain.Common;
using FaultDesk.Domain.Vehicles;

namespace FaultDesk.Domain.Tests.Vehicles;

public class VehicleDetailsTests
{
    [Fact]
    public void Create_trims_and_keeps_details()
    {
        var vehicle = VehicleDetails.Create(" Ford ", " Focus ", " 1.5 TDCi Zetec ", 2016, 1499, FuelType.Diesel, currentYear: 2026);

        Assert.Equal("Ford", vehicle.Make);
        Assert.Equal("Focus", vehicle.Model);
        Assert.Equal("1.5 TDCi Zetec", vehicle.Variant);
        Assert.Equal("2016 Ford Focus 1.5 TDCi Zetec, 1499cc diesel", vehicle.Summary);
    }

    [Fact]
    public void Summary_omits_unknown_parts()
    {
        var vehicle = VehicleDetails.Create("Tesla", "Model 3", null, 2021, null, FuelType.Electric, currentYear: 2026);

        Assert.Equal("2021 Tesla Model 3, electric", vehicle.Summary);
    }

    [Theory]
    [InlineData(null, "Focus")]
    [InlineData("Ford", "")]
    public void Create_requires_make_and_model(string? make, string? model)
    {
        Assert.Throws<DomainException>(() => VehicleDetails.Create(make, model, null, 2016, null, FuelType.Petrol, currentYear: 2026));
    }

    [Theory]
    [InlineData(1899)]
    [InlineData(2028)]
    public void Create_rejects_years_outside_the_plausible_range(int year)
    {
        Assert.Throws<DomainException>(() => VehicleDetails.Create("Ford", "Focus", null, year, null, FuelType.Petrol, currentYear: 2026));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(50_000)]
    public void Create_rejects_implausible_engine_sizes(int engineSizeCc)
    {
        Assert.Throws<DomainException>(() => VehicleDetails.Create("Ford", "Focus", null, 2016, engineSizeCc, FuelType.Petrol, currentYear: 2026));
    }
}
