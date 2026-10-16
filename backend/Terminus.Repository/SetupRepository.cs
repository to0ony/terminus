using Microsoft.EntityFrameworkCore;
using Terminus.DAL;
using Terminus.Model.Common;
using Terminus.Repository.Common;

namespace Terminus.Repository;

public class SetupRepository(TerminusDbContext context) : ISetupRepository
{
    public async Task<TimetableCounts> GetCountsAsync(CancellationToken cancellationToken = default) => new(
        Agencies: await context.Agencies.CountAsync(cancellationToken),
        Stops: await context.Stops.CountAsync(cancellationToken),
        Routes: await context.Routes.CountAsync(cancellationToken),
        Trips: await context.Trips.CountAsync(cancellationToken));
}
