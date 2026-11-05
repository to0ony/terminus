using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using Terminus.DAL;
using Terminus.DAL.Entities;
using Terminus.Model.Common;
using Terminus.Repository.Common;
using Terminus.Repository.Mapping;

namespace Terminus.Repository;

/// <summary>
/// Zamjena voznog reda u jednoj transakciji: TRUNCATE, pa upis feeda. Manje
/// tablice idu kroz EF, a kalendari, točke trasa, vožnje i vremena binarnim
/// COPY-jem, jer ih može biti milijuni. created_at i updated_at postavlja baza.
/// </summary>
public class GtfsImportRepository(TerminusDbContext context) : IGtfsImportRepository
{
    private static readonly TimeSpan CopyTimeout = TimeSpan.FromMinutes(10);

    /// <summary>Sve tablice voznog reda; korisnici i postavke instance ostaju.</summary>
    private const string TruncateSql =
        "TRUNCATE TABLE stop_times, trips, route_variant_stops, route_variants, fare_rules, fares, shape_points, shapes, " +
        "calendar_dates, season_periods, season_days, service_calendars, timetable_seasons, routes, stops, agencies, feed_info";

    public async Task ReplaceAllAsync(GtfsFeedData feed, IReadOnlyList<IRouteVariantModel> variants, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await context.Database.ExecuteSqlRawAsync(TruncateSql, cancellationToken);

        AddBase(feed, variants);
        await context.SaveChangesAsync(cancellationToken);

        // Redoslijed prati strane ključeve.
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();

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
            ("end_date", NpgsqlDbType.Date, c => c.EndDate),
            ("season_id", NpgsqlDbType.Uuid, c => c.SeasonId));

        await CopyAsync(connection, "calendar_dates", feed.CalendarDates, cancellationToken,
            ("service_calendar_id", NpgsqlDbType.Uuid, d => d.ServiceCalendarId),
            ("date", NpgsqlDbType.Date, d => d.Date),
            ("exception_type", NpgsqlDbType.Smallint, d => (short)d.ExceptionType));

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
            ("variant_id", NpgsqlDbType.Uuid, t => t.VariantId),
            ("departure_id", NpgsqlDbType.Uuid, t => t.DepartureId),
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

        // Dani pokazuju na kalendare, pa idu nakon njih.
        context.SeasonDays.AddRange(feed.SeasonDays.Select(d => new DAL.Entities.SeasonDay
        {
            SeasonId = feed.Season!.Id,
            Date = d.Date,
            Mode = (short)d.Mode,
            CalendarId = d.CalendarId,
            Source = (short)d.Source,
            Note = d.Note
        }));
        await context.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    /// <summary>Tablice koje su male i imaju svoje mappere; Id-evi dolaze iz feeda.</summary>
    private void AddBase(GtfsFeedData feed, IReadOnlyList<IRouteVariantModel> variants)
    {
        foreach (var agency in feed.Agencies)
        {
            var entity = new Agency { Id = agency.Id };
            AgencyMapper.Apply(agency, entity);
            context.Agencies.Add(entity);
        }

        foreach (var stop in feed.Stops)
        {
            var entity = new Stop { Id = stop.Id };
            StopMapper.Apply(stop, entity);
            context.Stops.Add(entity);
        }

        foreach (var route in feed.Routes)
        {
            var entity = new Route { Id = route.Id };
            RouteMapper.Apply(route, entity);
            context.Routes.Add(entity);
        }

        foreach (var shape in feed.Shapes)
        {
            context.Shapes.Add(new Shape { Id = shape.Id, Code = shape.Code });
        }

        foreach (var variant in variants)
        {
            var entity = new RouteVariant { Id = variant.Id };
            RouteVariantMapper.Apply(variant, entity);
            context.RouteVariants.Add(entity);
        }

        foreach (var fare in feed.Fares)
        {
            var entity = new Fare { Id = fare.Id };
            FareMapper.Apply(fare, entity);
            entity.Rules = fare.Rules.Select((rule, i) => FareMapper.ToEntity(fare.Id, i + 1, rule)).ToList();
            context.Fares.Add(entity);
        }

        if (feed.FeedInfo is not null)
        {
            var entity = new FeedInfo();
            FeedInfoMapper.Apply(feed.FeedInfo, entity);
            context.FeedInfo.Add(entity);
        }

        if (feed.Season is { } season)
        {
            context.TimetableSeasons.Add(new TimetableSeason
            {
                Id = season.Id,
                Name = season.Name,
                StartDate = season.StartDate,
                EndDate = season.EndDate,
                HolidayCountry = season.HolidayCountry,
                HolidayMode = (short)season.HolidayMode
            });
        }
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
            case int v: writer.Write(v, type); break;
            case short v: writer.Write(v, type); break;
            case double v: writer.Write(v, type); break;
            case bool v: writer.Write(v, type); break;
            case DateOnly v: writer.Write(v, type); break;
            default: throw new NotSupportedException($"COPY ne podržava vrijednost tipa {value.GetType()}.");
        }
    }
}
