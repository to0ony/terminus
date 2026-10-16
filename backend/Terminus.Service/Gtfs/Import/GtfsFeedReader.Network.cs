using System.Globalization;
using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;

namespace Terminus.Service.Gtfs.Import;

internal sealed partial class GtfsFeedReader
{
    // ---------------------------------------------------------------- routes.txt

    private void ReadRoutes()
    {
        using var table = OpenRequired("routes.txt", "route_id", "route_type");
        if (table is null)
        {
            return;
        }

        foreach (var row in table.Rows())
        {
            var code = row["route_id"];
            var where = Where(row, "route_id", code);

            if (code is null)
            {
                _report.Warning(table.FileName, "Linija bez route_id je preskočena.", where);
                continue;
            }

            if (_routesByCode.ContainsKey(code))
            {
                _report.Error(table.FileName, "Dvostruki route_id.", where);
                continue;
            }

            // agency_id smije izostati samo kad feed ima jednog prijevoznika.
            var agencyCode = row["agency_id"];
            var agency = agencyCode is null
                ? (_agencies.Count == 1 ? _agencies[0] : null)
                : _agenciesByCode.GetValueOrDefault(agencyCode);

            if (agency is null)
            {
                _report.Warning(table.FileName,
                    agencyCode is null
                        ? "Linija bez agency_id je preskočena (feed ima više prijevoznika)."
                        : "Linija s nepostojećim prijevoznikom (agency_id) je preskočena.",
                    where);
                continue;
            }

            if (!TryInt(row["route_type"], out var routeTypeValue) || routeTypeValue is < short.MinValue or > short.MaxValue
                || !RouteTypes.IsValid((RouteType)routeTypeValue))
            {
                _report.Warning(table.FileName, "Linija s nepoznatom vrstom prijevoza (route_type) je preskočena.", where);
                continue;
            }

            var shortName = row["route_short_name"];
            var longName = row["route_long_name"];
            if (shortName is null && longName is null)
            {
                _report.Warning(table.FileName, "Linija nema ni kratku oznaku ni naziv; kao oznaka koristi se route_id.", where);
                shortName = code;
            }

            int? sortOrder = null;
            if (row["route_sort_order"] is { } sortValue)
            {
                if (TryInt(sortValue, out var sort) && sort >= 0)
                {
                    sortOrder = sort;
                }
                else
                {
                    _report.Warning(table.FileName, "Neispravan route_sort_order je zanemaren.", where);
                }
            }

            _routesByCode[code] = new RouteModel
            {
                Id = Guid.CreateVersion7(),
                AgencyId = agency.Id,
                Code = code,
                ShortName = shortName,
                LongName = longName,
                Description = row["route_desc"],
                RouteType = (RouteType)routeTypeValue,
                Url = OptionalUrl(row["route_url"], table.FileName, "route_url", where),
                Color = Color(row["route_color"], table.FileName, "route_color", where),
                TextColor = Color(row["route_text_color"], table.FileName, "route_text_color", where),
                SortOrder = sortOrder,
                ContinuousPickup = OptionalEnum<ContinuousPickupDropOff>(row["continuous_pickup"], table.FileName, "continuous_pickup", where),
                ContinuousDropOff = OptionalEnum<ContinuousPickupDropOff>(row["continuous_drop_off"], table.FileName, "continuous_drop_off", where),
                NetworkId = row["network_id"],
                IsActive = true
            };
        }
    }

    private string? Color(string? value, string file, string column, string where)
    {
        var color = Validators.NormalizeColor(value);
        if (color is null || Validators.IsHexColor(color))
        {
            return color;
        }

        _report.Warning(file, $"Neispravna boja ({column}) je zanemarena.", $"{where}: '{value}'");
        return null;
    }

    private T? OptionalEnum<T>(string? value, string file, string column, string where) where T : struct, Enum
    {
        if (TryOptionalEnum<T>(value, out var result))
        {
            return result;
        }

        _report.Warning(file, $"Nepoznata vrijednost {column} je zanemarena.", $"{where}: '{value}'");
        return null;
    }

    // ---------------------------------------------------------------- calendar.txt, calendar_dates.txt

