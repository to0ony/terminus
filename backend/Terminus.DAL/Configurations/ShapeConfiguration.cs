using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Terminus.DAL.Entities;

namespace Terminus.DAL.Configurations;

public class ShapeConfiguration : IEntityTypeConfiguration<Shape>
{
    public void Configure(EntityTypeBuilder<Shape> builder)
    {
        builder.ToTable("shapes");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.Code).IsRequired();

        builder.ConfigureAuditColumns();

        builder.HasIndex(e => e.Code)
            .IsUnique()
            .HasDatabaseName("uq_shapes_code");
    }
}

public class ShapePointConfiguration : IEntityTypeConfiguration<ShapePoint>
{
    public void Configure(EntityTypeBuilder<ShapePoint> builder)
    {
        builder.ToTable("shape_points", t =>
        {
            t.HasCheckConstraint("ck_shape_points_sequence", "sequence >= 0");
            t.HasCheckConstraint("ck_shape_points_latitude", "latitude BETWEEN -90 AND 90");
            t.HasCheckConstraint("ck_shape_points_longitude", "longitude BETWEEN -180 AND 180");
            t.HasCheckConstraint("ck_shape_points_dist", "dist_traveled IS NULL OR dist_traveled >= 0");
        });

        builder.HasKey(e => new { e.ShapeId, e.Sequence });

        // Ključ je zadan iz aplikacije, ne generira se.
        builder.Property(e => e.Sequence).ValueGeneratedNever();

        builder.HasOne(e => e.Shape)
            .WithMany(s => s.Points)
            .HasForeignKey(e => e.ShapeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
