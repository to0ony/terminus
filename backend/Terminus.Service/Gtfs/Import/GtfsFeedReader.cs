using System.Globalization;
using Terminus.Common;
using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;
using Terminus.Service.Common;

namespace Terminus.Service.Gtfs.Import;

/// <summary>
/// Pretvara GTFS feed u model Terminusa. Pravilo: što bi pokvarilo cijeli feed
/// (nedostaje obavezna datoteka ili stupac, dvostruki ID, različite vremenske
/// zone) je greška i uvoz se odbija; neispravan pojedinačni zapis se preskače ili
/// ispravlja uz upozorenje, a ono što o njemu ovisi preskače se lančano
/// (npr. vožnja na nepostojećoj liniji).
/// </summary>
internal sealed partial class GtfsFeedReader
{
    private const string DefaultTimezone = "Europe/Zagreb";
    private const string DefaultLang = "hr";

    /// <summary>Datoteke koje Terminus čita; ostale se navode kao preskočene.</summary>
    private static readonly HashSet<string> SupportedFiles =
    [
        "agency.txt", "stops.txt", "routes.txt", "trips.txt", "stop_times.txt", "calendar.txt", "calendar_dates.txt",
        "shapes.txt", "frequencies.txt", "fare_attributes.txt", "fare_rules.txt", "feed_info.txt"
    ];

    private readonly GtfsArchive _archive;
    private readonly ImportReportBuilder _report;

    private readonly List<AgencyModel> _agencies = [];
    private readonly Dictionary<string, AgencyModel> _agenciesByCode = new(StringComparer.Ordinal);
    private readonly Dictionary<string, StopModel> _stopsByCode = new(StringComparer.Ordinal);
    private readonly Dictionary<string, RouteModel> _routesByCode = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ServiceCalendarModel> _calendarsByCode = new(StringComparer.Ordinal);
    private readonly List<CalendarDateModel> _calendarDates = [];
    private readonly Dictionary<string, ShapeModel> _shapesByCode = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TripModel> _tripsByCode = new(StringComparer.Ordinal);
    private readonly List<TripModel> _trips = [];
    private readonly List<StopTimeModel> _stopTimes = [];
    private readonly List<FareModel> _fares = [];
    private FeedInfoModel? _feedInfo;
    private string? _feedLang;
    private int _tripsFromFrequencies;
    private int _interpolatedStopTimes;

    private GtfsFeedReader(GtfsArchive archive, ImportReportBuilder report)
    {
        _archive = archive;
        _report = report;
    }

    /// <returns>Podaci feeda i broj zapisa, ili null podaci ako ima grešaka.</returns>
    public static (GtfsFeedData? Feed, GtfsImportCounts Counts) Read(GtfsArchive archive, ImportReportBuilder report)
    {
        var reader = new GtfsFeedReader(archive, report);
        reader.ReadAll();

        var counts = reader.Counts();
        return (report.HasErrors ? null : reader.ToFeed(), counts);
    }

    private void ReadAll()
    {
        foreach (var file in _archive.FileNames.Where(f => !SupportedFiles.Contains(f)).Order(StringComparer.Ordinal))
        {
            _report.SkippedFiles.Add(new GtfsSkippedFile(file, SkipReason(file)));
        }

        foreach (var required in new[] { "agency.txt", "stops.txt", "routes.txt", "trips.txt", "stop_times.txt" })
        {
            if (!_archive.Contains(required))
            {
                _report.Error(required, "Obavezna datoteka nedostaje.");
            }
        }

        if (!_archive.Contains("calendar.txt") && !_archive.Contains("calendar_dates.txt"))
        {
            _report.Error(null, "Feed mora imati calendar.txt ili calendar_dates.txt.");
        }

        if (_report.HasErrors)
        {
            return;
        }

        // feed_info prvi: njegov jezik je zadani jezik prijevoznika.
        ReadFeedInfo();
        ReadAgencies();
        ReadStops();
        ReadRoutes();
        ReadCalendars();
        ReadShapes();
        ReadTrips();
        ReadStopTimesAndFrequencies();
        ReadFares();

        if (!_report.HasErrors && _trips.Count == 0)
        {
            _report.Error(null, "Nakon provjere u feedu nije ostala nijedna vožnja.");
        }
    }