    private void ReadCalendars()
    {
        string[] weekdays = ["monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday"];

        using (var table = _archive.Contains("calendar.txt")
                   ? OpenRequired("calendar.txt", ["service_id", .. weekdays, "start_date", "end_date"])
                   : null)
        {
            foreach (var row in table?.Rows() ?? [])
            {
                var code = row["service_id"];
                var where = Where(row, "service_id", code);

                if (code is null)
                {
                    _report.Warning(table!.FileName, "Kalendar bez service_id je preskočen.", where);
                    continue;
                }

                if (_calendarsByCode.ContainsKey(code))
                {
                    _report.Error(table!.FileName, "Dvostruki service_id.", where);
                    continue;
                }

                var days = weekdays.Select(d => row[d]).ToArray();
                if (days.Any(d => d is not ("0" or "1"))
                    || !TryDate(row["start_date"], out var start)
                    || !TryDate(row["end_date"], out var end))
                {
                    _report.Warning(table!.FileName, "Kalendar s neispravnim danima (0/1) ili datumima (YYYYMMDD) je preskočen.", where);
                    continue;
                }

                if (end < start)
                {
                    _report.Warning(table!.FileName, "Kalendar koji završava prije početka je preskočen.", where);
                    continue;
                }

                _calendarsByCode[code] = new ServiceCalendarModel
                {
                    Id = Guid.CreateVersion7(),
                    Code = code,
                    Monday = days[0] == "1",
                    Tuesday = days[1] == "1",
                    Wednesday = days[2] == "1",
                    Thursday = days[3] == "1",
                    Friday = days[4] == "1",
                    Saturday = days[5] == "1",
                    Sunday = days[6] == "1",
                    StartDate = start,
                    EndDate = end
                };
            }
        }

        ReadCalendarDates();

        var addedDates = _calendarDates
            .Where(d => d.ExceptionType == CalendarExceptionType.Added)
            .ToLookup(d => d.ServiceCalendarId, d => d.Date);

        foreach (var calendar in _calendarsByCode.Values)
        {
            calendar.Name = CalendarName(calendar, addedDates[calendar.Id].ToList());
        }
    }

    private void ReadCalendarDates()
    {
        using var table = _archive.Contains("calendar_dates.txt")
            ? OpenRequired("calendar_dates.txt", "service_id", "date", "exception_type")
            : null;
        if (table is null)
        {
            return;
        }

        // Servisi zadani samo datumima (bez retka u calendar.txt): kalendar bez
        // dana u tjednu, s razdobljem od prvog do zadnjeg datuma.
        var datesOnly = new Dictionary<string, List<(DateOnly Date, CalendarExceptionType Type)>>(StringComparer.Ordinal);
        var seen = new HashSet<(string, DateOnly)>();

        foreach (var row in table.Rows())
        {
            var code = row["service_id"];
            var where = Where(row, "service_id", code);

            if (code is null || !TryDate(row["date"], out var date)
                || !TryEnum<CalendarExceptionType>(row["exception_type"], out var type, default) || type == default)
            {
                _report.Warning(table.FileName, "Iznimka s neispravnim service_id, datumom ili exception_type (1/2) je preskočena.", where);
                continue;
            }

            if (!seen.Add((code, date)))
            {
                _report.Warning(table.FileName, "Dvostruka iznimka za isti datum je preskočena.", $"{where}, datum {row["date"]}");
                continue;
            }

            if (_calendarsByCode.TryGetValue(code, out var calendar))
            {
                _calendarDates.Add(new CalendarDateModel { ServiceCalendarId = calendar.Id, Date = date, ExceptionType = type });
            }
            else
            {
                if (!datesOnly.TryGetValue(code, out var list))
                {
                    datesOnly[code] = list = [];
                }
                list.Add((date, type));
            }
        }

        foreach (var (code, dates) in datesOnly)
        {
            var calendar = new ServiceCalendarModel
            {
                Id = Guid.CreateVersion7(),
                Code = code,
                StartDate = dates.Min(d => d.Date),
                EndDate = dates.Max(d => d.Date)
            };
            _calendarsByCode[code] = calendar;

            _calendarDates.AddRange(dates.Select(d => new CalendarDateModel
            {
                ServiceCalendarId = calendar.Id,
                Date = d.Date,
                ExceptionType = d.Type
            }));

            if (dates.All(d => d.Type == CalendarExceptionType.Removed))
            {
                _report.Warning(table.FileName, "Servis zadan samo datumima kad ne vozi nikad ne vozi.", $"service_id '{code}'");
            }
        }
    }

