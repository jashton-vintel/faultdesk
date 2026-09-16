using FaultDesk.Domain.Tickets;
using FaultDesk.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FaultDesk.Infrastructure.Persistence.Configurations;

internal sealed class TicketEmbeddingConfiguration : IEntityTypeConfiguration<TicketEmbedding>
{
    public void Configure(EntityTypeBuilder<TicketEmbedding> builder)
    {
        builder.ToTable("TicketEmbeddings");

        builder.HasKey(e => e.TicketId);
        builder.Property(e => e.TicketId)
            .HasConversion(id => id.Value, value => new TicketId(value))
            .ValueGeneratedNever();

        builder.HasOne(e => e.Ticket)
            .WithOne()
            .HasForeignKey<TicketEmbedding>(e => e.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(e => e.ModelId).HasMaxLength(100).IsRequired();
        builder.HasIndex(e => e.ModelId);

        // SQL Server 2025 native vector type; VECTOR_DISTANCE requires both operands to have this dimension.
        builder.Property(e => e.Vector).HasColumnType($"vector({TicketEmbedding.Dimensions})").IsRequired();

        builder.Property(e => e.CreatedAt);
    }
}
