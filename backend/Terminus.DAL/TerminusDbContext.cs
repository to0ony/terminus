using Microsoft.EntityFrameworkCore;
using Terminus.DAL.Entities;

namespace Terminus.DAL;

public class TerminusDbContext(DbContextOptions<TerminusDbContext> options) : DbContext(options)
{
    public DbSet<Agency> Agencies => Set<Agency>();
    public DbSet<Stop> Stops => Set<Stop>();
    public DbSet<Route> Routes => Set<Route>();
    public DbSet<ServiceCalendar> ServiceCalendars => Set<ServiceCalendar>();
    public DbSet<CalendarDate> CalendarDates => Set<CalendarDate>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<StopTime> StopTimes => Set<StopTime>();
    public DbSet<Shape> Shapes => Set<Shape>();
    public DbSet<ShapePoint> ShapePoints => Set<ShapePoint>();
    public DbSet<Fare> Fares => Set<Fare>();
    public DbSet<FareRule> FareRules => Set<FareRule>();
    public DbSet<FeedInfo> FeedInfo => Set<FeedInfo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TerminusDbContext).Assembly);
    }
}
