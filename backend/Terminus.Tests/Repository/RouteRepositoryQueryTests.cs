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
public class RouteRepositoryQueryTests
{
    [Theory]
    [InlineData(RouteSortField.Default, false)]
    [InlineData(RouteSortField.ShortName, true)]
    [InlineData(RouteSortField.Agency, false)]
    [InlineData(RouteSortField.VariantCount, true)]
    [InlineData(RouteSortField.ValidTo, false)]
    public void TableQuery_WithAllFilters_TranslatesToSql(RouteSortField sortBy, bool descending)
    {
        using var context = CreateContext();
        var filter = new RouteFilter
        {
            Search = "4",
            AgencyId = Guid.NewGuid(),
            IsActive = true,
            RouteType = RouteType.Bus,
            HasTrips = true,
            StopId = Guid.NewGuid(),
            SortBy = sortBy,
            SortDescending = descending
        };

        var sql = RouteRepository
            .SortedQuery(context, RouteRepository.FilteredQuery(context, filter), filter)
            .ToQueryString();

        Assert.Contains("FROM routes", sql);
        Assert.Contains("ORDER BY", sql);
    }

    [Fact]
    public void ValidityQuery_TranslatesToSql()
    {
        using var context = CreateContext();

        var sql = RouteRepository.ValidityQuery(context, [Guid.NewGuid()]).ToQueryString();

        Assert.Contains("GROUP BY", sql);
    }

    private static TerminusDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<TerminusDbContext>()
            .UseNpgsql("Host=localhost;Database=terminus_query_translation")
            .UseSnakeCaseNamingConvention()
            .Options);
}
