using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using Terminus.DAL;
using Terminus.DAL.Entities;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;
using Terminus.Repository.Common;
using Terminus.Repository.Mapping;

namespace Terminus.Repository;

/// <summary>
/// Upis osnove iz GTFS-a kao dopuna: zapisi se prepoznaju po GTFS ID-u (code).
/// Točke trasa idu binarnim COPY-jem, jer ih može biti stotine tisuća.
/// </summary>
public class GtfsImportRepository(TerminusDbContext context) : IGtfsImportRepository
{
    private static readonly TimeSpan CopyTimeout = TimeSpan.FromMinutes(10);

    public async Task<GtfsExistingCodes> GetExistingCodesAsync(CancellationToken cancellationToken = default) => new(
        (await context.Agencies.Select(a => a.Code).ToListAsync(cancellationToken)).ToHashSet(StringComparer.Ordinal),
        (await context.Stops.Select(s => s.Code).ToListAsync(cancellationToken)).ToHashSet(StringComparer.Ordinal),
        (await context.Routes.Select(r => r.Code).ToListAsync(cancellationToken)).ToHashSet(StringComparer.Ordinal),
        (await context.Shapes.Select(s => s.Code).ToListAsync(cancellationToken)).ToHashSet(StringComparer.Ordinal));

    public async Task MergeAsync(GtfsFeedData feed, IReadOnlyList<IRouteVariantModel> variants, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        // Id iz feeda → Id u bazi (postojeći zapis s istim kodom ili novi).
        var agencyIds = await UpsertAsync(
            context.Agencies, feed.Agencies, a => a.Code, e => e.Code, id => new Agency { Id = id },
            (model, entity, _) => AgencyMapper.Apply(model, entity), cancellationToken);

        var stopIds = await IdsByCodeAsync(context.Stops, feed.Stops, s => s.Code, e => e.Code, cancellationToken);
        await UpsertAsync(
            context.Stops, feed.Stops, s => s.Code, e => e.Code, id => new Stop { Id = id },
            (model, entity, _) =>
            {
                StopMapper.Apply(model, entity);
                entity.ParentStationId = model.ParentStationId is { } parent ? stopIds.GetValueOrDefault(parent, parent) : null;
            },
            cancellationToken);

        var routeIds = await UpsertAsync(
            context.Routes, feed.Routes, r => r.Code, e => e.Code, id => new Route { Id = id },
            (model, entity, _) =>
            {
                RouteMapper.Apply(model, entity);
                entity.AgencyId = agencyIds.GetValueOrDefault(model.AgencyId, model.AgencyId);
            },
            cancellationToken);

        var shapeIds = await UpsertAsync(
            context.Shapes, feed.Shapes, s => s.Code, e => e.Code, id => new Shape { Id = id },
            (model, entity, _) => entity.Code = model.Code, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        // Točke trasa se pišu ispočetka.
        var shapeTargets = feed.Shapes.Select(s => shapeIds[s.Id]).ToList();
        await context.ShapePoints.Where(p => shapeTargets.Contains(p.ShapeId)).ExecuteDeleteAsync(cancellationToken);
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        await CopyAsync(connection, "shape_points",
            feed.Shapes.SelectMany(s => s.Points.Select(p => (ShapeId: shapeIds[s.Id], Point: p))), cancellationToken,
            ("shape_id", NpgsqlDbType.Uuid, x => x.ShapeId),
            ("sequence", NpgsqlDbType.Integer, x => x.Point.Sequence),
            ("latitude", NpgsqlDbType.Double, x => x.Point.Latitude),
            ("longitude", NpgsqlDbType.Double, x => x.Point.Longitude),
            ("dist_traveled", NpgsqlDbType.Double, x => x.Point.DistTraveled));

        await MergeVariantsAsync(variants, routeIds, stopIds, shapeIds, cancellationToken);
        await ReplaceFaresAsync(feed, agencyIds, routeIds, cancellationToken);
        await ReplaceFeedInfoAsync(feed.FeedInfo, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    /// <summary>
    /// Varijante iz uvoza bez vožnji se zamjenjuju novima; ručne i one s
    /// vožnjama ostaju, a nova varijanta istog smjera i stanica kao postojeća se
    /// preskače.
    /// </summary>
    private async Task MergeVariantsAsync(
        IReadOnlyList<IRouteVariantModel> variants,
        IReadOnlyDictionary<Guid, Guid> routeIds,
        IReadOnlyDictionary<Guid, Guid> stopIds,
        IReadOnlyDictionary<Guid, Guid> shapeIds,
        CancellationToken cancellationToken)
    {
        var targetRoutes = variants.Select(v => routeIds[v.RouteId]).Distinct().ToList();

        await context.RouteVariants
            .Where(v => targetRoutes.Contains(v.RouteId) && v.Source == (short)VariantSource.Gtfs && !v.Trips.Any())
            .ExecuteDeleteAsync(cancellationToken);

        var kept = await context.RouteVariants
            .AsNoTracking()
            .Where(v => targetRoutes.Contains(v.RouteId))
            .Select(v => new { v.RouteId, v.DirectionId, Stops = v.Stops.OrderBy(s => s.Sequence).Select(s => s.StopId).ToList() })
            .ToListAsync(cancellationToken);
        var existing = kept.Select(v => Key(v.RouteId, v.DirectionId, v.Stops)).ToHashSet(StringComparer.Ordinal);
        var sortOrders = kept.GroupBy(v => (v.RouteId, v.DirectionId)).ToDictionary(g => g.Key, g => g.Count());

        foreach (var variant in variants)
        {
            var routeId = routeIds[variant.RouteId];
            var stops = variant.Stops.Select(s => stopIds.GetValueOrDefault(s.StopId, s.StopId)).ToList();
            var direction = (short?)variant.DirectionId;
            if (!existing.Add(Key(routeId, direction, stops)))
            {
                continue;
            }

            var order = sortOrders.GetValueOrDefault((routeId, direction));
            sortOrders[(routeId, direction)] = order + 1;

            var entity = new RouteVariant { Id = variant.Id };
            RouteVariantMapper.Apply(variant, entity);
            entity.RouteId = routeId;
            entity.ShapeId = variant.ShapeId is { } shape ? shapeIds.GetValueOrDefault(shape, shape) : null;
            entity.SortOrder = order;
            foreach (var (stop, i) in entity.Stops.Select((s, i) => (s, i)))
            {
                stop.StopId = stops[i];
            }
            context.RouteVariants.Add(entity);
        }
    }

    private static string Key(Guid routeId, short? direction, IEnumerable<Guid> stops) =>
        $"{routeId}|{direction}|{string.Join(',', stops)}";

    /// <summary>Cijene ne uređuje nitko osim uvoza, pa se zamjenjuju cijele.</summary>
    private async Task ReplaceFaresAsync(
        GtfsFeedData feed,
        IReadOnlyDictionary<Guid, Guid> agencyIds,
        IReadOnlyDictionary<Guid, Guid> routeIds,
        CancellationToken cancellationToken)
    {
        await context.Fares.ExecuteDeleteAsync(cancellationToken);
        foreach (var fare in feed.Fares)
        {
            var entity = new Fare { Id = fare.Id };
            FareMapper.Apply(fare, entity);
            entity.AgencyId = fare.AgencyId is { } agency ? agencyIds.GetValueOrDefault(agency, agency) : null;
            entity.Rules = fare.Rules
                .Select((rule, i) =>
                {
                    var row = FareMapper.ToEntity(fare.Id, i + 1, rule);
                    row.RouteId = rule.RouteId is { } route ? routeIds.GetValueOrDefault(route, route) : null;
                    return row;
                })
                .ToList();
            context.Fares.Add(entity);
        }
    }

    private async Task ReplaceFeedInfoAsync(IFeedInfoModel? info, CancellationToken cancellationToken)
    {
        if (info is null)
        {
            return;
        }
        var entity = await context.FeedInfo.FirstOrDefaultAsync(cancellationToken);
        if (entity is null)
        {
            entity = new FeedInfo();
            context.FeedInfo.Add(entity);
        }
        FeedInfoMapper.Apply(info, entity);
    }

    /// <summary>Id iz feeda → Id postojećeg zapisa s istim kodom, ili isti Id za novi.</summary>
    private static async Task<Dictionary<Guid, Guid>> IdsByCodeAsync<TModel, TEntity>(
        DbSet<TEntity> set,
        IEnumerable<TModel> models,
        Func<TModel, string> modelCode,
        Func<TEntity, string> entityCode,
        CancellationToken cancellationToken)
        where TModel : class
        where TEntity : class
    {
        var existing = (await set.AsNoTracking().ToListAsync(cancellationToken))
            .ToDictionary(entityCode, EntityId, StringComparer.Ordinal);
        return models.ToDictionary(
            m => ModelId(m),
            m => existing.TryGetValue(modelCode(m), out var id) ? id : ModelId(m));
    }

    /// <summary>Ažurira zapise s istim kodom, a za ostale dodaje nove s Id-em iz feeda.</summary>
    private static async Task<Dictionary<Guid, Guid>> UpsertAsync<TModel, TEntity>(
        DbSet<TEntity> set,
        IEnumerable<TModel> models,
        Func<TModel, string> modelCode,
        Func<TEntity, string> entityCode,
        Func<Guid, TEntity> create,
        Action<TModel, TEntity, bool> apply,
        CancellationToken cancellationToken)
        where TModel : class
        where TEntity : class
    {
        var existing = (await set.ToListAsync(cancellationToken)).ToDictionary(entityCode, StringComparer.Ordinal);
        var ids = new Dictionary<Guid, Guid>();

        foreach (var model in models)
        {
            var isNew = !existing.TryGetValue(modelCode(model), out var entity);
            if (isNew)
            {
                entity = create(ModelId(model));
                set.Add(entity);
            }
            apply(model, entity!, isNew);
            ids[ModelId(model)] = EntityId(entity!);
        }

        return ids;
    }

    private static Guid ModelId(object model) => model switch
    {
        IAgencyModel a => a.Id,
        IStopModel s => s.Id,
        IRouteModel r => r.Id,
        IShapeModel s => s.Id,
        _ => throw new NotSupportedException(model.GetType().Name)
    };

    private static Guid EntityId(object entity) => entity switch
    {
        Agency a => a.Id,
        Stop s => s.Id,
        Route r => r.Id,
        Shape s => s.Id,
        _ => throw new NotSupportedException(entity.GetType().Name)
    };

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
            case int v: writer.Write(v, type); break;
            case double v: writer.Write(v, type); break;
            default: throw new NotSupportedException($"COPY ne podržava vrijednost tipa {value.GetType()}.");
        }
    }
}
