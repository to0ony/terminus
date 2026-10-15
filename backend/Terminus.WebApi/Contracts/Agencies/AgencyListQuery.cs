namespace Terminus.WebApi.Contracts.Agencies;

/// <summary>Query parametri za popis agencija.</summary>
public class AgencyListQuery
{
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
