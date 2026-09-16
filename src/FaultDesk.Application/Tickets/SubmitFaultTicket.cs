using FaultDesk.Application.Abstractions;
using FaultDesk.Domain.Tickets;
using FaultDesk.Domain.Vehicles;
using Microsoft.Extensions.Logging;

namespace FaultDesk.Application.Tickets;

public sealed record VehicleInput(string? Make, string? Model, string? Variant, int Year, int? EngineSizeCc, FuelType FuelType);

public sealed record SubmitFaultTicketCommand(
    string? Registration,
    VehicleInput Vehicle,
    string? Description,
    string? CustomerName,
    string? CustomerContact);

public sealed record SubmitFaultTicketResult(TicketId TicketId, string Reference, TriageSummary? Triage);

/// <summary>
/// Customer submits a fault: validate, triage with AI (best-effort), persist, embed for similarity search (best-effort).
/// Invalid input surfaces as a <see cref="FaultDesk.Domain.Common.DomainException"/>; AI problems never block the submission.
/// </summary>
public sealed class SubmitFaultTicketHandler(
    IFaultTicketRepository tickets,
    IFaultTriageService triage,
    TicketEmbeddingUpdater embeddingUpdater,
    TimeProvider timeProvider,
    ILogger<SubmitFaultTicketHandler> logger)
{
    public async Task<SubmitFaultTicketResult> HandleAsync(SubmitFaultTicketCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var registration = VehicleRegistration.Parse(command.Registration);
        var input = command.Vehicle;
        var vehicle = VehicleDetails.Create(input.Make, input.Model, input.Variant, input.Year, input.EngineSizeCc, input.FuelType);
        var now = timeProvider.GetUtcNow();

        var ticket = FaultTicket.Create(registration, vehicle, command.Description, command.CustomerName, command.CustomerContact, now);

        var summary = await TryTriageAsync(vehicle, ticket.Description, cancellationToken);
        if (summary is not null)
        {
            ticket.ApplyTriage(summary, now);
        }

        await tickets.AddAsync(ticket, cancellationToken);
        logger.LogInformation("Ticket {Reference} submitted for {Registration} ({Vehicle})", ticket.Reference, registration.Display, vehicle.Summary);

        await embeddingUpdater.TryEmbedAsync(ticket, cancellationToken);

        return new SubmitFaultTicketResult(ticket.Id, ticket.Reference, ticket.Triage);
    }

    private async Task<TriageSummary?> TryTriageAsync(VehicleDetails vehicle, string description, CancellationToken cancellationToken)
    {
        try
        {
            return await triage.TriageAsync(vehicle, description, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AI triage failed; the ticket will be stored without a triage summary");
            return null;
        }
    }
}
