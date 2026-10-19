using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Terminus.DAL.Entities;

namespace Terminus.DAL.Configurations;

public class SeasonDayConfiguration : IEntityTypeConfiguration<SeasonDay>
{
    public void Configure(EntityTypeBuilder<SeasonDay> builder)
    {
        builder.ToTable("season_days", t =>
        {
            t.HasCheckConstraint("ck_season_days_mode", "mode IN (0, 1, 2)");
            t.HasCheckConstraint("ck_season_days_source", "source IN (0, 1, 2)");
        });

        builder.HasKey(e => new { e.SeasonId, e.Date });

        builder.HasOne(e => e.Season)
            .WithMany(s => s.Days)
            .HasForeignKey(e => e.SeasonId)
            .OnDelete(DeleteBehavior.Cascade);

        // Brisanje tipa dana briše i dane koji su vozili kao on.
        builder.HasOne<ServiceCalendar>()
            .WithMany()
            .HasForeignKey(e => e.CalendarId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
