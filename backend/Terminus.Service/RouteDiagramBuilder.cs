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

        var otherVariants = usable
            .Where(v => v.Variant != forward.Variant && v.Variant != backward.Variant)
            .ToList();

        // Gdje ostale varijante počinju i završavaju, ako to nije kraj glavne trase.
        var variantEnds = otherVariants
            .SelectMany(v => new[] { Key(Name(v.Stops[0].StopId)), Key(Name(v.Stops[^1].StopId)) })
            .ToHashSet();

        var diagramStops = rows
            .Select((key, i) => new RouteDiagramStop(
                names[i]!, forwardMinutes[i], backwardMinutes[i], terminus[i], !terminus[i] && variantEnds.Contains(key)))
            .ToList();

        var others = otherVariants
            .OrderBy(v => v.Variant.DirectionId ?? TripDirection.Outbound)
            .ThenBy(v => v.Variant.SortOrder)
            .Select(v =>
            {
                var from = Name(v.Stops[0].StopId);
                var to = Name(v.Stops[^1].StopId);
                var range = Range(rows, v.Stops.Select(s => Key(Name(s.StopId))).ToList(), Key(from), Key(to));
                return new RouteDiagramVariant(v.Variant.Name, v.Variant.DirectionId, from, to, v.Stops.Count, range);
            })
            .ToList();

        return new RouteDiagram(
            route,
            agencyName,
            Direction(forward.Variant, forward.Stops, Name),
            backward.Variant is null ? null : Direction(backward.Variant, backward.Stops, Name),
            diagramStops,
            others);
    }

    /// <summary>
    /// Raspon redaka sheme kojima varijanta prolazi. Kraj je zatvoren ako je tu
    /// i kraj varijante; inače varijanta nastavlja do stanica izvan sheme.
    /// </summary>
    private static RouteDiagramRange? Range(List<string> rows, List<string> keys, string from, string to)
    {
        var indexes = keys.Select(k => rows.IndexOf(k)).Where(i => i >= 0).ToList();
        if (indexes.Count == 0)
        {
            return null;
        }

        var top = indexes.Min();
        var bottom = indexes.Max();
        var ends = new[] { rows.IndexOf(from), rows.IndexOf(to) };
        return new RouteDiagramRange(top, bottom, ends.Contains(top), ends.Contains(bottom));
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
