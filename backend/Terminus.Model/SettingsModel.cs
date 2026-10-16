using Terminus.Model.Common;

namespace Terminus.Model;

public class SettingsModel : ISettingsModel
{
    public string? DisplayName { get; set; }
    public string? PrimaryColor { get; set; }
    public string? PrimaryTextColor { get; set; }
    public string? PublicUrl { get; set; }
    public bool PublicGtfsEnabled { get; set; }
    public DateTime? LogoUpdatedAt { get; set; }
    public DateTime? SetupCompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
