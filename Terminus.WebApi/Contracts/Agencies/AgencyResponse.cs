namespace Terminus.WebApi.Contracts.Agencies;

public record AgencyResponse(
    Guid Id,
    string Name,
    string Url,
    string Timezone,
    string Lang,
    string? Phone,
    string? Email,
    DateTime CreatedAt,
    DateTime UpdatedAt);
