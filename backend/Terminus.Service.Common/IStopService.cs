using Terminus.Common;
using Terminus.Model.Common;
using Terminus.Model.Common.Filters;

namespace Terminus.Service.Common;

public interface IStopService
{
    Task<IStopModel> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<IStopModel>> FindAsync(StopFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stanice unutar područja karte, najviše <c>StopService.MaxAreaStops</c>.
    /// Ako ih ima više, rezultat je skraćen i karta treba približiti prikaz.
    /// </summary>
    Task<StopsInArea> FindInAreaAsync(GeoBounds bounds, bool? isActive, CancellationToken cancellationToken = default);

    /// <returns>Područje svih stanica (početni prikaz karte); null ako nijedna nema koordinate.</returns>
    Task<GeoBounds?> GetExtentAsync(CancellationToken cancellationToken = default);

    Task<IStopModel> CreateAsync(IStopModel stop, CancellationToken cancellationToken = default);

    Task<IStopModel> UpdateAsync(Guid id, IStopModel stop, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <param name="Truncated">true ako u području ima više stanica od vraćenih.</param>
public sealed record StopsInArea(IReadOnlyList<IStopModel> Stops, bool Truncated);
