using Terminus.Common;
using Terminus.Model;
using Terminus.Model.Common.Enums;

namespace Terminus.Service.Gtfs.Import;

internal sealed partial class GtfsFeedReader
{
    /// <summary>Vožnje iz trips.txt; vremena se dodaju iz stop_times.txt.</summary>
    private readonly Dictionary<string, List<RawStopTime>> _rawStopTimes = new(StringComparer.Ordinal);

    // ---------------------------------------------------------------- trips.txt

    private void ReadTrips()
    {
        using var table = OpenRequired("trips.txt", "route_id", "service_id", "trip_id");
        if (table is null)
        {
            return;
        }

        foreach (var row in table.Rows())
        {
            var code = row["trip_id"];
            var where = Where(row, "trip_id", code);

            if (code is null)
            {
                _report.Warning(table.FileName, "Vožnja bez trip_id je preskočena.", where);
                continue;
            }

            if (_tripsByCode.ContainsKey(code))
            {
                _report.Error(table.FileName, "Dvostruki trip_id.", where);
                continue;
            }

            if (row["route_id"] is not { } routeCode || !_routesByCode.TryGetValue(routeCode, out var route))
            {
                _report.Warning(table.FileName, "Vožnja na nepostojećoj ili preskočenoj liniji je preskočena.", where);
                continue;
            }

            if (row["service_id"] is not { } serviceCode || !_calendarsByCode.TryGetValue(serviceCode, out var calendar))
            {
                _report.Warning(table.FileName, "Vožnja s nepostojećim ili preskočenim kalendarom (service_id) je preskočena.", where);
                continue;
            }

            Guid? shapeId = null;
            if (row["shape_id"] is { } shapeCode)
            {
                if (_shapesByCode.TryGetValue(shapeCode, out var shape))
                {
                    shapeId = shape.Id;
                }
                else
                {
                    _report.Warning(table.FileName, "Nepostojeća ili preskočena trasa (shape_id) je zanemarena.", where);
                }
            }

            _tripsByCode[code] = new TripModel
            {
                Id = Guid.CreateVersion7(),
                RouteId = route.Id,
                ServiceCalendarId = calendar.Id,
                Code = code,
                Headsign = row["trip_headsign"],
                ShortName = row["trip_short_name"],
                DirectionId = OptionalEnum<TripDirection>(row["direction_id"], table.FileName, "direction_id", where),
                BlockId = row["block_id"],
                ShapeId = shapeId,
                WheelchairAccessible = EnumOrDefault(row["wheelchair_accessible"], WheelchairAccessible.Unknown, table.FileName, "wheelchair_accessible", where),
                BikesAllowed = EnumOrDefault(row["bikes_allowed"], BikesAllowed.Unknown, table.FileName, "bikes_allowed", where)
            };
        }
    }

    // ---------------------------------------------------------------- stop_times.txt, frequencies.txt

    private void ReadStopTimesAndFrequencies()
    {
        ReadRawStopTimes();
        if (_report.HasErrors)
        {
            return;
        }

        var frequencies = ReadFrequencies();
        var usedCodes = new HashSet<string>(_tripsByCode.Keys, StringComparer.Ordinal);

        foreach (var (code, trip) in _tripsByCode)
        {
            if (!_rawStopTimes.TryGetValue(code, out var raw) || raw.Count == 0)
            {
                _report.Warning("stop_times.txt", "Vožnja bez stanica i vremena je preskočena.", $"trip_id '{code}'");
                continue;
            }

            var stopTimes = BuildStopTimes(code, raw);
            if (stopTimes is null)
            {
                continue;
            }

            if (frequencies.TryGetValue(code, out var periods))
            {
                ExpandFrequencies(trip, stopTimes, periods, usedCodes);
            }
            else
            {
                AddTrip(trip, stopTimes);
            }
        }

        _rawStopTimes.Clear();
    }

