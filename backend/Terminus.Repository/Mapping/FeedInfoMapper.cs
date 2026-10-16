using Terminus.DAL.Entities;
using Terminus.Model;
using Terminus.Model.Common;

namespace Terminus.Repository.Mapping;

internal static class FeedInfoMapper
{
    public static IFeedInfoModel ToModel(FeedInfo entity) => new FeedInfoModel
    {
        PublisherName = entity.PublisherName,
        PublisherUrl = entity.PublisherUrl,
        Lang = entity.Lang,
        DefaultLang = entity.DefaultLang,
        ContactEmail = entity.ContactEmail,
        ContactUrl = entity.ContactUrl,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };

    public static void Apply(IFeedInfoModel model, FeedInfo entity)
    {
        entity.PublisherName = model.PublisherName;
        entity.PublisherUrl = model.PublisherUrl;
        entity.Lang = model.Lang;
        entity.DefaultLang = model.DefaultLang;
        entity.ContactEmail = model.ContactEmail;
        entity.ContactUrl = model.ContactUrl;
    }
}
