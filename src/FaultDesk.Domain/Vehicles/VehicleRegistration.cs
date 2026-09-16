using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using FaultDesk.Domain.Common;

namespace FaultDesk.Domain.Vehicles;

/// <summary>
/// A UK vehicle registration mark. Stored normalised (upper case, no spaces or hyphens) so that
/// "ab12 cde", "AB12-CDE" and "AB12CDE" all identify the same vehicle.
/// </summary>
public sealed partial record VehicleRegistration
{
    public const int MaxLength = 7;

    public string Value { get; }

    private VehicleRegistration(string value) => Value = value;

    public static VehicleRegistration Parse(string? input)
    {
        return TryParse(input, out var registration, out var error)
            ? registration
            : throw new DomainException(error);
    }

    public static bool TryParse(
        string? input,
        [NotNullWhen(true)] out VehicleRegistration? registration,
        out string error)
    {
        registration = null;
        error = string.Empty;

        var normalised = Normalise(input);
        if (normalised.Length == 0)
        {
            error = "Registration is required.";
            return false;
        }

        if (!AllowedCharacters().IsMatch(normalised))
        {
            error = "Registration must be 2 to 7 letters and digits.";
            return false;
        }

        if (!normalised.Any(char.IsLetter) || !normalised.Any(char.IsDigit))
        {
            error = "Registration must contain both letters and digits.";
            return false;
        }

        registration = new VehicleRegistration(normalised);
        return true;
    }

    public static string Normalise(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        return new string(input.Where(c => c != ' ' && c != '-').ToArray()).ToUpperInvariant();
    }

    /// <summary>Human-friendly form with the conventional space, e.g. "AB12 CDE".</summary>
    public string Display
    {
        get
        {
            if (CurrentFormat().IsMatch(Value))
            {
                return $"{Value[..4]} {Value[4..]}";
            }

            if (PrefixFormat().IsMatch(Value))
            {
                return $"{Value[..^3]} {Value[^3..]}";
            }

            if (SuffixFormat().IsMatch(Value))
            {
                return $"{Value[..3]} {Value[3..]}";
            }

            return Value;
        }
    }

    public override string ToString() => Display;

    [GeneratedRegex("^[A-Z0-9]{2,7}$")]
    private static partial Regex AllowedCharacters();

    [GeneratedRegex("^[A-Z]{2}[0-9]{2}[A-Z]{3}$")]
    private static partial Regex CurrentFormat();

    [GeneratedRegex("^[A-Z][0-9]{1,3}[A-Z]{3}$")]
    private static partial Regex PrefixFormat();

    [GeneratedRegex("^[A-Z]{3}[0-9]{1,3}[A-Z]$")]
    private static partial Regex SuffixFormat();
}
