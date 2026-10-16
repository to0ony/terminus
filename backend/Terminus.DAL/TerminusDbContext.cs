using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Terminus.DAL.Entities;

namespace Terminus.DAL;

/// <summary>
/// Vozni red, administratori (Identity, bez uloga) i ključevi za zaštitu kolačića
/// (Data Protection), da prijava preživi restart i više instanci aplikacije.
/// </summary>
public class TerminusDbContext(DbContextOptions<TerminusDbContext> options)
    : IdentityUserContext<AdminUser, Guid>(options), IDataProtectionKeyContext
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
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Identity shema; konfiguracije ispod mijenjaju nazive tablica i indeksa.
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TerminusDbContext).Assembly);
    }
}
