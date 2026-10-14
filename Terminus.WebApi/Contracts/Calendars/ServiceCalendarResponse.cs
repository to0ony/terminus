namespace Terminus.WebApi.Contracts.Calendars;

public record ServiceCalendarResponse(
    Guid Id,
    Guid AgencyId,
    string Code,
    string Name,
    bool Monday,
    bool Tuesday,
    bool Wednesday,
    bool Thursday,
    bool Friday,
    bool Saturday,
    bool Sunday,
    DateOnly StartDate,
    DateOnly EndDate,
    DateTime CreatedAt,
    DateTime UpdatedAt);
