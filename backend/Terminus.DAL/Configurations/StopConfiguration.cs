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
            // GTFS: koordinate su obavezne osim za čvorove (3) i mjesta ukrcavanja (4).
            t.HasCheckConstraint("ck_stops_coordinates",
                "location_type IN (3, 4) OR (latitude IS NOT NULL AND longitude IS NOT NULL)");
            t.HasCheckConstraint("ck_stops_location_type", "location_type IN (0, 1, 2, 3, 4)");
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

        builder.HasOne(e => e.ParentStation)
            .WithMany(p => p.ChildStops)
            .HasForeignKey(e => e.ParentStationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.Code)
            .IsUnique()
            .HasDatabaseName("uq_stops_code");

        builder.HasIndex(e => e.ParentStationId)
            .HasDatabaseName("ix_stops_parent_station_id");
    }
}
