using Terminus.Model.Common;
using Terminus.Model.Common.Enums;

namespace Terminus.Repository.Common;

/// <summary>Čitanje voznog reda za pregled u dashboardu (linije, stanice).</summary>
public interface ITimetableRepository
{
    /// <summary>Vožnje linije s vremenima.</summary>
    /// <param name="directionId">Samo vožnje u tom smjeru; null za sve.</param>
    /// <param name="serviceCalendarIds">Samo vožnje tih kalendara; null za sve.</param>
    Task<IReadOnlyList<TripStopTimes>> GetRouteTripsAsync(
        Guid routeId,
        TripDirection? directionId,
        IReadOnlyCollection<Guid>? serviceCalendarIds,
        CancellationToken cancellationToken = default);

    /// <summary>Vožnje svih linija tih kalendara, s vremenima.</summary>
    Task<IReadOnlyList<TripStopTimes>> GetTripsAsync(
        IReadOnlyCollection<Guid> serviceCalendarIds,
        CancellationToken cancellationToken = default);

    /// <returns>Linije čije vožnje staju na barem jednoj od stanica, poredane kao popis linija.</returns>
    Task<IReadOnlyList<IRouteModel>> GetRoutesServingAsync(
        IReadOnlyCollection<Guid> stopIds,
        CancellationToken cancellationToken = default);
}
