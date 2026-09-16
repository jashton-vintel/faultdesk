using System.ComponentModel.DataAnnotations;
using FaultDesk.Application.Tickets;
using FaultDesk.Domain.Tickets;
using FaultDesk.Domain.Vehicles;

namespace FaultDesk.Web.Components.Customer;

/// <summary>Step 2 of the report form. Pre-filled from the registration lookup when we know the car.</summary>
public sealed class VehicleForm
{
    [Required(ErrorMessage = "Enter the make, e.g. Ford")]
    [StringLength(60)]
    public string Make { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter the model, e.g. Focus")]
    [StringLength(60)]
    public string Model { get; set; } = string.Empty;

    [StringLength(80)]
    public string? Variant { get; set; }

    [Range(1900, 2100, ErrorMessage = "Enter the year of first registration")]
    public int Year { get; set; } = DateTime.UtcNow.Year;

    [Range(1, 20000, ErrorMessage = "Enter the engine size in cc, e.g. 1598")]
    public int? EngineSizeCc { get; set; }

    public FuelType FuelType { get; set; } = FuelType.Unknown;

    [StringLength(100)]
    public string? CustomerName { get; set; }

    [StringLength(200)]
    public string? CustomerContact { get; set; }

    public void Apply(VehicleDetails vehicle)
    {
        Make = vehicle.Make;
        Model = vehicle.Model;
        Variant = vehicle.Variant;
        Year = vehicle.Year;
        EngineSizeCc = vehicle.EngineSizeCc;
        FuelType = vehicle.FuelType;
    }

    public void ClearVehicle()
    {
        Make = string.Empty;
        Model = string.Empty;
        Variant = null;
        Year = DateTime.UtcNow.Year;
        EngineSizeCc = null;
        FuelType = FuelType.Unknown;
    }

    public VehicleInput ToInput() => new(Make, Model, Variant, Year, EngineSizeCc, FuelType);

    public string Summary => string.Join(' ', new[] { Year.ToString(), Make, Model, Variant }.Where(s => !string.IsNullOrWhiteSpace(s)));
}

/// <summary>Step 3 of the report form.</summary>
public sealed class ProblemForm
{
    [Required(ErrorMessage = "Tell us what's wrong")]
    [StringLength(
        FaultTicket.MaxDescriptionLength,
        MinimumLength = FaultTicket.MinDescriptionLength,
        ErrorMessage = "Please give us at least a sentence (10 characters or more)")]
    public string Description { get; set; } = string.Empty;
}
