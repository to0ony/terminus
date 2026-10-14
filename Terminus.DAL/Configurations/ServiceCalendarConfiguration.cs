using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Terminus.DAL.Entities;

namespace Terminus.DAL.Configurations;

public class ServiceCalendarConfiguration : IEntityTypeConfiguration<ServiceCalendar>
{
    public void Configure(EntityTypeBuilder<ServiceCalendar> builder)
    {
        builder.ToTable("service_calendars", t =>
        {
            t.HasCheckConstraint("ck_service_calendars_dates", "end_date >= start_date");
        });

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.Code).IsRequired();
        builder.Property(e => e.Name).IsRequired();
        builder.Property(e => e.Monday).HasDefaultValue(false);
        builder.Property(e => e.Tuesday).HasDefaultValue(false);
        builder.Property(e => e.Wednesday).HasDefaultValue(false);
        builder.Property(e => e.Thursday).HasDefaultValue(false);
        builder.Property(e => e.Friday).HasDefaultValue(false);
        builder.Property(e => e.Saturday).HasDefaultValue(false);
        builder.Property(e => e.Sunday).HasDefaultValue(false);

        builder.ConfigureAuditColumns();

        builder.HasOne(e => e.Agency)
            .WithMany(a => a.ServiceCalendars)
            .HasForeignKey(e => e.AgencyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.AgencyId, e.Code })
            .IsUnique()
            .HasDatabaseName("uq_service_calendars_agency_code");
    }
}
