using Terminus.Model.Common;
using Terminus.Model.Common.Enums;

namespace Terminus.Service.Common;

/// <summary>
/// Vozni red linije za grafikon: stanice jednog smjera (redoslijed svih
/// obrazaca) i vožnje s vremenima, te spremanje izmjena odjednom.
/// </summary>
public interface IScheduleService
{
    /// <param name="calendarId">Samo vožnje tog kalendara (tipa dana); null za sve.</param>
    Task<RouteSchedule> GetAsync(Guid routeId, TripDirection? directionId, Guid? calendarId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sprema izmjene u jednoj transakciji. Vožnja koju je u međuvremenu
    /// promijenio netko drugi (drukčiji UpdatedAt) je sukob (409), a vremena
    /// moraju poštovati GTFS pravila.
    /// </summary>
    Task SaveAsync(Guid routeId, ScheduleChanges changes, CancellationToken cancellationToken = default);
}

/// <param name="Stops">
/// Redci grafikona: spojeni redoslijed stanica varijanti smjera (redom, glavna
/// prva) i vožnji svih kalendara.
/// </param>
/// <param name="Variants">Varijante smjera, glavna prva.</param>
/// <param name="Trips">Vožnje traženog kalendara, poredane po prvom polasku.</param>
/// <param name="TripCounts">Broj vožnji smjera po kalendaru.</param>
/// <param name="VariantTripCounts">Broj vožnji smjera (svih kalendara) po varijanti.</param>
/// <param name="DepartureDayTypes">Tipovi dana (kalendari) u kojima vozi svaki polazak.</param>
public sealed record RouteSchedule(
    IRouteModel Route,
    TripDirection? DirectionId,
    IReadOnlyList<IStopModel> Stops,
    IReadOnlyList<IRouteVariantModel> Variants,
    IReadOnlyList<TripStopTimes> Trips,
    IReadOnlyDictionary<Guid, int> TripCounts,
    IReadOnlyDictionary<Guid, int> VariantTripCounts,
    IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> DepartureDayTypes);

/// <param name="DirectionId">Smjer nove i izmijenjenih vožnji.</param>
public sealed record ScheduleChanges(
    TripDirection? DirectionId,
    IReadOnlyList<ScheduleTripChange> Upserts,
    IReadOnlyList<TripVersion> Deletes);

/// <param name="Trip">Vožnja; Id je Guid.Empty za novu.</param>
/// <param name="UpdatedAt">Za postojeću vožnju vrijeme izmjene koje je klijent učitao.</param>
/// <param name="DayTypes">
/// Svi tipovi dana (kalendari) u kojima polazak vozi, uz kalendar same vožnje; vožnje
/// u ostalim tipovima dana se dodaju, usklađuju ili brišu. Null: polazak ostaje u
/// istim tipovima dana, a vožnje u njima dobivaju ista vremena.
/// </param>
/// <param name="Detach">Vožnja izlazi iz polaska; vožnje u ostalim tipovima dana ostaju kakve jesu.</param>
public sealed record ScheduleTripChange(
    ITripModel Trip,
    IReadOnlyList<IStopTimeModel> StopTimes,
    DateTime? UpdatedAt,
    IReadOnlyList<Guid>? DayTypes = null,
    bool Detach = false);

public sealed record TripVersion(Guid Id, DateTime UpdatedAt);
