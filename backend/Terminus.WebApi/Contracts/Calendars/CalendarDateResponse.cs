using Terminus.Model.Common.Enums;

namespace Terminus.WebApi.Contracts.Calendars;

public record CalendarDateResponse(DateOnly Date, CalendarExceptionType ExceptionType);
