using FaultDesk.Domain.Common;

namespace FaultDesk.Domain.Tickets;

/// <summary>What the workshop actually found and did. Feeds future similar-ticket suggestions.</summary>
public sealed record Resolution
{
    public string Notes { get; }
    public DateTimeOffset ResolvedAt { get; }

    private Resolution(string notes, DateTimeOffset resolvedAt)
    {
        Notes = notes;
        ResolvedAt = resolvedAt;
    }

    public static Resolution Create(string? notes, DateTimeOffset resolvedAt) =>
        new(Guard.Required(notes, "Resolution notes", 2000), resolvedAt);
}
