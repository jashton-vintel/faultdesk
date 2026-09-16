using FaultDesk.Domain.Diagnostics;
using FaultDesk.Domain.Tickets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FaultDesk.Infrastructure.Persistence.Configurations;

internal sealed class InvestigationConfiguration : IEntityTypeConfiguration<Investigation>
{
    public void Configure(EntityTypeBuilder<Investigation> builder)
    {
        builder.ToTable("Investigations");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.TicketId)
            .HasConversion(id => id.Value, value => new TicketId(value));
        builder.HasIndex(i => i.TicketId);
        builder.HasOne<FaultTicket>()
            .WithMany()
            .HasForeignKey(i => i.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(i => i.ReportMarkdown).IsRequired();

        builder.OwnsOne(i => i.Metadata, metadata =>
        {
            metadata.Property(m => m.Provider).HasColumnName("AiProvider").HasMaxLength(50);
            metadata.Property(m => m.Model).HasColumnName("AiModel").HasMaxLength(100);
            metadata.Property(m => m.PromptVersion).HasColumnName("PromptVersion").HasMaxLength(50);
            metadata.Property(m => m.InputTokens).HasColumnName("InputTokens");
            metadata.Property(m => m.OutputTokens).HasColumnName("OutputTokens");
            metadata.Property(m => m.Duration)
                .HasColumnName("DurationTicks")
                .HasConversion(d => d.Ticks, ticks => TimeSpan.FromTicks(ticks));
            metadata.Property(m => m.WebSearchUsed).HasColumnName("WebSearchUsed");
        });
        builder.Navigation(i => i.Metadata).IsRequired();

        builder.Property(i => i.CreatedAt);
    }
}
