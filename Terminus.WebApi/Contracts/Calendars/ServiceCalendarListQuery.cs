namespace Terminus.WebApi.Contracts.Calendars;

/// <summary>Query parametri za popis kalendara.</summary>
public class ServiceCalendarListQuery
{
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
