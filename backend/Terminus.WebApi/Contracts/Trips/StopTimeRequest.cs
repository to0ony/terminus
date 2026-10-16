using System.ComponentModel.DataAnnotations;
using Terminus.Model.Common.Enums;

namespace Terminus.WebApi.Contracts.Trips;

/// <summary>Jedna stanica vožnje. Redni broj određuje položaj u popisu.</summary>
public class StopTimeRequest
{
    [Required]
    public Guid? StopId { get; set; }

    /// <summary>"HH:MM:SS" (ili "HH:MM"); sati mogu biti iznad 24 za vožnje preko ponoći.</summary>
    [Required]
    public string? ArrivalTime { get; set; }

    /// <summary>Ako se izostavi, jednako je vremenu dolaska.</summary>
    public string? DepartureTime { get; set; }

    public string? StopHeadsign { get; set; }
    public PickupDropOffType PickupType { get; set; } = PickupDropOffType.Regular;
    public PickupDropOffType DropOffType { get; set; } = PickupDropOffType.Regular;

    /// <summary>Izostavljeno znači vrijednost linije.</summary>
    public ContinuousPickupDropOff? ContinuousPickup { get; set; }

    public ContinuousPickupDropOff? ContinuousDropOff { get; set; }

    /// <summary>Prijeđena udaljenost duž trase, u istim jedinicama kao trasa.</summary>
    public double? ShapeDistTraveled { get; set; }

    public bool Timepoint { get; set; } = true;
}
