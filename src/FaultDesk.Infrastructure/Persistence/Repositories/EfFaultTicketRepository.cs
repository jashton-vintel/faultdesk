using FaultDesk.Application.Abstractions;
using FaultDesk.Domain.Tickets;
using FaultDesk.Domain.Vehicles;
using Microsoft.EntityFrameworkCore;

namespace FaultDesk.Infrastructure.Persistence.Repositories;

/// <summary>
/// One DbContext per operation (via the factory) because Blazor Server circuits are long-lived and a
/// circuit-scoped context would accumulate tracked state across requests.
/// </summary>
internal sealed class EfFaultTicketRepository(IDbContextFactory<FaultDeskDbContext> factory) : IFaultTicketRepository
{
    public async Task AddAsync(FaultTicket ticket, CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(FaultTicket ticket, CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        db.Tickets.Update(ticket);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<FaultTicket?> GetAsync(TicketId id, CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.Tickets.AsNoTracking().SingleOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<FaultTicket>> ListAsync(CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.Tickets.AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FaultTicket>> ListByRegistrationAsync(VehicleRegistration registration, CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.Tickets.AsNoTracking()
            .Where(t => t.Registration == registration)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
