using Microsoft.EntityFrameworkCore;
using Terminus.DAL;
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

    private static TerminusDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<TerminusDbContext>()
            .UseNpgsql("Host=localhost;Database=terminus_query_translation")
            .UseSnakeCaseNamingConvention()
            .Options);
}
