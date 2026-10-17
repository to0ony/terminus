using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Terminus.DAL.Entities;

namespace Terminus.DAL.Configurations;

public class RouteVariantStopConfiguration : IEntityTypeConfiguration<RouteVariantStop>
{
    public void Configure(EntityTypeBuilder<RouteVariantStop> builder)
    {
        builder.ToTable("route_variant_stops", t =>
        {
            t.HasCheckConstraint("ck_route_variant_stops_times", "travel_time >= 0 AND dwell_time >= 0");
        });

        builder.HasKey(e => new { e.VariantId, e.Sequence });

        builder.HasOne(e => e.Variant)
            .WithMany(v => v.Stops)
            .HasForeignKey(e => e.VariantId)
            .OnDelete(DeleteBehavior.Cascade);

        // Stanica koju koristi varijanta ne može se obrisati.
        builder.HasOne(e => e.Stop)
            .WithMany()
            .HasForeignKey(e => e.StopId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(e => e.StopId)
            .HasDatabaseName("ix_route_variant_stops_stop_id");
    }
}
