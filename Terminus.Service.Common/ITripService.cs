using Terminus.Common;
using Terminus.Model.Common;
using Terminus.Model.Common.Filters;

namespace Terminus.Service.Common;

/// <summary>Vožnje (GTFS trips) i njihova vremena po stanicama (GTFS stop_times).</summary>
public interface ITripService
{
    Task<ITripModel> GetAsync(Guid agencyId, Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<ITripModel>> FindAsync(Guid agencyId, TripFilter filter, CancellationToken cancellationToken = default);

    Task<ITripModel> CreateAsync(Guid agencyId, ITripModel trip, CancellationToken cancellationToken = default);

    Task<ITripModel> UpdateAsync(Guid agencyId, Guid id, ITripModel trip, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid agencyId, Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IStopTimeModel>> GetStopTimesAsync(Guid agencyId, Guid tripId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Zamjenjuje sva vremena vožnje. Redni brojevi dodjeljuju se prema
    /// redoslijedu u popisu (1, 2, 3 ...); StopSequence iz ulaza se zanemaruje.
    /// </summary>
    Task<IReadOnlyList<IStopTimeModel>> ReplaceStopTimesAsync(
        Guid agencyId,
        Guid tripId,
        IReadOnlyList<IStopTimeModel> stopTimes,
        CancellationToken cancellationToken = default);
}
