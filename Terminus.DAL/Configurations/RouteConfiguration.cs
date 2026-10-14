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
        });

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.Code).IsRequired();
        builder.Property(e => e.RouteType).HasDefaultValue((short)3).ValueGeneratedNever();
        builder.Property(e => e.Color).HasColumnType("char(6)");
        builder.Property(e => e.TextColor).HasColumnType("char(6)");
        builder.Property(e => e.IsActive).HasDefaultValue(true).ValueGeneratedNever();

        builder.ConfigureAuditColumns();

        builder.HasOne(e => e.Agency)
            .WithMany(a => a.Routes)
            .HasForeignKey(e => e.AgencyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.AgencyId, e.Code })
            .IsUnique()
            .HasDatabaseName("uq_routes_agency_code");
    }
}
