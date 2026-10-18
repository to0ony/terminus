using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Model.Common.Filters;

namespace Terminus.WebApi.Contracts.Agencies;

public static class AgencyMappings
{
    // Name i Url su [Required]; prazan Code servis kod stvaranja izvodi iz naziva,
    // a prazne Timezone i Lang zamjenjuje zadanim vrijednostima.
    public static IAgencyModel ToModel(this AgencyRequest request) => new AgencyModel
    {
        Code = request.Code ?? string.Empty,
        Name = request.Name!,
        Url = request.Url!,
        Timezone = request.Timezone ?? string.Empty,
        Lang = request.Lang ?? string.Empty,
        Phone = request.Phone,
        FareUrl = request.FareUrl,
        Email = request.Email
    };

    public static AgencyResponse ToResponse(this IAgencyModel model) => new(
        model.Id,
        model.Code,
        model.Name,
        model.Url,
        model.Timezone,
        model.Lang,
        model.Phone,
        model.FareUrl,
        model.Email,
        model.CreatedAt,
        model.UpdatedAt);

    public static AgencyFilter ToFilter(this AgencyListQuery query) => new()
    {
        Search = query.Search,
        Page = query.Page,
        PageSize = query.PageSize
    };
}
