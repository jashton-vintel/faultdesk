using FaultDesk.Domain.Tickets;
using FaultDesk.Domain.Vehicles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FaultDesk.Infrastructure.Persistence.Configurations;

internal sealed class FaultTicketConfiguration : IEntityTypeConfiguration<FaultTicket>
{
    public void Configure(EntityTypeBuilder<FaultTicket> builder)
    {
        builder.ToTable("FaultTickets");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasConversion(id => id.Value, value => new TicketId(value))
            .ValueGeneratedNever();

        builder.Property(t => t.Reference).HasMaxLength(20).IsRequired();
        builder.HasIndex(t => t.Reference).IsUnique();

        builder.Property(t => t.Registration)
            .HasConversion(r => r.Value, value => VehicleRegistration.Parse(value))
            .HasMaxLength(VehicleRegistration.MaxLength)
            .IsRequired();
        builder.HasIndex(t => t.Registration);

        builder.OwnsOne(t => t.Vehicle, vehicle =>
        {
            vehicle.Property(v => v.Make).HasColumnName("VehicleMake").HasMaxLength(60).IsRequired();
            vehicle.Property(v => v.Model).HasColumnName("VehicleModel").HasMaxLength(60).IsRequired();
            vehicle.Property(v => v.Variant).HasColumnName("VehicleVariant").HasMaxLength(80);
            vehicle.Property(v => v.Year).HasColumnName("VehicleYear");
            vehicle.Property(v => v.EngineSizeCc).HasColumnName("VehicleEngineSizeCc");
            vehicle.Property(v => v.FuelType).HasColumnName("VehicleFuelType").HasConversion<string>().HasMaxLength(20);
        });
        builder.Navigation(t => t.Vehicle).IsRequired();

        builder.Property(t => t.Description).HasMaxLength(FaultTicket.MaxDescriptionLength).IsRequired();
        builder.Property(t => t.CustomerName).HasMaxLength(100);
        builder.Property(t => t.CustomerContact).HasMaxLength(200);

        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(t => t.Status);

        builder.OwnsOne(t => t.Triage, triage =>
        {
            triage.Property(x => x.Title).HasColumnName("TriageTitle").HasMaxLength(120);
            triage.Property(x => x.Category).HasColumnName("TriageCategory").HasConversion<string>().HasMaxLength(30);
            triage.Property(x => x.Severity).HasColumnName("TriageSeverity").HasConversion<string>().HasMaxLength(20);
            triage.Property(x => x.SafeToDrive).HasColumnName("TriageSafeToDrive").HasConversion<string>().HasMaxLength(20);
            triage.Property(x => x.Symptoms).HasColumnName("TriageSymptoms");
            triage.Property(x => x.LikelySystems).HasColumnName("TriageLikelySystems");
            triage.Property(x => x.AdviserQuestions).HasColumnName("TriageAdviserQuestions");
            triage.Property(x => x.CustomerSummary).HasColumnName("TriageCustomerSummary").HasMaxLength(500);
        });

        builder.OwnsOne(t => t.Resolution, resolution =>
        {
            resolution.Property(x => x.Notes).HasColumnName("ResolutionNotes").HasMaxLength(2000);
            resolution.Property(x => x.ResolvedAt).HasColumnName("ResolvedAt");
        });

        builder.Property(t => t.CreatedAt);
        builder.HasIndex(t => t.CreatedAt);
        builder.Property(t => t.UpdatedAt);
    }
}
