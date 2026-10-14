using System.ComponentModel.DataAnnotations;
using Terminus.Model.Common.Enums;

namespace Terminus.WebApi.Contracts.Calendars;

/// <summary>Tijelo zahtjeva za postavljanje iznimke; datum je u ruti.</summary>
public class CalendarDateRequest
{
    /// <summary>1 = vozi taj dan, 2 = ne vozi taj dan.</summary>
    [Required]
    public CalendarExceptionType? ExceptionType { get; set; }
}
