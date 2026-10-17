using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Terminus.DAL.Entities;

namespace Terminus.DAL.Configurations;

public class RouteVariantConfiguration : IEntityTypeConfiguration<RouteVariant>
{
    public void Configure(EntityTypeBuilder<RouteVariant> builder)
    {
        builder.ToTable("route_variants", t =>
        {
            t.HasCheckConstraint("ck_route_variants_direction", "direction_id IS NULL OR direction_id IN (0, 1)");
            t.HasCheckConstraint("ck_route_variants_source", "source IN (0, 1)");
        });

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.Name).IsRequired();

        builder.ConfigureAuditColumns();

        builder.HasOne(e => e.Route)
            .WithMany()
            .HasForeignKey(e => e.RouteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Shape)
            .WithMany()
            .HasForeignKey(e => e.ShapeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.RouteId)
            .HasDatabaseName("ix_route_variants_route_id");

        builder.HasIndex(e => e.ShapeId)
            .HasDatabaseName("ix_route_variants_shape_id");
    }
}
