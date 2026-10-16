using Terminus.DAL.Entities;
using Terminus.Model;
using Terminus.Model.Common;

namespace Terminus.Repository.Mapping;

internal static class AgencyMapper
{
    public static IAgencyModel ToModel(Agency entity) => new AgencyModel
    {
        Id = entity.Id,
        Code = entity.Code,
        Name = entity.Name,
        Url = entity.Url,
        Timezone = entity.Timezone,
        Lang = entity.Lang,
        Phone = entity.Phone,
        FareUrl = entity.FareUrl,
        Email = entity.Email,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };

    /// <summary>Prepisuje polja koja korisnik smije mijenjati (bez Id-a i vremenskih oznaka).</summary>
    public static void Apply(IAgencyModel model, Agency entity)
    {
        entity.Code = model.Code;
        entity.Name = model.Name;
        entity.Url = model.Url;
        entity.Timezone = model.Timezone;
        entity.Lang = model.Lang;
        entity.Phone = model.Phone;
        entity.FareUrl = model.FareUrl;
        entity.Email = model.Email;
    }
}
