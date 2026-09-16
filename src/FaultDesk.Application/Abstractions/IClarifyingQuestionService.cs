using FaultDesk.Domain.Vehicles;

namespace FaultDesk.Application.Abstractions;

/// <summary>Suggests a few follow-up questions a service adviser would ask before booking the car in.</summary>
public interface IClarifyingQuestionService
{
    Task<IReadOnlyList<string>> SuggestQuestionsAsync(VehicleDetails vehicle, string description, CancellationToken cancellationToken);
}
