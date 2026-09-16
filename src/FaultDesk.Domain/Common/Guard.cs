namespace FaultDesk.Domain.Common;

internal static class Guard
{
    public static string Required(string? value, string name, int maxLength = 200)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{name} is required.");
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength
            ? trimmed
            : throw new DomainException($"{name} must be {maxLength} characters or fewer.");
    }

    public static string? Optional(string? value, string name, int maxLength = 200)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength
            ? trimmed
            : throw new DomainException($"{name} must be {maxLength} characters or fewer.");
    }
}
