using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Terminus.DAL.Entities;

namespace Terminus.DAL.Configurations;

public class FeedInfoConfiguration : IEntityTypeConfiguration<FeedInfo>
{
    public void Configure(EntityTypeBuilder<FeedInfo> builder)
    {
        builder.ToTable("feed_info", t =>
        {
            t.HasCheckConstraint("ck_feed_info_single_row", "id = 1");
        });

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.PublisherName).IsRequired();
        builder.Property(e => e.PublisherUrl).IsRequired();
        builder.Property(e => e.Lang).IsRequired();

        builder.ConfigureAuditColumns();
    }
}