    private void ReadRawStopTimes()
    {
        using var table = OpenRequired("stop_times.txt", "trip_id", "stop_sequence");
        if (table is null)
        {
            return;
        }

        if (!table.Has("stop_id"))
        {
            _report.Error(table.FileName, "Nedostaje stupac stop_id (GTFS-Flex vožnje bez stanica nisu podržane).");
            return;
        }

        var invalidTrips = new HashSet<string>(StringComparer.Ordinal);

        foreach (var row in table.Rows())
        {
            var tripCode = row["trip_id"];
            var where = Where(row, "trip_id", tripCode);

            if (tripCode is null || !_tripsByCode.ContainsKey(tripCode))
            {
                // Vožnje preskočene u trips.txt su već prijavljene.
                if (tripCode is null)
                {
                    _report.Warning(table.FileName, "Vrijeme bez trip_id je preskočeno.", where);
                }
                continue;
            }

            if (invalidTrips.Contains(tripCode))
            {
                continue;
            }

            string? problem = null;
            var stopCode = row["stop_id"];

            if (stopCode is null)
            {
                problem = "Vožnja s vremenom bez stanice (GTFS-Flex) je preskočena.";
            }
            else if (!_stopsByCode.TryGetValue(stopCode, out var stop))
            {
                problem = "Vožnja kroz nepostojeću ili preskočenu stanicu je preskočena.";
            }
            else if (stop.LocationType != LocationType.StopOrPlatform)
            {
                problem = "Vožnja koja staje na stanici koja nije stajalište ili peron (location_type 0) je preskočena.";
            }
            else if (!TryInt(row["stop_sequence"], out var sequence) || sequence < 0)
            {
                problem = "Vožnja s neispravnim stop_sequence je preskočena.";
            }
            else if (!TryOptionalTime(row["arrival_time"], out var arrival) || !TryOptionalTime(row["departure_time"], out var departure))
            {
                problem = "Vožnja s neispravnim vremenom (HH:MM:SS) je preskočena.";
            }
            else if (!TryEnum<PickupDropOffType>(row["pickup_type"], out var pickup, PickupDropOffType.Regular)
                     || !TryEnum<PickupDropOffType>(row["drop_off_type"], out var dropOff, PickupDropOffType.Regular)
                     || !TryOptionalEnum<ContinuousPickupDropOff>(row["continuous_pickup"], out var continuousPickup)
                     || !TryOptionalEnum<ContinuousPickupDropOff>(row["continuous_drop_off"], out var continuousDropOff)
                     || row["timepoint"] is not (null or "0" or "1"))
            {
                problem = "Vožnja s nepoznatom vrijednošću pickup_type, drop_off_type, continuous_* ili timepoint je preskočena.";
            }
            else
            {
                double? dist = null;
                if (row["shape_dist_traveled"] is { } distValue)
                {
                    if (TryDouble(distValue, out var d) && d >= 0)
                    {
                        dist = d;
                    }
                    else
                    {
                        _report.Warning(table.FileName, "Neispravan shape_dist_traveled je zanemaren.", where);
                    }
                }

                if (!_rawStopTimes.TryGetValue(tripCode, out var list))
                {
                    _rawStopTimes[tripCode] = list = [];
                }

                list.Add(new RawStopTime(
                    sequence, stop.Id, stop.Latitude, stop.Longitude, arrival, departure, row["stop_headsign"],
                    pickup, dropOff, continuousPickup, continuousDropOff, dist,
                    Timepoint: row["timepoint"] != "0"));
                continue;
            }

            _report.Warning(table.FileName, problem, where);
            invalidTrips.Add(tripCode);
            _rawStopTimes.Remove(tripCode);
        }

        foreach (var code in invalidTrips)
        {
            _tripsByCode.Remove(code);
        }
    }

    /// <summary>
    /// Vremena jedne vožnje: redoslijed po stop_sequence, prazna vremena
    /// interpolirana između susjednih (timepoint = 0), provjera da ne idu
    /// unatrag i da ne prelaze 48 h.
    /// </summary>
    /// <returns>Vremena s rednim brojevima 1, 2, 3 ... ili null ako je vožnja preskočena.</returns>
    private List<StopTimeModel>? BuildStopTimes(string tripCode, List<RawStopTime> raw)
    {
        const string file = "stop_times.txt";
        var where = $"trip_id '{tripCode}'";

        var ordered = raw.OrderBy(r => r.Sequence).ToList();

        if (ordered.Select(r => r.Sequence).Distinct().Count() != ordered.Count)
        {
            _report.Warning(file, "Vožnja s dvostrukim stop_sequence je preskočena.", where);
            return null;
        }

        if (ordered.Count < 2)
        {
            _report.Warning(file, "Vožnja s manje od dvije stanice je preskočena.", where);
            return null;
        }

        var times = ordered.Select(r => (Arrival: r.Arrival ?? r.Departure, Departure: r.Departure ?? r.Arrival, r.Timepoint)).ToArray();

        if (times[0].Departure is null || times[^1].Arrival is null)
        {
            _report.Warning(file, "Vožnja bez vremena na prvoj ili zadnjoj stanici je preskočena.", where);
            return null;
        }

        var interpolated = Interpolate(ordered, times);
        if (interpolated < 0)
        {
            _report.Warning(file, "Vožnja čija vremena idu unatrag je preskočena.", where);
            return null;
        }

        for (var i = 0; i < times.Length; i++)
        {
            var (arrival, departure, _) = times[i];
            if (departure < arrival || (i > 0 && arrival < times[i - 1].Departure))
            {
                _report.Warning(file, "Vožnja čija vremena idu unatrag je preskočena.", where);
                return null;
            }

            if (departure > GtfsTime.MaxSeconds)
            {
                _report.Warning(file, "Vožnja s vremenom iza 48:00:00 je preskočena.", where);
                return null;
            }
        }

        var dists = ordered.Select(r => r.ShapeDistTraveled).ToList();
        var keepDist = IsNonDecreasing(dists);
        if (!keepDist)
        {
            _report.Warning(file, "shape_dist_traveled vožnje se smanjuje, pa je zanemaren za cijelu vožnju.", where);
        }

        _interpolatedStopTimes += interpolated;

        return ordered.Select((r, i) => new StopTimeModel
        {
            StopSequence = i + 1,
            StopId = r.StopId,
            ArrivalTime = times[i].Arrival!.Value,
            DepartureTime = times[i].Departure!.Value,
            StopHeadsign = r.StopHeadsign,
            PickupType = r.PickupType,
            DropOffType = r.DropOffType,
            ContinuousPickup = r.ContinuousPickup,
            ContinuousDropOff = r.ContinuousDropOff,
            ShapeDistTraveled = keepDist ? r.ShapeDistTraveled : null,
            Timepoint = times[i].Timepoint
        }).ToList();
    }