    // ---------------------------------------------------------------- agency.txt

    private void ReadAgencies()
    {
        using var table = OpenRequired("agency.txt", "agency_name", "agency_url", "agency_timezone");
        if (table is null)
        {
            return;
        }

        foreach (var row in table.Rows())
        {
            var name = row["agency_name"];
            var url = row["agency_url"];
            var timezone = row["agency_timezone"];
            var where = Where(row, "agency_id", row["agency_id"] ?? name);

            if (name is null)
            {
                _report.Error(table.FileName, "Prijevoznik nema naziv (agency_name).", where);
                continue;
            }

            if (url is null || !Validators.IsHttpUrl(url))
            {
                _report.Error(table.FileName, "Web adresa prijevoznika (agency_url) mora biti potpuna http ili https adresa.", where);
                continue;
            }

            if (timezone is null || !Validators.IsIanaTimezone(timezone))
            {
                _report.Error(table.FileName, "Vremenska zona prijevoznika (agency_timezone) mora biti IANA naziv, npr. Europe/Zagreb.", where);
                continue;
            }

            var agency = new AgencyModel
            {
                Id = Guid.CreateVersion7(),
                Code = row["agency_id"] ?? Slug.From(name, fallback: "1"),
                Name = name,
                Url = url,
                Timezone = timezone,
                Lang = Language(row["agency_lang"], table.FileName, where) ?? _feedLang ?? DefaultLang,
                Phone = row["agency_phone"],
                FareUrl = OptionalUrl(row["agency_fare_url"], table.FileName, "agency_fare_url", where),
                Email = OptionalEmail(row["agency_email"], table.FileName, where)
            };

            if (!_agenciesByCode.TryAdd(agency.Code, agency))
            {
                _report.Error(table.FileName, "Dvostruki agency_id.", where);
                continue;
            }

            _agencies.Add(agency);
        }

        if (_agencies.Count == 0 && !_report.HasErrors)
        {
            _report.Error(table.FileName, "Feed nema nijednog prijevoznika.");
        }

        if (_agencies.Count > 1 && !table.Has("agency_id"))
        {
            _report.Error(table.FileName, "Feed ima više prijevoznika, pa je stupac agency_id obavezan.");
        }

        // GTFS traži istu zonu za sve prijevoznike; polasci se računaju u njoj.
        if (_agencies.Select(a => a.Timezone).Distinct().Count() > 1)
        {
            _report.Error(table.FileName,
                $"Prijevoznici imaju različite vremenske zone ({string.Join(", ", _agencies.Select(a => a.Timezone).Distinct())}); GTFS traži istu.");
        }
    }

    // ---------------------------------------------------------------- stops.txt

