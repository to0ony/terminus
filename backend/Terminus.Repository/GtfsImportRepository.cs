using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using Terminus.DAL;
using Terminus.DAL.Entities;
using Terminus.Model.Common;
using Terminus.Repository.Common;

namespace Terminus.Repository;

/// <summary>
/// Zamjena voznog reda: TRUNCATE i binarni COPY u jednoj transakciji. COPY je
/// za red veličine brži od INSERT-a kroz EF, što je bitno za stop_times
/// (stotine tisuća do milijuni redaka). created_at i updated_at postavlja baza.
/// </summary>
public class GtfsImportRepository(TerminusDbContext context) : IGtfsImportRepository
{
    private static readonly TimeSpan CopyTimeout = TimeSpan.FromMinutes(10);

    public async Task ReplaceAllAsync(GtfsFeedData feed, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();

        await context.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE stop_times, trips, route_variant_stops, route_variants, fare_rules, fares, shape_points, shapes, calendar_dates, " +
            "season_days, service_calendars, timetable_seasons, routes, stops, agencies, feed_info",
            cancellationToken);

        // Redoslijed prati strane ključeve.
        await CopyAsync(connection, "agencies", feed.Agencies, cancellationToken,
            ("id", NpgsqlDbType.Uuid, a => a.Id),
            ("code", NpgsqlDbType.Text, a => a.Code),
            ("name", NpgsqlDbType.Text, a => a.Name),
            ("url", NpgsqlDbType.Text, a => a.Url),
            ("timezone", NpgsqlDbType.Text, a => a.Timezone),
            ("lang", NpgsqlDbType.Text, a => a.Lang),
            ("phone", NpgsqlDbType.Text, a => a.Phone),
            ("fare_url", NpgsqlDbType.Text, a => a.FareUrl),
            ("email", NpgsqlDbType.Text, a => a.Email));

        // Strani ključ na nadređenu stanicu provjerava se na kraju naredbe, pa redoslijed redaka nije bitan.
        await CopyAsync(connection, "stops", feed.Stops, cancellationToken,
            ("id", NpgsqlDbType.Uuid, s => s.Id),
            ("code", NpgsqlDbType.Text, s => s.Code),
            ("public_code", NpgsqlDbType.Text, s => s.PublicCode),
            ("name", NpgsqlDbType.Text, s => s.Name),
            ("tts_name", NpgsqlDbType.Text, s => s.TtsName),
            ("description", NpgsqlDbType.Text, s => s.Description),
            ("latitude", NpgsqlDbType.Double, s => s.Latitude),
            ("longitude", NpgsqlDbType.Double, s => s.Longitude),
            ("zone_id", NpgsqlDbType.Text, s => s.ZoneId),
            ("url", NpgsqlDbType.Text, s => s.Url),
            ("location_type", NpgsqlDbType.Smallint, s => (short)s.LocationType),
            ("parent_station_id", NpgsqlDbType.Uuid, s => s.ParentStationId),
            ("timezone", NpgsqlDbType.Text, s => s.Timezone),
            ("platform_code", NpgsqlDbType.Text, s => s.PlatformCode),
            ("wheelchair_boarding", NpgsqlDbType.Smallint, s => (short)s.WheelchairBoarding),
            ("is_active", NpgsqlDbType.Boolean, s => s.IsActive));

        await CopyAsync(connection, "routes", feed.Routes, cancellationToken,
            ("id", NpgsqlDbType.Uuid, r => r.Id),
            ("agency_id", NpgsqlDbType.Uuid, r => r.AgencyId),
            ("code", NpgsqlDbType.Text, r => r.Code),
            ("short_name", NpgsqlDbType.Text, r => r.ShortName),
            ("long_name", NpgsqlDbType.Text, r => r.LongName),
            ("description", NpgsqlDbType.Text, r => r.Description),
            ("route_type", NpgsqlDbType.Smallint, r => (short)r.RouteType),
            ("url", NpgsqlDbType.Text, r => r.Url),
            ("color", NpgsqlDbType.Char, r => r.Color),
            ("text_color", NpgsqlDbType.Char, r => r.TextColor),
            ("sort_order", NpgsqlDbType.Integer, r => r.SortOrder),
            ("continuous_pickup", NpgsqlDbType.Smallint, r => (short?)r.ContinuousPickup),
            ("continuous_drop_off", NpgsqlDbType.Smallint, r => (short?)r.ContinuousDropOff),
            ("network_id", NpgsqlDbType.Text, r => r.NetworkId),
            ("is_active", NpgsqlDbType.Boolean, r => r.IsActive));

