namespace Terminus.Model.Common;

/// <summary>
/// Podaci o izdavaču feeda (GTFS feed_info.txt). Jedan zapis po instanci.
/// Razdoblje važenja i verzija izračunavaju se pri izvozu.
/// </summary>
public interface IFeedInfoModel
{
    string PublisherName { get; set; }
    string PublisherUrl { get; set; }

    /// <summary>Jezik feeda (BCP 47), npr. "hr".</summary>
    string Lang { get; set; }

    /// <summary>Jezik za putnike kad jezik nije poznat (GTFS default_lang).</summary>
    string? DefaultLang { get; set; }

    string? ContactEmail { get; set; }
    string? ContactUrl { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}
