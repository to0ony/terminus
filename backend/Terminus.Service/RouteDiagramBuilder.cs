using Terminus.Model.Common;
using Terminus.Model.Common.Enums;
using Terminus.Service.Common;

namespace Terminus.Service;

/// <summary>
/// Spaja glavne varijante oba smjera u jedan popis stanica za shemu linije.
/// Smjer 1 se obrne, pa obje idu odozgo prema dolje, a zajedničke stanice
/// (isti naziv) dijele redak.
/// </summary>
internal static class RouteDiagramBuilder
{
    /// <param name="variants">Varijante linije; prva po redoslijedu u smjeru je glavna.</param>
    /// <param name="stops">Stanice varijanti po Id-u.</param>
    /// <returns>Null ako nijedna varijanta nema stanica.</returns>
    public static RouteDiagram? Build(
        IRouteModel route,
        string agencyName,
        IReadOnlyList<IRouteVariantModel> variants,
        IReadOnlyDictionary<Guid, IStopModel> stops)
    {
        var usable = variants
            .Select(v => (Variant: v, Stops: v.Stops.Where(s => stops.ContainsKey(s.StopId)).ToList()))
            .Where(v => v.Stops.Count > 0)
            .ToList();
        if (usable.Count == 0)
        {
            return null;
        }

        // Glavna varijanta smjera 0 ide prema dolje; bez nje prva bilo kojeg smjera.
        var mains = usable
            .GroupBy(v => v.Variant.DirectionId)
            .Select(g => g.OrderBy(v => v.Variant.SortOrder).First())
            .ToList();
        var forward = mains.FirstOrDefault(v => v.Variant.DirectionId == TripDirection.Outbound);
        if (forward.Variant is null)
        {
            forward = mains.OrderBy(v => v.Variant.DirectionId ?? TripDirection.Outbound).First();
        }
        var backward = mains.FirstOrDefault(v =>
            v.Variant.DirectionId == TripDirection.Inbound && v.Variant != forward.Variant);

        string Name(Guid stopId) => stops[stopId].Name.Trim();

        var forwardKeys = forward.Stops.Select(s => Key(Name(s.StopId))).ToList();
        var backwardKeys = backward.Variant is null
            ? []
            : backward.Stops.Select(s => Key(Name(s.StopId))).Reverse().ToList();

        var rows = StopSequenceMerger.Merge<string>([forwardKeys, backwardKeys]);
        var forwardRows = StopSequenceMerger.Map(rows, forwardKeys);
        var backwardRows = StopSequenceMerger.Map(rows, backwardKeys);

        var names = new string?[rows.Count];
        var forwardMinutes = new int?[rows.Count];
        var backwardMinutes = new int?[rows.Count];
        var terminus = new bool[rows.Count];

        var times = Minutes(forward.Stops);
        for (var i = 0; i < forwardRows.Length; i++)
        {
            names[forwardRows[i]] = Name(forward.Stops[i].StopId);
            forwardMinutes[forwardRows[i]] = times[i];
        }
        terminus[forwardRows[0]] = terminus[forwardRows[^1]] = true;

        if (backward.Variant is not null)
        {
            // Redci smjera 1 su obrnuti: zadnji redak je njegova prva stanica.
            times = Minutes(backward.Stops);
            for (var i = 0; i < backwardRows.Length; i++)
            {
                var stop = backward.Stops.Count - 1 - i;
                names[backwardRows[i]] ??= Name(backward.Stops[stop].StopId);
                backwardMinutes[backwardRows[i]] = times[stop];
            }
            terminus[backwardRows[0]] = terminus[backwardRows[^1]] = true;
        }

        var diagramStops = rows
            .Select((_, i) => new RouteDiagramStop(names[i]!, forwardMinutes[i], backwardMinutes[i], terminus[i]))
            .ToList();

        var others = usable
            .Where(v => v.Variant != forward.Variant && v.Variant != backward.Variant)
            .OrderBy(v => v.Variant.DirectionId ?? TripDirection.Outbound)
            .ThenBy(v => v.Variant.SortOrder)
            .Select(v => new RouteDiagramVariant(
                v.Variant.Name, v.Variant.DirectionId, Name(v.Stops[0].StopId), Name(v.Stops[^1].StopId), v.Stops.Count))
            .ToList();

        return new RouteDiagram(
            route,
            agencyName,
            Direction(forward.Variant, forward.Stops, Name),
            backward.Variant is null ? null : Direction(backward.Variant, backward.Stops, Name),
            diagramStops,
            others);
    }

    /// <summary>Naziv bez razlike u velikim slovima i razmacima.</summary>
    private static string Key(string name) =>
        string.Join(' ', name.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();

    /// <summary>Minute od polaska s prve stanice do dolaska na svaku, zaokruženo.</summary>
    private static int[] Minutes(IReadOnlyList<VariantStop> stops)
    {
        var minutes = new int[stops.Count];
        var seconds = 0;
        for (var i = 1; i < stops.Count; i++)
        {
            seconds += stops[i - 1].DwellTime + stops[i].TravelTime;
            minutes[i] = (int)Math.Round(seconds / 60.0, MidpointRounding.AwayFromZero);
        }
        return minutes;
    }

    private static RouteDiagramDirection Direction(IRouteVariantModel variant, IReadOnlyList<VariantStop> stops, Func<Guid, string> name) =>
        new(string.IsNullOrWhiteSpace(variant.Headsign) ? name(stops[^1].StopId) : variant.Headsign.Trim(), name(stops[0].StopId));
}
