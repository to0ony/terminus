namespace Terminus.DAL.Entities;

/// <summary>GTFS feed_info. Tablica ima najviše jedan redak (Id = 1).</summary>
public class FeedInfo : IAuditable
{
    public const short SingletonId = 1;

    public short Id { get; set; } = SingletonId;
    public string PublisherName { get; set; } = null!;
    public string PublisherUrl { get; set; } = null!;
    public string Lang { get; set; } = null!;
    public string? DefaultLang { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
