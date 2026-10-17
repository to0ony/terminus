using Terminus.DAL.Entities;
using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;

namespace Terminus.Repository.Mapping;

internal static class RouteVariantMapper
{
    public static IRouteVariantModel ToModel(RouteVariant entity) => new RouteVariantModel
    {
        Id = entity.Id,
        RouteId = entity.RouteId,
        Name = entity.Name,
        DirectionId = (TripDirection?)entity.DirectionId,
        Headsign = entity.Headsign,
        ShapeId = entity.ShapeId,
        SortOrder = entity.SortOrder,
        Source = (VariantSource)entity.Source,
        Stops = entity.Stops
            .OrderBy(s => s.Sequence)
            .Select(s => new VariantStop(s.StopId, s.TravelTime, s.DwellTime))
            .ToList(),
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };

    /// <summary>Prepisuje polja i stanice (bez Id-a i vremenskih oznaka).</summary>
    public static void Apply(IRouteVariantModel model, RouteVariant entity)
    {
        entity.RouteId = model.RouteId;
        entity.Name = model.Name;
        entity.DirectionId = (short?)model.DirectionId;
        entity.Headsign = model.Headsign;
        entity.ShapeId = model.ShapeId;
        entity.SortOrder = model.SortOrder;
        entity.Source = (short)model.Source;
        entity.Stops = model.Stops
            .Select((s, i) => new RouteVariantStop
            {
                VariantId = entity.Id,
                Sequence = i + 1,
                StopId = s.StopId,
                TravelTime = s.TravelTime,
                DwellTime = s.DwellTime
            })
            .ToList();
    }
}
