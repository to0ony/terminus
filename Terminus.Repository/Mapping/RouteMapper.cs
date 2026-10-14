using Terminus.DAL.Entities;
using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;

namespace Terminus.Repository.Mapping;

internal static class RouteMapper
{
    public static IRouteModel ToModel(Route entity) => new RouteModel
    {
        Id = entity.Id,
        AgencyId = entity.AgencyId,
        Code = entity.Code,
        ShortName = entity.ShortName,
        LongName = entity.LongName,
        Description = entity.Description,
        RouteType = (RouteType)entity.RouteType,
        Color = entity.Color,
        TextColor = entity.TextColor,
        SortOrder = entity.SortOrder,
        IsActive = entity.IsActive,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };

    /// <summary>Prepisuje polja koja korisnik smije mijenjati (bez Id-a, AgencyId-a i vremenskih oznaka).</summary>
    public static void Apply(IRouteModel model, Route entity)
    {
        entity.Code = model.Code;
        entity.ShortName = model.ShortName;
        entity.LongName = model.LongName;
        entity.Description = model.Description;
        entity.RouteType = (short)model.RouteType;
        entity.Color = model.Color;
        entity.TextColor = model.TextColor;
        entity.SortOrder = model.SortOrder;
        entity.IsActive = model.IsActive;
    }
}
