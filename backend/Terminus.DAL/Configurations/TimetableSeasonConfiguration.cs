using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Terminus.DAL.Entities;

namespace Terminus.DAL.Configurations;

public class TimetableSeasonConfiguration : IEntityTypeConfiguration<TimetableSeason>
{
    public void Configure(EntityTypeBuilder<TimetableSeason> builder)
    {
        builder.ToTable("timetable_seasons", t =>
        {
            t.HasCheckConstraint("ck_timetable_seasons_dates", "end_date >= start_date");
            t.HasCheckConstraint("ck_timetable_seasons_holiday_mode", "holiday_mode IN (0, 1, 2)");
        });

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.Name).IsRequired();
        builder.Property(e => e.HolidayCountry).HasMaxLength(2);

        builder.HasOne<ServiceCalendar>()
            .WithMany()
            .HasForeignKey(e => e.HolidayCalendarId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.ConfigureAuditColumns();
    }
}
