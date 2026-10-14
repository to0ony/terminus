using System.ComponentModel.DataAnnotations;

namespace Terminus.WebApi.Contracts.Calendars;

/// <summary>Tijelo zahtjeva za stvaranje (POST) i potpunu izmjenu (PUT) kalendara.</summary>
public class ServiceCalendarRequest
{
    [Required]
    public string? Code { get; set; }

    [Required]
    public string? Name { get; set; }

    public bool Monday { get; set; }
    public bool Tuesday { get; set; }
    public bool Wednesday { get; set; }
    public bool Thursday { get; set; }
    public bool Friday { get; set; }
    public bool Saturday { get; set; }
    public bool Sunday { get; set; }

    /// <summary>Format "yyyy-MM-dd".</summary>
    [Required]
    public DateOnly? StartDate { get; set; }

    /// <summary>Format "yyyy-MM-dd".</summary>
    [Required]
    public DateOnly? EndDate { get; set; }
}
