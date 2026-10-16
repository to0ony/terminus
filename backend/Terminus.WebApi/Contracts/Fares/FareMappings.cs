using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Model.Common.Filters;

namespace Terminus.WebApi.Contracts.Fares;

public static class FareMappings
{
    // Code, Price i CurrencyType su [Required], pa ih je model binding već provjerio.
    public static IFareModel ToModel(this FareRequest request) => new FareModel
    {
        Code = request.Code!,
        Price = request.Price!.Value,
        CurrencyType = request.CurrencyType!,
        PaymentMethod = request.PaymentMethod,
        Transfers = request.Transfers,
        AgencyId = request.AgencyId,
        TransferDuration = request.TransferDuration,
        Rules = request.Rules
            .Select(r => (IFareRuleModel)new FareRuleModel
            {
                RouteId = r.RouteId,
                OriginId = r.OriginId,
                DestinationId = r.DestinationId,
                ContainsId = r.ContainsId
            })
            .ToList()
    };

    public static FareResponse ToResponse(this IFareModel model) => new(
        model.Id,
        model.Code,
        model.Price,
        model.CurrencyType,
        model.PaymentMethod,
        model.Transfers,
        model.AgencyId,
        model.TransferDuration,
        model.Rules.Select(r => new FareRuleResponse(r.RouteId, r.OriginId, r.DestinationId, r.ContainsId)).ToList(),
        model.CreatedAt,
        model.UpdatedAt);

    public static FareFilter ToFilter(this FareListQuery query) => new()
    {
        Search = query.Search,
        AgencyId = query.AgencyId,
        Page = query.Page,
        PageSize = query.PageSize
    };
}
