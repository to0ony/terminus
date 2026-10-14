using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Terminus.DAL.Entities;

namespace Terminus.DAL.Configurations;

public class AgencyConfiguration : IEntityTypeConfiguration<Agency>
{
    public void Configure(EntityTypeBuilder<Agency> builder)
    {
        builder.ToTable("agencies");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.Name).IsRequired();

        // GTFS agency.txt traži agency_url.
        builder.Property(e => e.Url).IsRequired();

        // Default postoji u bazi, ali EF uvijek šalje vrijednost iz C# inicijalizatora.
        builder.Property(e => e.Timezone).HasDefaultValue("Europe/Zagreb").ValueGeneratedNever();
        builder.Property(e => e.Lang).HasDefaultValue("hr").ValueGeneratedNever();

        builder.ConfigureAuditColumns();
    }
}
