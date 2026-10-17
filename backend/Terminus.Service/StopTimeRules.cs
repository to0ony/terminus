using Terminus.Common;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;

namespace Terminus.Service;

/// <summary>GTFS pravila za vremena jedne vožnje (stop_times).</summary>
internal static class StopTimeRules
{
    /// <summary>GTFS: vožnja mora imati barem dvije stanice.</summary>
    public const int MinStopTimes = 2;

    /// <summary>
    /// Stanice, vremena (0–48 h, polazak ne prije dolaska, dolazak ne prije
    /// polaska s prethodne stanice) i ostala polja. Greške idu pod
    /// <paramref name="path"/>[i].Polje.
    /// </summary>
    /// <param name="stops">Stanice vremena po Id-u; nepoznata stanica je greška.</param>
    public static void Validate(
        IReadOnlyList<IStopTimeModel> stopTimes,
        IReadOnlyDictionary<Guid, IStopModel> stops,
        ValidationErrors errors,
        string path)
    {
        for (var i = 0; i < stopTimes.Count; i++)
        {
            var stopTime = stopTimes[i];
            var prefix = $"{path}[{i}]";

            if (!stops.TryGetValue(stopTime.StopId, out var stop))
            {
                errors.Add($"{prefix}.StopId", "Stanica ne postoji.");
            }
            else if (stop.LocationType == LocationType.Station)
            {
                // GTFS: vožnja staje na stajalištu ili peronu, ne na roditeljskoj stanici.
                errors.Add($"{prefix}.StopId", $"'{stop.Name}' je kolodvor; odaberi njegov peron ili stajalište.");
            }
            else if (stop.LocationType != LocationType.StopOrPlatform)
            {
                errors.Add($"{prefix}.StopId", $"'{stop.Name}' nije stajalište ni peron (ulaz, čvor ili mjesto ukrcavanja).");
            }

            if (stopTime.ArrivalTime is < 0 or > GtfsTime.MaxSeconds)
            {
                errors.Add($"{prefix}.ArrivalTime", "Vrijeme dolaska mora biti između 00:00:00 i 48:00:00.");
            }

            if (stopTime.DepartureTime is < 0 or > GtfsTime.MaxSeconds)
            {
                errors.Add($"{prefix}.DepartureTime", "Vrijeme polaska mora biti između 00:00:00 i 48:00:00.");
            }

            if (stopTime.DepartureTime < stopTime.ArrivalTime)
            {
                errors.Add($"{prefix}.DepartureTime", "Polazak ne smije biti prije dolaska.");
            }

            if (i > 0 && stopTime.ArrivalTime < stopTimes[i - 1].DepartureTime)
            {
                errors.Add(
                    $"{prefix}.ArrivalTime",
                    $"Dolazak ({GtfsTime.Format(stopTime.ArrivalTime)}) je prije polaska s prethodne stanice ({GtfsTime.Format(stopTimes[i - 1].DepartureTime)}).");
            }

            if (!Enum.IsDefined(stopTime.PickupType))
            {
                errors.Add($"{prefix}.PickupType", "Nepoznata vrsta ukrcavanja.");
            }

            if (!Enum.IsDefined(stopTime.DropOffType))
            {
                errors.Add($"{prefix}.DropOffType", "Nepoznata vrsta iskrcavanja.");
            }

            if (stopTime.ContinuousPickup is { } continuousPickup && !Enum.IsDefined(continuousPickup))
            {
                errors.Add($"{prefix}.ContinuousPickup", "Nepoznata vrijednost za ukrcavanje duž trase.");
            }

            if (stopTime.ContinuousDropOff is { } continuousDropOff && !Enum.IsDefined(continuousDropOff))
            {
                errors.Add($"{prefix}.ContinuousDropOff", "Nepoznata vrijednost za iskrcavanje duž trase.");
            }

            if (stopTime.ShapeDistTraveled is { } distance)
            {
                if (!double.IsFinite(distance) || distance < 0)
                {
                    errors.Add($"{prefix}.ShapeDistTraveled", "Prijeđena udaljenost ne smije biti negativna.");
                }
                else if (i > 0 && stopTimes[i - 1].ShapeDistTraveled is { } previous && distance < previous)
                {
                    // GTFS: udaljenost duž trase ne smije se smanjivati.
                    errors.Add($"{prefix}.ShapeDistTraveled", "Prijeđena udaljenost ne smije biti manja nego na prethodnoj stanici.");
                }
            }
        }
    }
}
