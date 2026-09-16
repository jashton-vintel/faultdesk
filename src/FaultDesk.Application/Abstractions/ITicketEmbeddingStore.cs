using FaultDesk.Domain.Tickets;

namespace FaultDesk.Application.Abstractions;

/// <summary>Stores the vector representation of a ticket. Vectors from different models are never compared with each other.</summary>
public interface ITicketEmbeddingStore
{
    Task UpsertAsync(TicketId ticketId, string modelId, ReadOnlyMemory<float> vector, CancellationToken cancellationToken);
}