    /// <summary>
    /// Popunjava stanice bez vremena između dvije s vremenom, razmjerno udaljenosti:
    /// po shape_dist_traveled ako ga imaju sve, inače po zračnoj udaljenosti
    /// stanica, a bez koordinata jednoliko.
    /// </summary>
    /// <returns>Broj interpoliranih vremena ili -1 ako vremena oko praznina idu unatrag.</returns>
    internal static int Interpolate(IReadOnlyList<RawStopTime> stops, (int? Arrival, int? Departure, bool Timepoint)[] times)
    {
        var count = 0;
        var previous = 0;

        for (var next = 1; next < times.Length; next++)
        {
            if (times[next].Arrival is null)
            {
                continue;
            }

            if (next - previous > 1)
            {
                var start = times[previous].Departure!.Value;
                var end = times[next].Arrival!.Value;
                if (end < start)
                {
                    return -1;
                }

                var distances = SegmentDistances(stops, previous, next);
                var total = distances.Sum();

                var travelled = 0.0;
                for (var k = previous + 1; k < next; k++)
                {
                    travelled += distances[k - previous - 1];
                    var share = total > 0 ? travelled / total : (double)(k - previous) / (next - previous);
                    var time = start + (int)Math.Round((end - start) * share, MidpointRounding.AwayFromZero);
                    times[k] = (time, time, false);
                    count++;
                }
            }

            previous = next;
        }

        return count;
    }

    /// <summary>Udaljenosti od stanice k-1 do k za k = from+1 .. to.</summary>
    private static double[] SegmentDistances(IReadOnlyList<RawStopTime> stops, int from, int to)
    {
        var segment = stops.Skip(from).Take(to - from + 1).ToList();

        if (segment.All(s => s.ShapeDistTraveled is not null) && IsNonDecreasing(segment.Select(s => s.ShapeDistTraveled)))
        {
            return segment.Zip(segment.Skip(1), (a, b) => b.ShapeDistTraveled!.Value - a.ShapeDistTraveled!.Value).ToArray();
        }

        if (segment.All(s => s.Latitude is not null && s.Longitude is not null))
        {
            return segment.Zip(segment.Skip(1), (a, b) => Haversine(a, b)).ToArray();
        }

        return new double[segment.Count - 1];
    }

    private static double Haversine(RawStopTime a, RawStopTime b)
    {
        const double earthRadius = 6_371_000;
        static double Rad(double degrees) => degrees * Math.PI / 180;

        var dLat = Rad(b.Latitude!.Value - a.Latitude!.Value);
        var dLon = Rad(b.Longitude!.Value - a.Longitude!.Value);
        var h = Math.Pow(Math.Sin(dLat / 2), 2)
              + Math.Cos(Rad(a.Latitude.Value)) * Math.Cos(Rad(b.Latitude.Value)) * Math.Pow(Math.Sin(dLon / 2), 2);
        return 2 * earthRadius * Math.Asin(Math.Sqrt(h));
    }

