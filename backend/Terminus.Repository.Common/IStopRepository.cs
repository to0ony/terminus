using Terminus.Common;
using Terminus.Model.Common;
using Terminus.Model.Common.Filters;

namespace Terminus.Repository.Common;

public interface IStopRepository
{
    Task<IStopModel?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <returns>Stanica s GTFS kodom (stop_id) ili null.</returns>
    Task<IStopModel?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <returns>Stanice s traženim Id-evima; nepostojeći Id-evi se izostavljaju.</returns>
    Task<IReadOnlyList<IStopModel>> GetManyAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    Task<PagedResult<IStopModel>> FindAsync(StopFilter filter, CancellationToken cancellationToken = default);

    /// <summary>Kao FindAsync, uz kolodvor, perone, broj vožnji i linije svake stanice.</summary>
    Task<PagedResult<StopTableRow>> FindTableAsync(StopFilter filter, CancellationToken cancellationToken = default);

    /// <summary>Stanice s koordinatama unutar područja, poredane po nazivu.</summary>
    /// <param name="isActive">Samo aktivne ili neaktivne; null za sve.</param>
    /// <returns>Najviše <paramref name="limit"/> stanica.</returns>
    Task<IReadOnlyList<IStopModel>> FindInAreaAsync(GeoBounds bounds, bool? isActive, int limit, CancellationToken cancellationToken = default);

    /// <returns>Područje koje obuhvaća sve stanice s koordinatama; null ako ih nema.</returns>
    Task<GeoBounds?> GetExtentAsync(CancellationToken cancellationToken = default);

    Task<bool> HasChildStopsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <returns>true ako na stanici staje barem jedna vožnja.</returns>
    Task<bool> IsUsedByTripsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <returns>Varijante pa vožnje koje prolaze stanicom.</returns>
    Task<IReadOnlyList<StopSequenceUse>> GetSequencesThroughAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IStopModel> InsertAsync(IStopModel stop, CancellationToken cancellationToken = default);

    /// <returns>Ažurirana stanica ili null ako ne postoji.</returns>
    Task<IStopModel?> UpdateAsync(IStopModel stop, CancellationToken cancellationToken = default);

    /// <returns>false ako stanica ne postoji.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
