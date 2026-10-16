using Terminus.Model.Common;

namespace Terminus.Model;

public class AgencyModel : IAgencyModel
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Timezone { get; set; } = string.Empty;
    public string Lang { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? FareUrl { get; set; }
    public string? Email { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