    private void ReadStops()
    {
        using var table = OpenRequired("stops.txt", "stop_id");
        if (table is null)
        {
            return;
        }

        var parentCodes = new Dictionary<StopModel, string>();

        foreach (var row in table.Rows())
        {
            var code = row["stop_id"];
            var where = Where(row, "stop_id", code);

            if (code is null)
            {
                _report.Warning(table.FileName, "Stanica bez stop_id je preskočena.", where);
                continue;
            }

            if (_stopsByCode.ContainsKey(code))
            {
                _report.Error(table.FileName, "Dvostruki stop_id.", where);
                continue;
            }

            if (!TryEnum<LocationType>(row["location_type"], out var locationType, 0))
            {
                _report.Warning(table.FileName, "Stanica s nepoznatim location_type je preskočena.", where);
                continue;
            }

            var latitude = Coordinate(row["stop_lat"], -90, 90);
            var longitude = Coordinate(row["stop_lon"], -180, 180);
            var hasCoordinates = row["stop_lat"] is not null || row["stop_lon"] is not null;
            if (hasCoordinates && (latitude is null || longitude is null))
            {
                _report.Warning(table.FileName, "Neispravne ili nepotpune koordinate stanice su zanemarene.", where);
                latitude = longitude = null;
            }

            // GTFS: koordinate i naziv su obavezni za stajališta, stanice i ulaze.
            var needsLocation = locationType is LocationType.StopOrPlatform or LocationType.Station or LocationType.Entrance;
            var name = row["stop_name"];

            if (needsLocation && (latitude is null || name is null))
            {
                _report.Warning(table.FileName, "Stanica bez naziva ili koordinata je preskočena (obavezni za location_type 0, 1 i 2).", where);
                continue;
            }

            var stop = new StopModel
            {
                Id = Guid.CreateVersion7(),
                Code = code,
                PublicCode = row["stop_code"],
                Name = name ?? code,
                TtsName = row["tts_stop_name"],
                Description = row["stop_desc"],
                Latitude = latitude,
                Longitude = longitude,
                ZoneId = row["zone_id"],
                Url = OptionalUrl(row["stop_url"], table.FileName, "stop_url", where),
                LocationType = locationType,
                Timezone = OptionalTimezone(row["stop_timezone"], table.FileName, where),
                PlatformCode = row["platform_code"],
                WheelchairBoarding = EnumOrDefault(row["wheelchair_boarding"], WheelchairBoarding.Unknown, table.FileName, "wheelchair_boarding", where),
                IsActive = true
            };

            _stopsByCode[code] = stop;
            if (row["parent_station"] is { } parent)
            {
                parentCodes[stop] = parent;
            }
        }

        ResolveParents(table.FileName, parentCodes);
    }

    /// <summary>
    /// Nadređene stanice po pravilima GTFS-a (vidi <see cref="StopService.ParentRule"/>).
    /// Ide od vrha prema dolje: stanice, zatim stajališta, ulazi i čvorovi, zatim
    /// mjesta ukrcavanja, da preskakanje roditelja preskoči i djecu kojoj je obavezan.
    /// </summary>
    private void ResolveParents(string file, Dictionary<StopModel, string> parentCodes)
    {
        LocationType[][] levels =
        [
            [LocationType.Station],
            [LocationType.StopOrPlatform, LocationType.Entrance, LocationType.GenericNode],
            [LocationType.BoardingArea]
        ];

        foreach (var level in levels)
        {
            foreach (var stop in _stopsByCode.Values.Where(s => level.Contains(s.LocationType)).ToList())
            {
                var (parentType, required) = StopService.ParentRule(stop.LocationType);
                var where = $"stop_id '{stop.Code}'";

                if (!parentCodes.TryGetValue(stop, out var parentCode))
                {
                    if (required)
                    {
                        _report.Warning(file, $"{LocationName(stop.LocationType)} bez nadređene stanice (parent_station) je preskočen(o).", where);
                        _stopsByCode.Remove(stop.Code);
                    }
                    continue;
                }

                var parent = parentType is null ? null : _stopsByCode.GetValueOrDefault(parentCode);
                if (parent is not null && parent.LocationType == parentType && parent != stop)
                {
                    stop.ParentStationId = parent.Id;
                }
                else if (required)
                {
                    _report.Warning(file, $"{LocationName(stop.LocationType)} s nepostojećom ili neodgovarajućom nadređenom stanicom je preskočen(o).", where);
                    _stopsByCode.Remove(stop.Code);
                }
                else
                {
                    _report.Warning(file, "Nepostojeća ili neodgovarajuća nadređena stanica (parent_station) je zanemarena.", where);
                }
            }
        }
    }

    private static string LocationName(LocationType type) => type switch
    {
        LocationType.Entrance => "Ulaz",
        LocationType.GenericNode => "Čvor",
        LocationType.BoardingArea => "Mjesto ukrcavanja",
        _ => "Zapis"
    };

