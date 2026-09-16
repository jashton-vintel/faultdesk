using FaultDesk.Domain.Tickets;
using Microsoft.Data.SqlTypes;

namespace FaultDesk.Infrastructure.Persistence.Entities;

/// <summary>
/// Infrastructure-owned vector for a ticket (see ADR-0001). Lives in its own table so the domain never sees
/// <see cref="SqlVector{T}"/>, so the 6 KB vector is not loaded with every ticket, and so vectors from different
/// embedding models are never compared with each other.
/// </summary>
public sealed class TicketEmbedding
{
    /// <summary>Fixed by the column type; must match the configured embedding service.</summary>
    public const int Dimensions = 1536;

    public TicketId TicketId { get; set; }

    public string ModelId { get; set; } = null!;

    public SqlVector<float> Vector { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public FaultTicket Ticket { get; set; } = null!;
}
