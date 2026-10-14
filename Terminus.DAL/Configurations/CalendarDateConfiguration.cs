using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Terminus.DAL.Entities;

namespace Terminus.DAL.Configurations;

public class CalendarDateConfiguration : IEntityTypeConfiguration<CalendarDate>
{
    public void Configure(EntityTypeBuilder<CalendarDate> builder)
    {
        builder.ToTable("calendar_dates", t =>
        {
            t.HasCheckConstraint("ck_calendar_dates_type", "exception_type IN (1, 2)");
        });

        builder.HasKey(e => new { e.ServiceCalendarId, e.Date });

        builder.HasOne(e => e.ServiceCalendar)
            .WithMany(c => c.CalendarDates)
            .HasForeignKey(e => e.ServiceCalendarId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.Date)
            .HasDatabaseName("ix_calendar_dates_date");
    }
}
