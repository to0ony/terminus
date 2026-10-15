using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Terminus.DAL.Entities;

namespace Terminus.DAL.Configurations;

public class TripConfiguration : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> builder)
    {
        builder.ToTable("trips", t =>
        {
            t.HasCheckConstraint("ck_trips_direction", "direction_id IS NULL OR direction_id IN (0, 1)");
            t.HasCheckConstraint("ck_trips_wheelchair", "wheelchair_accessible IN (0, 1, 2)");
            t.HasCheckConstraint("ck_trips_bikes", "bikes_allowed IN (0, 1, 2)");
        });

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.Code).IsRequired();
        builder.Property(e => e.WheelchairAccessible).HasDefaultValue((short)0);
        builder.Property(e => e.BikesAllowed).HasDefaultValue((short)0);

        builder.ConfigureAuditColumns();

        builder.HasOne(e => e.Route)
            .WithMany(r => r.Trips)
            .HasForeignKey(e => e.RouteId)
            .OnDelete(DeleteBehavior.Cascade);

        // NO ACTION (kao u shemi): kalendar koji koriste vožnje ne može se obrisati.
        builder.HasOne(e => e.ServiceCalendar)
            .WithMany(c => c.Trips)
            .HasForeignKey(e => e.ServiceCalendarId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(e => e.Code)
            .IsUnique()
            .HasDatabaseName("uq_trips_code");

        builder.HasIndex(e => e.RouteId)
            .HasDatabaseName("ix_trips_route_id");

        builder.HasIndex(e => e.ServiceCalendarId)
            .HasDatabaseName("ix_trips_service_calendar_id");
    }
}
