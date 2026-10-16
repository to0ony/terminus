using Terminus.Model.Common;

namespace Terminus.Service.Common;

/// <summary>Podaci o izdavaču feeda (GTFS feed_info).</summary>
public interface IFeedInfoService
{
    /// <exception cref="Terminus.Common.Exceptions.NotFoundException">Podaci još nisu uneseni.</exception>
    Task<IFeedInfoModel> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>Stvara zapis ili mijenja postojeći.</summary>
    Task<IFeedInfoModel> SaveAsync(IFeedInfoModel feedInfo, CancellationToken cancellationToken = default);

    /// <summary>Bez zapisa izvoz koristi prvog prijevoznika kao izdavača.</summary>
    Task DeleteAsync(CancellationToken cancellationToken = default);
}
