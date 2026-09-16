using FaultDesk.Domain.Diagnostics;
using FaultDesk.Domain.Tickets;
using FaultDesk.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FaultDesk.Infrastructure.Persistence;

public sealed class FaultDeskDbContext(DbContextOptions<FaultDeskDbContext> options) : DbContext(options)
{
    public DbSet<FaultTicket> Tickets => Set<FaultTicket>();

    public DbSet<TicketEmbedding> TicketEmbeddings => Set<TicketEmbedding>();

    public DbSet<Investigation> Investigations => Set<Investigation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FaultDeskDbContext).Assembly);
    }
}
