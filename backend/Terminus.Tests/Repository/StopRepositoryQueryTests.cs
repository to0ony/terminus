using Microsoft.EntityFrameworkCore;
using Terminus.DAL;
using Terminus.Model.Common.Enums;
using Terminus.Model.Common.Filters;
using Terminus.Repository;

namespace Terminus.Tests.Repository;

/// <summary>
/// Provjerava samo da EF upiti znaju prevesti u SQL (inače bi pali tek pri izvršavanju).
/// ToQueryString ne otvara vezu, pa baza nije potrebna.
/// </summary>
public class StopRepositoryQueryTests
{
    [Fact]
    public void VariantsThroughQuery_TranslatesToSql()
    {
        using var context = CreateContext();

        var sql = StopRepository.VariantsThroughQuery(context, Guid.NewGuid()).ToQueryString();

        Assert.Contains("FROM route_variants", sql);
    }

    [Fact]
    public void TripsThroughQuery_TranslatesToSql()
    {
        using var context = CreateContext();

        var sql = StopRepository.TripsThroughQuery(context, Guid.NewGuid()).ToQueryString();

        Assert.Contains("FROM trips", sql);
    }

    [Theory]
    [InlineData(StopSortField.Name, false)]
    [InlineData(StopSortField.PublicCode, true)]
    [InlineData(StopSortField.RouteCount, false)]
    public void TableQuery_WithAllFilters_TranslatesToSql(StopSortField sortBy, bool descending)
    {
        using var context = CreateContext();
        var filter = new StopFilter
        {
            Search = "kolodvor",
            IsActive = true,
            WheelchairBoarding = WheelchairBoarding.Accessible,
            HasTrips = true,
            HasCoordinates = false,
            RouteId = Guid.NewGuid(),
            SortBy = sortBy,
            SortDescending = descending
        };

        var sql = StopRepository
            .SortedQuery(context, StopRepository.FilteredQuery(context, filter), filter)
            .ToQueryString();

        Assert.Contains("FROM stops", sql);
        Assert.Contains("ORDER BY", sql);
    }

    [Fact]
    public void RoutesAtQuery_TranslatesToSql()
    {
        using var context = CreateContext();

        var sql = StopRepository.RoutesAtQuery(context, [Guid.NewGuid()]).ToQueryString();

        Assert.Contains("DISTINCT", sql);
    }

    private static TerminusDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<TerminusDbContext>()
            .UseNpgsql("Host=localhost;Database=terminus_query_translation")
            .UseSnakeCaseNamingConvention()
            .Options);
}
