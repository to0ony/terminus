using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;

namespace Terminus.Service;

/// <summary>Prepoznaje varijante linije iz njezinih vožnji.</summary>
internal static class RouteVariantBuilder
{
    /// <summary>
    /// Vožnje istog smjera i redoslijeda stanica čine varijantu. Vremena vožnje
    /// i stajanja su srednja (medijan) među njezinim vožnjama, natpis smjera i
    /// trasa najčešći, a naziv "prva → zadnja stanica". Unutar smjera varijante
    /// su poredane po broju vožnji, pa je prva glavna.
    /// </summary>
    public static IReadOnlyList<(IRouteVariantModel Variant, IReadOnlyList<Guid> TripIds)> Derive(
        Guid routeId,
        IReadOnlyList<TripStopTimes> trips,
        Func<Guid, string> stopName)
    {
        var groups = trips
            .Where(t => t.StopTimes.Count >= 2)
            .GroupBy(t => (t.Trip.DirectionId, Key: string.Join(',', t.StopTimes.Select(st => st.StopId))))
            .Select(g => g.ToList())
            .OrderBy(g => g[0].Trip.DirectionId is null)
            .ThenBy(g => g[0].Trip.DirectionId)
            .ThenByDescending(g => g.Count)
            .ToList();

        var result = new List<(IRouteVariantModel, IReadOnlyList<Guid>)>();
        var names = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var (group, index) in groups.Select((g, i) => (g, i)))
        {
            var first = group[0].StopTimes;
            var stops = first
                .Select((st, i) => new VariantStop(
                    st.StopId,
                    i == 0 ? 0 : Median(group.Select(t => t.StopTimes[i].ArrivalTime - t.StopTimes[i - 1].DepartureTime)),
                    Median(group.Select(t => t.StopTimes[i].DepartureTime - t.StopTimes[i].ArrivalTime))))
                .ToList();

            // Isti naziv (npr. dvije varijante između istih krajnjih stanica) dobiva broj.
            var name = $"{stopName(first[0].StopId)} → {stopName(first[^1].StopId)}";
            names[name] = names.GetValueOrDefault(name) + 1;
            if (names[name] > 1)
            {
                name = $"{name} ({names[name]})";
            }

            var direction = group[0].Trip.DirectionId;
            var variant = new RouteVariantModel
            {
                Id = Guid.NewGuid(),
                RouteId = routeId,
                Name = name,
                DirectionId = direction,
                Headsign = MostCommon(group.Select(t => t.Trip.Headsign)),
                ShapeId = MostCommon(group.Select(t => t.Trip.ShapeId)),
                SortOrder = groups.Take(index).Count(g => g[0].Trip.DirectionId == direction),
                Source = VariantSource.Gtfs,
                Stops = stops
            };

            result.Add((variant, group.Select(t => t.Trip.Id).ToList()));
        }

        return result;
    }

    /// <summary>Medijan, nikad negativan (vremena se ne smiju vraćati).</summary>
    private static int Median(IEnumerable<int> values)
    {
        var sorted = values.Order().ToList();
        return sorted.Count == 0 ? 0 : Math.Max(0, sorted[sorted.Count / 2]);
    }

    private static T? MostCommon<T>(IEnumerable<T?> values) =>
        values.Where(v => v is not null).GroupBy(v => v).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault();
}
