using Terminus.Model.Common;

namespace Terminus.Repository.Common;

/// <summary>Jedini zapis feed_info instance.</summary>
public interface IFeedInfoRepository
{
    /// <returns>Podaci o izdavaču ili null ako još nisu uneseni.</returns>
    Task<IFeedInfoModel?> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>Stvara zapis ili mijenja postojeći.</summary>
    Task<IFeedInfoModel> SaveAsync(IFeedInfoModel feedInfo, CancellationToken cancellationToken = default);

    /// <returns>false ako zapis ne postoji.</returns>
    Task<bool> DeleteAsync(CancellationToken cancellationToken = default);
}