    // ---------------------------------------------------------------- feed_info.txt

    private void ReadFeedInfo()
    {
        using var table = OpenOptional("feed_info.txt", "feed_publisher_name", "feed_publisher_url", "feed_lang");
        if (table is null)
        {
            return;
        }

        foreach (var row in table.Rows())
        {
            var where = Where(row);

            if (_feedInfo is not null)
            {
                _report.Warning(table.FileName, "Uvozi se samo prvi redak feed_info.txt.", where);
                break;
            }

            var name = row["feed_publisher_name"];
            var url = row["feed_publisher_url"];
            var lang = row["feed_lang"];

            if (name is null || url is null || !Validators.IsHttpUrl(url)
                || lang is null || (lang != "mul" && !Validators.IsKnownLanguage(lang)))
            {
                _report.Warning(table.FileName,
                    "Podaci o izdavaču nisu potpuni ili ispravni (naziv, http(s) adresa, jezik), pa nisu uvezeni; izvoz će kao izdavača koristiti prvog prijevoznika.",
                    where);
                break;
            }

            _feedLang = lang == "mul" ? null : lang;
            _feedInfo = new FeedInfoModel
            {
                PublisherName = name,
                PublisherUrl = url,
                Lang = lang,
                DefaultLang = Language(row["default_lang"], table.FileName, where),
                ContactEmail = OptionalEmail(row["feed_contact_email"], table.FileName, where),
                ContactUrl = OptionalUrl(row["feed_contact_url"], table.FileName, "feed_contact_url", where)
            };
        }
    }

    // ---------------------------------------------------------------- pomoćno

    /// <summary>Obavezna datoteka; nedostajući obavezni stupci su greška.</summary>
    private GtfsTable? OpenRequired(string fileName, params string[] columns)
    {
        var table = _archive.Open(fileName);
        if (table is null)
        {
            return null;
        }

        var missing = table.Missing(columns);
        if (missing.Count > 0)
        {
            _report.Error(fileName, $"Nedostaju obavezni stupci: {string.Join(", ", missing)}.");
            table.Dispose();
            return null;
        }

        return table;
    }

    /// <summary>Neobavezna datoteka; bez obaveznih stupaca preskače se uz upozorenje.</summary>
    private GtfsTable? OpenOptional(string fileName, params string[] columns)
    {
        var table = _archive.Open(fileName);
        if (table is null)
        {
            return null;
        }

        var missing = table.Missing(columns);
        if (missing.Count > 0)
        {
            _report.Warning(fileName, $"Datoteka nije uvezena jer nedostaju obavezni stupci: {string.Join(", ", missing)}.");
            table.Dispose();
            return null;
        }

        return table;
    }

    private static string Where(GtfsRow row, string? column = null, string? value = null) =>
        column is null || value is null ? $"redak {row.Line}" : $"redak {row.Line}: {column} '{value}'";

    private string? Language(string? value, string file, string where)
    {
        if (value is null)
        {
            return null;
        }

        if (Validators.IsKnownLanguage(value))
        {
            return value;
        }

        _report.Warning(file, "Nepoznata oznaka jezika je zanemarena.", $"{where}: '{value}'");
        return null;
    }

    private string? OptionalUrl(string? value, string file, string column, string where)
    {
        if (value is null || Validators.IsHttpUrl(value))
        {
            return value;
        }

        _report.Warning(file, $"Neispravna adresa ({column}) je zanemarena.", $"{where}: '{value}'");
        return null;
    }

    private string? OptionalEmail(string? value, string file, string where)
    {
        if (value is null || Validators.IsEmail(value))
        {
            return value;
        }

        _report.Warning(file, "Neispravna e-mail adresa je zanemarena.", $"{where}: '{value}'");
        return null;
    }

    private string? OptionalTimezone(string? value, string file, string where)
    {
        if (value is null || Validators.IsIanaTimezone(value))
        {
            return value;
        }

        _report.Warning(file, "Neispravna vremenska zona stanice (stop_timezone) je zanemarena.", $"{where}: '{value}'");
        return null;
    }

