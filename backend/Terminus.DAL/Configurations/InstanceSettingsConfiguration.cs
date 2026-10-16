using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Terminus.DAL.Entities;

namespace Terminus.DAL.Configurations;

public class InstanceSettingsConfiguration : IEntityTypeConfiguration<InstanceSettings>
{
    public void Configure(EntityTypeBuilder<InstanceSettings> builder)
    {
        builder.ToTable("settings", t =>
        {
            t.HasCheckConstraint("ck_settings_single_row", "id = 1");
            t.HasCheckConstraint("ck_settings_primary_color", "primary_color ~ '^[0-9A-F]{6}$'");
            t.HasCheckConstraint("ck_settings_primary_text_color", "primary_text_color ~ '^[0-9A-F]{6}$'");
            t.HasCheckConstraint("ck_settings_logo",
                "(logo IS NULL) = (logo_content_type IS NULL) AND (logo IS NULL) = (logo_updated_at IS NULL)");
        });

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.PublicGtfsEnabled).HasDefaultValue(false);

        builder.ConfigureAuditColumns();
    }
}
