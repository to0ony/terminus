using Terminus.DAL.Entities;
using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;

namespace Terminus.Repository.Mapping;

internal static class StopMapper
{
    public static IStopModel ToModel(Stop entity) => new StopModel
    {
        Id = entity.Id,
        Code = entity.Code,
        Name = entity.Name,
        Description = entity.Description,
        Latitude = entity.Latitude,
        Longitude = entity.Longitude,
        LocationType = (LocationType)entity.LocationType,
        ParentStationId = entity.ParentStationId,
        PlatformCode = entity.PlatformCode,
        WheelchairBoarding = (WheelchairBoarding)entity.WheelchairBoarding,
        IsActive = entity.IsActive,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };

    /// <summary>
    /// Prepisuje polja koja korisnik smije mijenjati. Id i vremenske
    /// oznake ne dira (njih postavljaju baza, interceptor ili repozitorij).
    /// </summary>
    public static void Apply(IStopModel model, Stop entity)
    {
        entity.Code = model.Code;
        entity.Name = model.Name;
        entity.Description = model.Description;
        entity.Latitude = model.Latitude;
        entity.Longitude = model.Longitude;
        entity.LocationType = (short)model.LocationType;
        entity.ParentStationId = model.ParentStationId;
        entity.PlatformCode = model.PlatformCode;
        entity.WheelchairBoarding = (short)model.WheelchairBoarding;
        entity.IsActive = model.IsActive;
    }
}