        await CopyAsync(connection, "service_calendars", feed.Calendars, cancellationToken,
            ("id", NpgsqlDbType.Uuid, c => c.Id),
            ("code", NpgsqlDbType.Text, c => c.Code),
            ("name", NpgsqlDbType.Text, c => c.Name),
            ("monday", NpgsqlDbType.Boolean, c => c.Monday),
            ("tuesday", NpgsqlDbType.Boolean, c => c.Tuesday),
            ("wednesday", NpgsqlDbType.Boolean, c => c.Wednesday),
            ("thursday", NpgsqlDbType.Boolean, c => c.Thursday),
            ("friday", NpgsqlDbType.Boolean, c => c.Friday),
            ("saturday", NpgsqlDbType.Boolean, c => c.Saturday),
            ("sunday", NpgsqlDbType.Boolean, c => c.Sunday),
            ("start_date", NpgsqlDbType.Date, c => c.StartDate),
            ("end_date", NpgsqlDbType.Date, c => c.EndDate));

        await CopyAsync(connection, "calendar_dates", feed.CalendarDates, cancellationToken,
            ("service_calendar_id", NpgsqlDbType.Uuid, d => d.ServiceCalendarId),
            ("date", NpgsqlDbType.Date, d => d.Date),
            ("exception_type", NpgsqlDbType.Smallint, d => (short)d.ExceptionType));

        await CopyAsync(connection, "shapes", feed.Shapes, cancellationToken,
            ("id", NpgsqlDbType.Uuid, s => s.Id),
            ("code", NpgsqlDbType.Text, s => s.Code));

        await CopyAsync(connection, "shape_points",
            feed.Shapes.SelectMany(s => s.Points.Select(p => (ShapeId: s.Id, Point: p))), cancellationToken,
            ("shape_id", NpgsqlDbType.Uuid, x => x.ShapeId),
            ("sequence", NpgsqlDbType.Integer, x => x.Point.Sequence),
            ("latitude", NpgsqlDbType.Double, x => x.Point.Latitude),
            ("longitude", NpgsqlDbType.Double, x => x.Point.Longitude),
            ("dist_traveled", NpgsqlDbType.Double, x => x.Point.DistTraveled));

        await CopyAsync(connection, "trips", feed.Trips, cancellationToken,
            ("id", NpgsqlDbType.Uuid, t => t.Id),
            ("route_id", NpgsqlDbType.Uuid, t => t.RouteId),
            ("service_calendar_id", NpgsqlDbType.Uuid, t => t.ServiceCalendarId),
            ("code", NpgsqlDbType.Text, t => t.Code),
            ("headsign", NpgsqlDbType.Text, t => t.Headsign),
            ("short_name", NpgsqlDbType.Text, t => t.ShortName),
            ("direction_id", NpgsqlDbType.Smallint, t => (short?)t.DirectionId),
            ("block_id", NpgsqlDbType.Text, t => t.BlockId),
            ("shape_id", NpgsqlDbType.Uuid, t => t.ShapeId),
            ("wheelchair_accessible", NpgsqlDbType.Smallint, t => (short)t.WheelchairAccessible),
            ("bikes_allowed", NpgsqlDbType.Smallint, t => (short)t.BikesAllowed));

        await CopyAsync(connection, "stop_times", feed.StopTimes, cancellationToken,
            ("trip_id", NpgsqlDbType.Uuid, st => st.TripId),
            ("stop_sequence", NpgsqlDbType.Integer, st => st.StopSequence),
            ("stop_id", NpgsqlDbType.Uuid, st => st.StopId),
            ("arrival_time", NpgsqlDbType.Integer, st => st.ArrivalTime),
            ("departure_time", NpgsqlDbType.Integer, st => st.DepartureTime),
            ("stop_headsign", NpgsqlDbType.Text, st => st.StopHeadsign),
            ("pickup_type", NpgsqlDbType.Smallint, st => (short)st.PickupType),
            ("drop_off_type", NpgsqlDbType.Smallint, st => (short)st.DropOffType),
            ("continuous_pickup", NpgsqlDbType.Smallint, st => (short?)st.ContinuousPickup),
            ("continuous_drop_off", NpgsqlDbType.Smallint, st => (short?)st.ContinuousDropOff),
            ("shape_dist_traveled", NpgsqlDbType.Double, st => st.ShapeDistTraveled),
            ("timepoint", NpgsqlDbType.Boolean, st => st.Timepoint));

