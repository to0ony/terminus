using System.ComponentModel.DataAnnotations;

namespace Terminus.WebApi.Contracts.Trips;

/// <summary>Cijeli popis stanica vožnje, redom kojim ih vožnja obilazi.</summary>
public class ReplaceStopTimesRequest
{
    [Required]
    public List<StopTimeRequest>? StopTimes { get; set; }
}
