using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Terminus.DAL.Entities;

namespace Terminus.DAL.Configurations;

public class StopTimeConfiguration : IEntityTypeConfiguration<StopTime>
{
    public void Configure(EntityTypeBuilder<StopTime> builder)
    {
        builder.ToTable("stop_times", t =>
        {
            t.HasCheckConstraint("ck_stop_times_sequence", "stop_sequence >= 0");
            t.HasCheckConstraint("ck_stop_times_arrival", "arrival_time BETWEEN 0 AND 172800");
            t.HasCheckConstraint("ck_stop_times_departure", "departure_time BETWEEN 0 AND 172800");
            t.HasCheckConstraint("ck_stop_times_order", "departure_time >= arrival_time");
            t.HasCheckConstraint("ck_stop_times_pickup", "pickup_type IN (0, 1, 2, 3)");
            t.HasCheckConstraint("ck_stop_times_dropoff", "drop_off_type IN (0, 1, 2, 3)");
        });

        builder.HasKey(e => new { e.TripId, e.StopSequence });

        // Ključ je zadan iz aplikacije, ne generira se.
        builder.Property(e => e.StopSequence).ValueGeneratedNever();

        builder.Property(e => e.PickupType).HasDefaultValue((short)0);
        builder.Property(e => e.DropOffType).HasDefaultValue((short)0);
        builder.Property(e => e.Timepoint).HasDefaultValue(true).ValueGeneratedNever();

        builder.HasOne(e => e.Trip)
            .WithMany(t => t.StopTimes)
            .HasForeignKey(e => e.TripId)
            .OnDelete(DeleteBehavior.Cascade);

        // NO ACTION (kao u shemi): stanica koja se koristi u voznom redu ne može se obrisati.
        builder.HasOne(e => e.Stop)
            .WithMany(s => s.StopTimes)
            .HasForeignKey(e => e.StopId)
            .OnDelete(DeleteBehavior.NoAction);

        // Najčešći upit: polasci sa stanice poredani po vremenu.
        // Pokriva i FK stop_id, pa EF ne stvara zaseban indeks.
        builder.HasIndex(e => new { e.StopId, e.DepartureTime })
            .HasDatabaseName("ix_stop_times_stop_departure");
    }
}