    /// <summary>
    /// Naziv za prikaz i stupac PDF voznog reda (npr. "Radni dan"), iz dana u tjednu.
    /// Kalendar bez dana u tjednu (cijeli raspored u calendar_dates.txt) imenuje se
    /// po danima na koje dodani datumi padaju: dani s barem pola najčešćeg broja
    /// datuma su njegovi dani, a rijetki ostali su praznici ("Nedjelja i praznici").
    /// Do tri datuma navode se izravno.
    /// </summary>
    internal static string CalendarName(IServiceCalendarModel c, IReadOnlyCollection<DateOnly> addedDates)
    {
        bool[] days = [c.Monday, c.Tuesday, c.Wednesday, c.Thursday, c.Friday, c.Saturday, c.Sunday];
        if (days.Any(d => d) || addedDates.Count == 0)
        {
            return WeekdayName(days);
        }

        if (addedDates.Count <= 3)
        {
            return "Posebni datumi: " + string.Join(", ", addedDates.Order().Select(d => d.ToString("d.M.", CultureInfo.InvariantCulture)));
        }

        var perDay = new int[7];
        foreach (var date in addedDates)
        {
            perDay[((int)date.DayOfWeek + 6) % 7]++;
        }

        var threshold = perDay.Max() / 2.0;
        var main = perDay.Select(count => count > 0 && count >= threshold).ToArray();
        var name = WeekdayName(main);

        return perDay.Where((count, i) => count > 0 && !main[i]).Any() ? $"{name} i praznici" : name;
    }

    /// <param name="days">Ponedjeljak do nedjelja.</param>
    private static string WeekdayName(bool[] days)
    {
        string[] full = ["Ponedjeljak", "Utorak", "Srijeda", "Četvrtak", "Petak", "Subota", "Nedjelja"];
        string[] shortNames = ["Pon", "Uto", "Sri", "Čet", "Pet", "Sub", "Ned"];

        var count = days.Count(d => d);
        var workdays = days[..5].All(d => d);

        return count switch
        {
            0 => "Posebni datumi",
            7 => "Svaki dan",
            1 => full[Array.IndexOf(days, true)],
            5 when workdays => "Radni dan",
            6 when workdays && days[5] => "Radni dan i subota",
            2 when days[5] && days[6] => "Subota i nedjelja",
            _ => string.Join(", ", shortNames.Where((_, i) => days[i]))
        };
    }

    // ---------------------------------------------------------------- shapes.txt

    private void ReadShapes()
    {
        using var table = OpenOptional("shapes.txt", "shape_id", "shape_pt_lat", "shape_pt_lon", "shape_pt_sequence");
        if (table is null)
        {
            return;
        }

        var points = new Dictionary<string, List<(int Sequence, ShapePointModel Point)>>(StringComparer.Ordinal);

        foreach (var row in table.Rows())
        {
            var code = row["shape_id"];
            var where = Where(row, "shape_id", code);
            var latitude = Coordinate(row["shape_pt_lat"], -90, 90);
            var longitude = Coordinate(row["shape_pt_lon"], -180, 180);

            if (code is null || latitude is null || longitude is null
                || !TryInt(row["shape_pt_sequence"], out var sequence) || sequence < 0)
            {
                _report.Warning(table.FileName, "Točka trase s neispravnim shape_id, koordinatama ili rednim brojem je preskočena.", where);
                continue;
            }

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

            if (!points.TryGetValue(code, out var list))
            {
                points[code] = list = [];
            }
            list.Add((sequence, new ShapePointModel { Latitude = latitude.Value, Longitude = longitude.Value, DistTraveled = dist }));
        }

        foreach (var (code, list) in points)
        {
            var where = $"shape_id '{code}'";
            var ordered = list.OrderBy(p => p.Sequence).ToList();

            if (ordered.Select(p => p.Sequence).Distinct().Count() != ordered.Count)
            {
                _report.Warning(table.FileName, "Trasa s dvostrukim rednim brojem točke je preskočena.", where);
                continue;
            }

            if (ordered.Count < 2)
            {
                _report.Warning(table.FileName, "Trasa s manje od dvije točke je preskočena.", where);
                continue;
            }

            var shapePoints = ordered.Select(p => p.Point).ToList();
            if (!IsNonDecreasing(shapePoints.Select(p => p.DistTraveled)))
            {
                _report.Warning(table.FileName, "shape_dist_traveled trase se smanjuje, pa je zanemaren za cijelu trasu.", where);
                shapePoints.ForEach(p => p.DistTraveled = null);
            }

            for (var i = 0; i < shapePoints.Count; i++)
            {
                shapePoints[i].Sequence = i + 1;
            }

            _shapesByCode[code] = new ShapeModel
            {
                Id = Guid.CreateVersion7(),
                Code = code,
                Points = shapePoints,
                PointCount = shapePoints.Count
            };
        }
    }

    /// <summary>Zadane vrijednosti ne smiju se smanjivati; prazne se preskaču.</summary>
    private static bool IsNonDecreasing(IEnumerable<double?> values)
    {
        double? previous = null;
        foreach (var value in values)
        {
            if (value is null)
            {
                continue;
            }

            if (previous is not null && value < previous)
            {
                return false;
            }

            previous = value;
        }

        return true;
    }

    // ---------------------------------------------------------------- fare_attributes.txt, fare_rules.txt

