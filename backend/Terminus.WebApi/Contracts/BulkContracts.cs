using System.ComponentModel.DataAnnotations;

namespace Terminus.WebApi.Contracts;

/// <summary>Aktivacija ili deaktivacija više stanica ili linija odjednom.</summary>
public class BulkActiveRequest
{
    [Required]
    public List<Guid>? Ids { get; set; }

    public bool IsActive { get; set; }
}

/// <summary>Premještanje više linija drugom prijevozniku.</summary>
public class BulkAgencyRequest
{
    [Required]
    public List<Guid>? Ids { get; set; }

    public Guid AgencyId { get; set; }
}

/// <param name="Changed">Koliko se zapisa promijenilo (već takvi se ne broje).</param>
public record BulkResultResponse(int Changed);
