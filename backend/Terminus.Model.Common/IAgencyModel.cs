namespace Terminus.Model.Common;

public interface IAgencyModel
{
    Guid Id { get; set; }
    string Name { get; set; }
    string Url { get; set; }

    /// <summary>IANA naziv vremenske zone, npr. "Europe/Zagreb".</summary>
    string Timezone { get; set; }

    /// <summary>Jezik (BCP 47), npr. "hr".</summary>
    string Lang { get; set; }

    string? Phone { get; set; }
    string? Email { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}