    private T EnumOrDefault<T>(string? value, T fallback, string file, string column, string where)
        where T : struct, Enum
    {
        if (TryEnum(value, out var result, fallback))
        {
            return result;
        }

        _report.Warning(file, $"Nepoznata vrijednost {column} zamijenjena je zadanom.", $"{where}: '{value}'");
        return fallback;
    }

    /// <summary>Prazno polje daje <paramref name="fallback"/>; inače broj mora biti definirana vrijednost enuma.</summary>
    private static bool TryEnum<T>(string? value, out T result, T fallback) where T : struct, Enum
    {
        result = fallback;
        if (value is null)
        {
            return true;
        }

        if (short.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
            && Enum.IsDefined(typeof(T), number))
        {
            result = (T)Enum.ToObject(typeof(T), number);
            return true;
        }

        return false;
    }

    /// <summary>Kao <see cref="TryEnum"/>, ali prazno polje daje null.</summary>
    private static bool TryOptionalEnum<T>(string? value, out T? result) where T : struct, Enum
    {
        result = null;
        if (value is null)
        {
            return true;
        }

        if (TryEnum<T>(value, out var parsed, default))
        {
            result = parsed;
            return true;
        }

        return false;
    }

    private static double? Coordinate(string? value, double min, double max) =>
        value is not null
        && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
        && number >= min && number <= max
            ? number
            : null;

    private static bool TryDate(string? value, out DateOnly date) =>
        DateOnly.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    private static bool TryInt(string? value, out int number) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out number);

    private static bool TryDouble(string? value, out double number) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number) && double.IsFinite(number);

    private static string SkipReason(string file) => file switch
    {
        "transfers.txt" => "Presjedanja nisu podržana.",
        "pathways.txt" or "levels.txt" => "Putevi i razine unutar stanica nisu podržani.",
        "translations.txt" => "Prijevodi nisu podržani.",
        "attributions.txt" => "Atribucije nisu podržane.",
        "fare_media.txt" or "fare_products.txt" or "fare_leg_rules.txt" or "fare_leg_join_rules.txt"
            or "fare_transfer_rules.txt" or "rider_categories.txt" or "timeframes.txt" or "areas.txt"
            or "stop_areas.txt" or "networks.txt" or "route_networks.txt" => "Fares v2 nije podržan; podržane su cijene iz fare_attributes.txt.",
        "booking_rules.txt" or "location_groups.txt" or "location_group_stops.txt" or "locations.geojson"
            => "GTFS-Flex (prijevoz na poziv) nije podržan.",
        _ => "Datoteka nije dio GTFS-a koji Terminus podržava."
    };

    private GtfsImportCounts Counts() => new(
        Agencies: _agencies.Count,
        Stops: _stopsByCode.Count,
        Routes: _routesByCode.Count,
        Calendars: _calendarsByCode.Count,
        CalendarDates: _calendarDates.Count,
        Trips: _trips.Count,
        StopTimes: _stopTimes.Count,
        Shapes: _shapesByCode.Count,
        ShapePoints: _shapesByCode.Values.Sum(s => s.Points.Count),
        Fares: _fares.Count,
        FareRules: _fares.Sum(f => f.Rules.Count),
        FeedInfo: _feedInfo is not null,
        TripsFromFrequencies: _tripsFromFrequencies,
        InterpolatedStopTimes: _interpolatedStopTimes);

    private GtfsFeedData ToFeed() => new()
    {
        Agencies = _agencies,
        Stops = _stopsByCode.Values.ToList(),
        Routes = _routesByCode.Values.ToList(),
        Calendars = _calendarsByCode.Values.ToList(),
        CalendarDates = _calendarDates,
        Shapes = _shapesByCode.Values.ToList(),
        Trips = _trips,
        StopTimes = _stopTimes,
        Fares = _fares,
        FeedInfo = _feedInfo
    };
}
