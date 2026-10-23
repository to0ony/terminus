using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Terminus.DAL.Entities;

namespace Terminus.DAL.Configurations;

public class RouteConfiguration : IEntityTypeConfiguration<Route>
{
    public void Configure(EntityTypeBuilder<Route> builder)
    {
        builder.ToTable("routes", t =>
        {
            t.HasCheckConstraint("ck_routes_name", "short_name IS NOT NULL OR long_name IS NOT NULL");
            t.HasCheckConstraint("ck_routes_color", "color IS NULL OR color ~ '^[0-9A-Fa-f]{6}$'");
            t.HasCheckConstraint("ck_routes_text_color", "text_color IS NULL OR text_color ~ '^[0-9A-Fa-f]{6}$'");
            t.HasCheckConstraint("ck_routes_continuous_pickup", "continuous_pickup IS NULL OR continuous_pickup IN (0, 1, 2, 3)");
            t.HasCheckConstraint("ck_routes_continuous_drop_off", "continuous_drop_off IS NULL OR continuous_drop_off IN (0, 1, 2, 3)");
            t.HasCheckConstraint("ck_routes_time_mode", "time_mode IN (0, 1)");
        });

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.Code).IsRequired();
        builder.Property(e => e.RouteType).HasDefaultValue((short)3).ValueGeneratedNever();
        builder.Property(e => e.Color).HasColumnType("char(6)");
        builder.Property(e => e.TextColor).HasColumnType("char(6)");
        builder.Property(e => e.TimeMode).HasDefaultValue((short)0).ValueGeneratedNever();
        builder.Property(e => e.IsActive).HasDefaultValue(true).ValueGeneratedNever();

        builder.ConfigureAuditColumns();

        // GTFS routes.agency_id: operater linije. Operater koji ima linije ne može se obrisati.
        builder.HasOne(e => e.Agency)
            .WithMany(a => a.Routes)
            .HasForeignKey(e => e.AgencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.Code)
            .IsUnique()
            .HasDatabaseName("uq_routes_code");

        builder.HasIndex(e => e.AgencyId)
            .HasDatabaseName("ix_routes_agency_id");
    }
}