        await CopyAsync(connection, "fares", feed.Fares, cancellationToken,
            ("id", NpgsqlDbType.Uuid, f => f.Id),
            ("code", NpgsqlDbType.Text, f => f.Code),
            ("price", NpgsqlDbType.Numeric, f => f.Price),
            ("currency_type", NpgsqlDbType.Char, f => f.CurrencyType),
            ("payment_method", NpgsqlDbType.Smallint, f => (short)f.PaymentMethod),
            ("transfers", NpgsqlDbType.Smallint, f => (short?)f.Transfers),
            ("agency_id", NpgsqlDbType.Uuid, f => f.AgencyId),
            ("transfer_duration", NpgsqlDbType.Integer, f => f.TransferDuration));

        await CopyAsync(connection, "fare_rules",
            feed.Fares.SelectMany(f => f.Rules.Select((r, i) => (FareId: f.Id, Sequence: i + 1, Rule: r))), cancellationToken,
            ("id", NpgsqlDbType.Uuid, x => Guid.CreateVersion7()),
            ("fare_id", NpgsqlDbType.Uuid, x => x.FareId),
            ("sequence", NpgsqlDbType.Integer, x => x.Sequence),
            ("route_id", NpgsqlDbType.Uuid, x => x.Rule.RouteId),
            ("origin_id", NpgsqlDbType.Text, x => x.Rule.OriginId),
            ("destination_id", NpgsqlDbType.Text, x => x.Rule.DestinationId),
            ("contains_id", NpgsqlDbType.Text, x => x.Rule.ContainsId));

        if (feed.FeedInfo is { } info)
        {
            await CopyAsync(connection, "feed_info", [info], cancellationToken,
                ("id", NpgsqlDbType.Smallint, _ => FeedInfo.SingletonId),
                ("publisher_name", NpgsqlDbType.Text, f => f.PublisherName),
                ("publisher_url", NpgsqlDbType.Text, f => f.PublisherUrl),
                ("lang", NpgsqlDbType.Text, f => f.Lang),
                ("default_lang", NpgsqlDbType.Text, f => f.DefaultLang),
                ("contact_email", NpgsqlDbType.Text, f => f.ContactEmail),
                ("contact_url", NpgsqlDbType.Text, f => f.ContactUrl));
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task CopyAsync<T>(
        NpgsqlConnection connection,
        string table,
        IEnumerable<T> rows,
        CancellationToken cancellationToken,
        params (string Name, NpgsqlDbType Type, Func<T, object?> Value)[] columns)
    {
        var columnList = string.Join(", ", columns.Select(c => c.Name));

        await using var writer = await connection.BeginBinaryImportAsync(
            $"COPY {table} ({columnList}) FROM STDIN (FORMAT BINARY)", cancellationToken);
        writer.Timeout = CopyTimeout;

        // Sinkrono pisanje u međuspremnik: async poziv po vrijednosti (milijuni za
        // stop_times) višestruko usporava upis, a čeka se samo kad se međuspremnik prazni.
        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            writer.StartRow();
            foreach (var (_, type, value) in columns)
            {
                Write(writer, value(row), type);
            }
        }

        await writer.CompleteAsync(cancellationToken);
    }

    private static void Write(NpgsqlBinaryImporter writer, object? value, NpgsqlDbType type)
    {
        switch (value)
        {
            case null: writer.WriteNull(); break;
            case Guid v: writer.Write(v, type); break;
            case string v: writer.Write(v, type); break;
            case short v: writer.Write(v, type); break;
            case int v: writer.Write(v, type); break;
            case double v: writer.Write(v, type); break;
            case decimal v: writer.Write(v, type); break;
            case bool v: writer.Write(v, type); break;
            case DateOnly v: writer.Write(v, type); break;
            default: throw new NotSupportedException($"COPY ne podržava vrijednost tipa {value.GetType()}.");
        }
    }
}
