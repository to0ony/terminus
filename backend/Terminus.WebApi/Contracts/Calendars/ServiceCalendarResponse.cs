namespace Terminus.WebApi.Contracts.Calendars;

/// <param name="SeasonId">Sezona kojoj kalendar pripada kao tip dana; takav se uređuje u sezoni.</param>
public record ServiceCalendarResponse(
    Guid Id,
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
    Guid? SeasonId,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <param name="ActiveCalendars">Kalendari koji voze na <paramref name="Date"/>.</param>
/// <param name="LastServiceDate">Zadnji dan s voznim redom; null ako od tog dana ništa ne vozi.</param>
public record ServiceOverviewResponse(
    DateOnly Date,
    IReadOnlyList<ServiceCalendarResponse> ActiveCalendars,
    DateOnly? LastServiceDate);

/// <summary>Query parametri za pregled voznog reda.</summary>
public class ServiceOverviewQuery
{
    /// <summary>Dan (YYYY-MM-DD); bez njega današnji u zoni prijevoznika.</summary>
    public DateOnly? Date { get; set; }
}
