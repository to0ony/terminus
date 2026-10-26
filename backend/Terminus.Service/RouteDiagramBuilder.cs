using Terminus.Model.Common;
using Terminus.Model.Common.Enums;
using Terminus.Service.Common;

namespace Terminus.Service;

/// <summary>
/// Spaja glavne varijante oba smjera u jedan popis stanica za shemu linije.
/// Smjer 1 se obrne, pa obje idu istim redom, a zajedničke stanice
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

        // Glavna varijanta smjera 0 određuje redoslijed; bez nje prva bilo kojeg smjera.
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

        var otherVariants = usable
            .Where(v => v.Variant != forward.Variant && v.Variant != backward.Variant)
            .ToList();

        // Prvo glavne varijante, pa redom ostale: njihove stanice umeću se
        // između najbližih zajedničkih stanica. Varijante smjera 1 se obrnu.
        var rows = StopSequenceMerger.Merge<string>([forwardKeys, backwardKeys]);
        foreach (var other in otherVariants)
        {
            var keys = other.Stops.Select(s => Key(Name(s.StopId))).ToList();
            if (backward.Variant is not null && other.Variant.DirectionId == backward.Variant.DirectionId)
            {
                keys.Reverse();
            }
            rows = StopSequenceMerger.Merge<string>([rows, keys]);
        }

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
            // Smjer 1 je obrnut: zadnja stanica sheme je njegova prva.
            times = Minutes(backward.Stops);
            for (var i = 0; i < backwardRows.Length; i++)
            {
                var stop = backward.Stops.Count - 1 - i;
                names[backwardRows[i]] ??= Name(backward.Stops[stop].StopId);
                backwardMinutes[backwardRows[i]] = times[stop];
            }
            terminus[backwardRows[0]] = terminus[backwardRows[^1]] = true;
        }

        // Stanice samo ostalih varijanti: naziv iz prve varijante koja ih ima.
        foreach (var other in otherVariants)
        {
            foreach (var stop in other.Stops)
            {
                var name = Name(stop.StopId);
                var row = rows.IndexOf(Key(name));
                names[row] ??= name;
            }
        }

        // Gdje ostale varijante počinju i završavaju, ako to nije kraj glavne trase.
        var variantEnds = otherVariants
            .SelectMany(v => new[] { Key(Name(v.Stops[0].StopId)), Key(Name(v.Stops[^1].StopId)) })
            .ToHashSet();

        // U kojem smjeru stanica ima polaske: staje li tu bilo koja varijanta tog smjera.
        HashSet<string> Served(IRouteVariantModel? main) => main is null
            ? []
            : usable
                .Where(v => v.Variant.DirectionId == main.DirectionId)
                .SelectMany(v => v.Stops.Select(s => Key(Name(s.StopId))))
                .ToHashSet();
        var forwardServed = Served(forward.Variant);
        var backwardServed = Served(backward.Variant);

        var diagramStops = rows
            .Select((key, i) => new RouteDiagramStop(
                names[i]!,
                forwardServed.Contains(key),
                backwardServed.Contains(key),
                forwardMinutes[i],
                backwardMinutes[i],
                terminus[i],
                !terminus[i] && variantEnds.Contains(key)))
            .ToList();

        return new RouteDiagram(
            route,
            agencyName,
            Direction(forward.Variant, forward.Stops, Name),
            backward.Variant is null ? null : Direction(backward.Variant, backward.Stops, Name),
            diagramStops);
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
