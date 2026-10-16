using Terminus.Common;
using Terminus.Model.Common;
using Terminus.Model.Common.Filters;

namespace Terminus.Service.Common;

/// <summary>Cijene karata (GTFS fare_attributes) i pravila primjene (fare_rules).</summary>
public interface IFareService
{
    Task<IFareModel> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<IFareModel>> FindAsync(FareFilter filter, CancellationToken cancellationToken = default);

    Task<IFareModel> CreateAsync(IFareModel fare, CancellationToken cancellationToken = default);

    /// <summary>Mijenja cijenu i zamjenjuje sva pravila.</summary>
    Task<IFareModel> UpdateAsync(Guid id, IFareModel fare, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
