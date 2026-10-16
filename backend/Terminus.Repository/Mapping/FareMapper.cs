using Terminus.DAL.Entities;
using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;

namespace Terminus.Repository.Mapping;

internal static class FareMapper
{
    /// <param name="rules">Pravila cijene; redoslijed se zadržava.</param>
    public static IFareModel ToModel(Fare entity, IEnumerable<FareRule> rules) => new FareModel
    {
        Id = entity.Id,
        Code = entity.Code,
        Price = entity.Price,
        CurrencyType = entity.CurrencyType,
        PaymentMethod = (FarePaymentMethod)entity.PaymentMethod,
        Transfers = (FareTransfers?)entity.Transfers,
        AgencyId = entity.AgencyId,
        TransferDuration = entity.TransferDuration,
        Rules = rules.OrderBy(r => r.Sequence).Select(ToModel).ToList(),
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };

    public static IFareRuleModel ToModel(FareRule entity) => new FareRuleModel
    {
        RouteId = entity.RouteId,
        OriginId = entity.OriginId,
        DestinationId = entity.DestinationId,
        ContainsId = entity.ContainsId
    };

    /// <summary>Prepisuje polja koja korisnik smije mijenjati (bez Id-a, pravila i vremenskih oznaka).</summary>
    public static void Apply(IFareModel model, Fare entity)
    {
        entity.Code = model.Code;
        entity.Price = model.Price;
        entity.CurrencyType = model.CurrencyType;
        entity.PaymentMethod = (short)model.PaymentMethod;
        entity.Transfers = (short?)model.Transfers;
        entity.AgencyId = model.AgencyId;
        entity.TransferDuration = model.TransferDuration;
    }

    public static FareRule ToEntity(Guid fareId, int sequence, IFareRuleModel model) => new()
    {
        FareId = fareId,
        Sequence = sequence,
        RouteId = model.RouteId,
        OriginId = model.OriginId,
        DestinationId = model.DestinationId,
        ContainsId = model.ContainsId
    };
}
