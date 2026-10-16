using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Terminus.DAL.Entities;

namespace Terminus.DAL.Configurations;

public class FareConfiguration : IEntityTypeConfiguration<Fare>
{
    public void Configure(EntityTypeBuilder<Fare> builder)
    {
        builder.ToTable("fares", t =>
        {
            t.HasCheckConstraint("ck_fares_price", "price >= 0");
            t.HasCheckConstraint("ck_fares_currency", "currency_type ~ '^[A-Z]{3}$'");
            t.HasCheckConstraint("ck_fares_payment_method", "payment_method IN (0, 1)");
            t.HasCheckConstraint("ck_fares_transfers", "transfers IS NULL OR transfers IN (0, 1, 2)");
            t.HasCheckConstraint("ck_fares_transfer_duration", "transfer_duration IS NULL OR transfer_duration >= 0");
        });

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.Code).IsRequired();

        // Iznosi u valutama s do 4 decimale; GTFS ne propisuje preciznost.
        builder.Property(e => e.Price).HasPrecision(12, 4);
        builder.Property(e => e.CurrencyType).IsRequired().HasColumnType("char(3)");

        builder.ConfigureAuditColumns();

        // Prijevoznik s cijenama ne može se obrisati.
        builder.HasOne(e => e.Agency)
            .WithMany(a => a.Fares)
            .HasForeignKey(e => e.AgencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.Code)
            .IsUnique()
            .HasDatabaseName("uq_fares_code");

        builder.HasIndex(e => e.AgencyId)
            .HasDatabaseName("ix_fares_agency_id");
    }
}

public class FareRuleConfiguration : IEntityTypeConfiguration<FareRule>
{
    public void Configure(EntityTypeBuilder<FareRule> builder)
    {
        builder.ToTable("fare_rules");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.HasOne(e => e.Fare)
            .WithMany(f => f.Rules)
            .HasForeignKey(e => e.FareId)
            .OnDelete(DeleteBehavior.Cascade);

        // Brisanje linije briše i pravila koja se na nju odnose.
        builder.HasOne(e => e.Route)
            .WithMany(r => r.FareRules)
            .HasForeignKey(e => e.RouteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.FareId, e.Sequence })
            .IsUnique()
            .HasDatabaseName("uq_fare_rules_fare_sequence");

        builder.HasIndex(e => e.RouteId)
            .HasDatabaseName("ix_fare_rules_route_id");
    }
}
