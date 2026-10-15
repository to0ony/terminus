namespace Terminus.Model.Common.Enums;

/// <summary>GTFS pickup_type / drop_off_type.</summary>
public enum PickupDropOffType : short
{
    /// <summary>Redovno ukrcavanje/iskrcavanje.</summary>
    Regular = 0,

    /// <summary>Nema ukrcavanja/iskrcavanja.</summary>
    None = 1,

    /// <summary>Potrebno je nazvati prijevoznika.</summary>
    PhoneAgency = 2,

    /// <summary>Potrebno je dogovoriti s vozačem.</summary>
    CoordinateWithDriver = 3
}
