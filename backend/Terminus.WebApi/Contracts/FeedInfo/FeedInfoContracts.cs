using System.ComponentModel.DataAnnotations;
using Terminus.Model;
using Terminus.Model.Common;

namespace Terminus.WebApi.Contracts.FeedInfo;

/// <summary>Podaci o izdavaču feeda (GTFS feed_info).</summary>
public class FeedInfoRequest
{
    [Required]
    public string? PublisherName { get; set; }

    [Required]
    public string? PublisherUrl { get; set; }

    /// <summary>Jezik feeda, npr. "hr" (ili "mul" za više jezika).</summary>
    [Required]
    public string? Lang { get; set; }

    public string? DefaultLang { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactUrl { get; set; }
}

public record FeedInfoResponse(
    string PublisherName,
    string PublisherUrl,
    string Lang,
    string? DefaultLang,
    string? ContactEmail,
    string? ContactUrl,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public static class FeedInfoMappings
{
    public static IFeedInfoModel ToModel(this FeedInfoRequest request) => new FeedInfoModel
    {
        PublisherName = request.PublisherName!,
        PublisherUrl = request.PublisherUrl!,
        Lang = request.Lang!,
        DefaultLang = request.DefaultLang,
        ContactEmail = request.ContactEmail,
        ContactUrl = request.ContactUrl
    };

    public static FeedInfoResponse ToResponse(this IFeedInfoModel model) => new(
        model.PublisherName,
        model.PublisherUrl,
        model.Lang,
        model.DefaultLang,
        model.ContactEmail,
        model.ContactUrl,
        model.CreatedAt,
        model.UpdatedAt);
}
