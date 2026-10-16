namespace Terminus.WebApi.Contracts.Agencies;

public record AgencyResponse(
    Guid Id,
    string Code,
    string Name,
    string Url,
    string Timezone,
    string Lang,
    string? Phone,
    string? FareUrl,
    string? Email,
    DateTime CreatedAt,
    DateTime UpdatedAt);
