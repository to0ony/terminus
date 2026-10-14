using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Terminus.DAL.Entities;

namespace Terminus.DAL.Configurations;

internal static class AuditableConfigurationExtensions
{
    public static void ConfigureAuditColumns<T>(this EntityTypeBuilder<T> builder)
        where T : class, IAuditable
    {
        // CreatedAt postavlja baza pri INSERT-u i EF ga nikad ne šalje u UPDATE.
        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()")
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        // Pri INSERT-u ga postavlja baza, pri UPDATE-u AuditableInterceptor.
        builder.Property(e => e.UpdatedAt)
            .HasDefaultValueSql("now()");
    }
}
