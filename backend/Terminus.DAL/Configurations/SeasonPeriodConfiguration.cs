using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Terminus.DAL.Entities;

namespace Terminus.DAL.Configurations;

public class SeasonPeriodConfiguration : IEntityTypeConfiguration<SeasonPeriod>
{
    public void Configure(EntityTypeBuilder<SeasonPeriod> builder)
    {
        builder.ToTable("season_periods", t =>
        {
            t.HasCheckConstraint("ck_season_periods_dates", "end_date >= start_date");
            t.HasCheckConstraint("ck_season_periods_mode", "mode IN (1, 2)");
        });

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.Name).IsRequired();

        builder.HasOne(e => e.Season)
            .WithMany(s => s.Periods)
            .HasForeignKey(e => e.SeasonId)
            .OnDelete(DeleteBehavior.Cascade);

        // Brisanje tipa dana briše i razdoblja koja ga spominju.
        builder.HasOne<ServiceCalendar>()
            .WithMany()
            .HasForeignKey(e => e.ReplacedCalendarId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ServiceCalendar>()
            .WithMany()
            .HasForeignKey(e => e.CalendarId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