    private Dictionary<string, List<FrequencyPeriod>> ReadFrequencies()
    {
        var result = new Dictionary<string, List<FrequencyPeriod>>(StringComparer.Ordinal);

        if (!_archive.Contains("frequencies.txt"))
        {
            return result;
        }

        // Bez frequencies bi se predlošci vožnji uvezli kao obične vožnje, što je pogrešno.
        using var table = OpenRequired("frequencies.txt", "trip_id", "start_time", "end_time", "headway_secs");
        foreach (var row in table?.Rows() ?? [])
        {
            var code = row["trip_id"];
            var where = Where(row, "trip_id", code);

            if (code is null || !_tripsByCode.ContainsKey(code))
            {
                continue;
            }

            if (!GtfsTime.TryParse(row["start_time"], out var start) || !GtfsTime.TryParse(row["end_time"], out var end)
                || !TryInt(row["headway_secs"], out var headway) || headway <= 0 || end <= start)
            {
                _report.Warning(table!.FileName, "Razdoblje s neispravnim vremenima ili intervalom je preskočeno.", where);
                continue;
            }

            if (!result.TryGetValue(code, out var list))
            {
                result[code] = list = [];
            }
            list.Add(new FrequencyPeriod(start, end, headway));
        }

        return result;
    }

    /// <summary>
    /// Vožnja iz frequencies.txt postaje niz pojedinačnih vožnji: svaka kreće u
    /// start_time, start_time + headway ... (prije end_time), s istim razmacima
    /// između stanica kao predložak. Kod nove vožnje je trip_id i vrijeme polaska.
    /// </summary>
    private void ExpandFrequencies(TripModel template, List<StopTimeModel> stopTimes, List<FrequencyPeriod> periods, HashSet<string> usedCodes)
    {
        var first = stopTimes[0].DepartureTime;
        var duration = stopTimes[^1].ArrivalTime - first;
        var tooLate = 0;

        foreach (var period in periods.OrderBy(p => p.Start))
        {
            for (var departure = period.Start; departure < period.End; departure += period.Headway)
            {
                if (departure + duration > GtfsTime.MaxSeconds)
                {
                    tooLate++;
                    continue;
                }

                var code = UniqueCode($"{template.Code}-{GtfsTime.Format(departure).Replace(":", "")}", usedCodes);
                var trip = new TripModel
                {
                    Id = Guid.CreateVersion7(),
                    RouteId = template.RouteId,
                    ServiceCalendarId = template.ServiceCalendarId,
                    Code = code,
                    Headsign = template.Headsign,
                    ShortName = template.ShortName,
                    DirectionId = template.DirectionId,
                    BlockId = template.BlockId,
                    ShapeId = template.ShapeId,
                    WheelchairAccessible = template.WheelchairAccessible,
                    BikesAllowed = template.BikesAllowed
                };

                var offset = departure - first;
                AddTrip(trip, stopTimes.Select(st => new StopTimeModel
                {
                    StopSequence = st.StopSequence,
                    StopId = st.StopId,
                    ArrivalTime = st.ArrivalTime + offset,
                    DepartureTime = st.DepartureTime + offset,
                    StopHeadsign = st.StopHeadsign,
                    PickupType = st.PickupType,
                    DropOffType = st.DropOffType,
                    ContinuousPickup = st.ContinuousPickup,
                    ContinuousDropOff = st.ContinuousDropOff,
                    ShapeDistTraveled = st.ShapeDistTraveled,
                    Timepoint = st.Timepoint
                }).ToList());
                _tripsFromFrequencies++;
            }
        }

        if (tooLate > 0)
        {
            _report.Warning("frequencies.txt", "Vožnje koje bi završile iza 48:00:00 su preskočene.", $"trip_id '{template.Code}': {tooLate}");
        }
    }

    private static string UniqueCode(string code, HashSet<string> usedCodes)
    {
        var candidate = code;
        for (var i = 2; !usedCodes.Add(candidate); i++)
        {
            candidate = $"{code}-{i}";
        }
        return candidate;
    }

    private void AddTrip(TripModel trip, List<StopTimeModel> stopTimes)
    {
        foreach (var stopTime in stopTimes)
        {
            stopTime.TripId = trip.Id;
        }

        _trips.Add(trip);
        _stopTimes.AddRange(stopTimes);
    }

    private static bool TryOptionalTime(string? value, out int? seconds)
    {
        seconds = null;
        if (value is null)
        {
            return true;
        }

        if (GtfsTime.TryParse(value, out var parsed))
        {
            seconds = parsed;
            return true;
        }

        return false;
    }

    internal sealed record RawStopTime(
        int Sequence,
        Guid StopId,
        double? Latitude,
        double? Longitude,
        int? Arrival,
        int? Departure,
        string? StopHeadsign,
        PickupDropOffType PickupType,
        PickupDropOffType DropOffType,
        ContinuousPickupDropOff? ContinuousPickup,
        ContinuousPickupDropOff? ContinuousDropOff,
        double? ShapeDistTraveled,
        bool Timepoint);

    private sealed record FrequencyPeriod(int Start, int End, int Headway);
}
