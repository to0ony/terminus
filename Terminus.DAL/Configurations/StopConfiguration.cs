using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Terminus.DAL.Entities;

namespace Terminus.DAL.Configurations;

public class StopConfiguration : IEntityTypeConfiguration<Stop>
{
    public void Configure(EntityTypeBuilder<Stop> builder)
    {
        builder.ToTable("stops", t =>
        {
            t.HasCheckConstraint("ck_stops_latitude", "latitude BETWEEN -90 AND 90");
            t.HasCheckConstraint("ck_stops_longitude", "longitude BETWEEN -180 AND 180");
            t.HasCheckConstraint("ck_stops_location_type", "location_type IN (0, 1)");
            t.HasCheckConstraint("ck_stops_wheelchair", "wheelchair_boarding IN (0, 1, 2)");
            t.HasCheckConstraint("ck_stops_not_own_parent", "parent_station_id IS NULL OR parent_station_id <> id");
        });

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.Code).IsRequired();
        builder.Property(e => e.Name).IsRequired();
        builder.Property(e => e.LocationType).HasDefaultValue((short)0).ValueGeneratedNever();
        builder.Property(e => e.WheelchairBoarding).HasDefaultValue((short)0).ValueGeneratedNever();
        builder.Property(e => e.IsActive).HasDefaultValue(true).ValueGeneratedNever();

        builder.ConfigureAuditColumns();

        builder.HasOne(e => e.Agency)
            .WithMany(a => a.Stops)
            .HasForeignKey(e => e.AgencyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.ParentStation)
            .WithMany(p => p.ChildStops)
            .HasForeignKey(e => e.ParentStationId)
            .OnDelete(DeleteBehavior.SetNull);

        // Pokriva i upite po agency_id (prvi stupac indeksa).
        builder.HasIndex(e => new { e.AgencyId, e.Code })
            .IsUnique()
            .HasDatabaseName("uq_stops_agency_code");

        builder.HasIndex(e => e.ParentStationId)
            .HasDatabaseName("ix_stops_parent_station_id");
    }
}