    private void ReadFares()
    {
        var faresByCode = new Dictionary<string, (FareModel Fare, List<IFareRuleModel> Rules)>(StringComparer.Ordinal);

        using (var table = OpenOptional("fare_attributes.txt", "fare_id", "price", "currency_type", "payment_method", "transfers"))
        {
            foreach (var row in table?.Rows() ?? [])
            {
                var code = row["fare_id"];
                var where = Where(row, "fare_id", code);
                var currency = row["currency_type"]?.ToUpperInvariant();

                if (code is null)
                {
                    _report.Warning(table!.FileName, "Cijena bez fare_id je preskočena.", where);
                    continue;
                }

                if (faresByCode.ContainsKey(code))
                {
                    _report.Error(table!.FileName, "Dvostruki fare_id.", where);
                    continue;
                }

                if (!decimal.TryParse(row["price"], NumberStyles.Number, CultureInfo.InvariantCulture, out var price) || price < 0
                    || currency is null || currency.Length != 3 || !currency.All(char.IsAsciiLetterUpper)
                    || !TryEnum<FarePaymentMethod>(row["payment_method"], out var payment, default) || row["payment_method"] is null
                    || !TryOptionalEnum<FareTransfers>(row["transfers"], out var transfers))
                {
                    _report.Warning(table!.FileName, "Cijena s neispravnom cijenom, valutom, načinom plaćanja ili brojem presjedanja je preskočena.", where);
                    continue;
                }

                Guid? agencyId = null;
                if (row["agency_id"] is { } agencyCode)
                {
                    if (!_agenciesByCode.TryGetValue(agencyCode, out var agency))
                    {
                        _report.Warning(table!.FileName, "Cijena s nepostojećim prijevoznikom (agency_id) je preskočena.", where);
                        continue;
                    }
                    agencyId = agency.Id;
                }
                else if (_agencies.Count > 1)
                {
                    _report.Warning(table!.FileName,
                        "Cijena nema agency_id, a feed ima više prijevoznika; izvoz će to tražiti ispraviti.", where);
                }

                int? transferDuration = null;
                if (row["transfer_duration"] is { } durationValue)
                {
                    if (TryInt(durationValue, out var duration) && duration >= 0)
                    {
                        transferDuration = duration;
                    }
                    else
                    {
                        _report.Warning(table!.FileName, "Neispravan transfer_duration je zanemaren.", where);
                    }
                }

                var fare = new FareModel
                {
                    Id = Guid.CreateVersion7(),
                    Code = code,
                    Price = price,
                    CurrencyType = currency,
                    PaymentMethod = payment,
                    Transfers = transfers,
                    AgencyId = agencyId,
                    TransferDuration = transferDuration
                };
                faresByCode[code] = (fare, []);
            }
        }

        using (var table = faresByCode.Count > 0 ? OpenOptional("fare_rules.txt", "fare_id") : null)
        {
            var zones = _stopsByCode.Values.Select(s => s.ZoneId).OfType<string>().ToHashSet(StringComparer.Ordinal);

            foreach (var row in table?.Rows() ?? [])
            {
                var code = row["fare_id"];
                var where = Where(row, "fare_id", code);

                if (code is null || !faresByCode.TryGetValue(code, out var fare))
                {
                    _report.Warning(table!.FileName, "Pravilo za nepostojeću cijenu je preskočeno.", where);
                    continue;
                }

                Guid? routeId = null;
                if (row["route_id"] is { } routeCode)
                {
                    if (!_routesByCode.TryGetValue(routeCode, out var route))
                    {
                        _report.Warning(table!.FileName, "Pravilo za nepostojeću liniju (route_id) je preskočeno.", where);
                        continue;
                    }
                    routeId = route.Id;
                }

                var rule = new FareRuleModel
                {
                    RouteId = routeId,
                    OriginId = row["origin_id"],
                    DestinationId = row["destination_id"],
                    ContainsId = row["contains_id"]
                };

                if (rule.RouteId is null && rule.OriginId is null && rule.DestinationId is null && rule.ContainsId is null)
                {
                    _report.Warning(table!.FileName, "Pravilo bez linije i zona je preskočeno.", where);
                    continue;
                }

                foreach (var zone in new[] { rule.OriginId, rule.DestinationId, rule.ContainsId }.OfType<string>())
                {
                    if (!zones.Contains(zone))
                    {
                        _report.Warning(table!.FileName, "Pravilo koristi zonu koju nema nijedna stanica; izvoz će to tražiti ispraviti.", $"{where}: zona '{zone}'");
                    }
                }

                fare.Rules.Add(rule);
            }
        }

        foreach (var (fare, rules) in faresByCode.Values)
        {
            fare.Rules = rules;
            _fares.Add(fare);
        }
    }
}
