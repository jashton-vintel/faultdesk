using FaultDesk.Application.Abstractions;
using FaultDesk.Domain.Diagnostics;
using FaultDesk.Domain.Tickets;
using Microsoft.EntityFrameworkCore;

namespace FaultDesk.Infrastructure.Persistence.Repositories;

internal sealed class EfInvestigationRepository(IDbContextFactory<FaultDeskDbContext> factory) : IInvestigationRepository
{
    public async Task AddAsync(Investigation investigation, CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        db.Investigations.Add(investigation);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Investigation>> ListForTicketAsync(TicketId ticketId, CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.Investigations.AsNoTracking()
            .Where(i => i.TicketId == ticketId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
