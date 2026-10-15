using Microsoft.EntityFrameworkCore;
using Terminus.DAL;
using Terminus.Repository;

namespace Terminus.Tests.Repository;

/// <summary>
/// Provjerava samo da EF upit zna prevesti u SQL (inače bi pao tek pri izvršavanju).
/// ToQueryString ne otvara vezu, pa baza nije potrebna.
/// </summary>
public class DepartureRepositoryQueryTests
{
    [Fact]
    public void ScheduledQuery_TranslatesToSql()
    {
        var options = new DbContextOptionsBuilder<TerminusDbContext>()
            .UseNpgsql("Host=localhost;Database=terminus_query_translation")
            .UseSnakeCaseNamingConvention()
            .Options;
        using var context = new TerminusDbContext(options);

        var sql = DepartureRepository
            .ScheduledQuery(context, [Guid.NewGuid()], [Guid.NewGuid()], 0, 3600, 10)
            .ToQueryString();

        Assert.Contains("FROM stop_times", sql);
        Assert.Contains("LIMIT", sql);
    }
}
