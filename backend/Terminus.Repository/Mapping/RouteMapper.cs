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
        Url = entity.Url,
        Color = entity.Color,
        TextColor = entity.TextColor,
        SortOrder = entity.SortOrder,
        ContinuousPickup = (ContinuousPickupDropOff?)entity.ContinuousPickup,
        ContinuousDropOff = (ContinuousPickupDropOff?)entity.ContinuousDropOff,
        NetworkId = entity.NetworkId,
        TimeMode = (TimeMode)entity.TimeMode,
        IsActive = entity.IsActive,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };

    /// <summary>Prepisuje polja koja korisnik smije mijenjati (bez Id-a i vremenskih oznaka).</summary>
    public static void Apply(IRouteModel model, Route entity)
    {
        entity.AgencyId = model.AgencyId;
        entity.Code = model.Code;
        entity.ShortName = model.ShortName;
        entity.LongName = model.LongName;
        entity.Description = model.Description;
        entity.RouteType = (short)model.RouteType;
        entity.Url = model.Url;
        entity.Color = model.Color;
        entity.TextColor = model.TextColor;
        entity.SortOrder = model.SortOrder;
        entity.ContinuousPickup = (short?)model.ContinuousPickup;
        entity.ContinuousDropOff = (short?)model.ContinuousDropOff;
        entity.NetworkId = model.NetworkId;
        entity.TimeMode = (short)model.TimeMode;
        entity.IsActive = model.IsActive;
    }
}
