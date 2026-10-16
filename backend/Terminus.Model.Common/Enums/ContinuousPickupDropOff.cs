namespace Terminus.Model.Common.Enums;

/// <summary>
/// GTFS continuous_pickup / continuous_drop_off: smije li se ulaziti ili izlaziti
/// bilo gdje na trasi između stanica. Prazno polje u GTFS-u znači <see cref="None"/>.
/// </summary>
public enum ContinuousPickupDropOff : short
{
    /// <summary>Bilo gdje na trasi.</summary>
    Continuous = 0,

    /// <summary>Samo na stanicama.</summary>
    None = 1,

    /// <summary>Potrebno je nazvati prijevoznika.</summary>
    PhoneAgency = 2,

    /// <summary>Potrebno je dogovoriti s vozačem.</summary>
    CoordinateWithDriver = 3
}
