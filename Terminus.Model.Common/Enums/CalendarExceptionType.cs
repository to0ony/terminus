namespace Terminus.Model.Common.Enums;

/// <summary>GTFS exception_type u calendar_dates.</summary>
public enum CalendarExceptionType : short
{
    /// <summary>Vozi taj dan (iako ga kalendar inače ne uključuje).</summary>
    Added = 1,

    /// <summary>Ne vozi taj dan (npr. praznik).</summary>
    Removed = 2
}
