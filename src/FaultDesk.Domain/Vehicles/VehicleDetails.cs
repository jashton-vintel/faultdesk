using FaultDesk.Domain.Common;

namespace FaultDesk.Domain.Vehicles;

/// <summary>What we know about the vehicle, whether looked up from a registration or typed by the customer.</summary>
public sealed record VehicleDetails
{
    public const int MinYear = 1900;

    public string Make { get; }
    public string Model { get; }
    public string? Variant { get; }
    public int Year { get; }
    public int? EngineSizeCc { get; }
    public FuelType FuelType { get; }

    private VehicleDetails(string make, string model, string? variant, int year, int? engineSizeCc, FuelType fuelType)
    {
        Make = make;
        Model = model;
        Variant = variant;
        Year = year;
        EngineSizeCc = engineSizeCc;
        FuelType = fuelType;
    }

    public static VehicleDetails Create(
        string? make,
        string? model,
        string? variant,
        int year,
        int? engineSizeCc,
        FuelType fuelType,
        int? currentYear = null)
    {
        var maxYear = (currentYear ?? DateTime.UtcNow.Year) + 1;
        if (year < MinYear || year > maxYear)
        {
            throw new DomainException($"Year must be between {MinYear} and {maxYear}.");
        }

        if (engineSizeCc is <= 0 or > 20_000)
        {
            throw new DomainException("Engine size must be a positive number of cc.");
        }

        return new VehicleDetails(
            Guard.Required(make, "Make", 60),
            Guard.Required(model, "Model", 60),
            Guard.Optional(variant, "Variant", 80),
            year,
            engineSizeCc,
            fuelType);
    }

    /// <summary>One-line description used in grids and prompts, e.g. "2016 Ford Focus 1.5 TDCi Zetec, 1499cc diesel".</summary>
    public string Summary
    {
        get
        {
            var headline = Variant is null ? $"{Year} {Make} {Model}" : $"{Year} {Make} {Model} {Variant}";

            var extras = new List<string>();
            if (EngineSizeCc is { } cc)
            {
                extras.Add($"{cc}cc");
            }

            if (FuelType != FuelType.Unknown)
            {
                extras.Add(FuelType.ToString().ToLowerInvariant());
            }

            return extras.Count == 0 ? headline : $"{headline}, {string.Join(" ", extras)}";
        }
    }
}
