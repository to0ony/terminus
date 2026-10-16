using Terminus.Model.Common;

namespace Terminus.Model;

public class FeedInfoModel : IFeedInfoModel
{
    public string PublisherName { get; set; } = string.Empty;
    public string PublisherUrl { get; set; } = string.Empty;
    public string Lang { get; set; } = string.Empty;
    public string? DefaultLang { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
