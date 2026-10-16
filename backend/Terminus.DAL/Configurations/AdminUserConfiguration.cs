using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Terminus.DAL.Entities;

namespace Terminus.DAL.Configurations;

/// <summary>
/// Identity tablice dobivaju nazive admin_users, admin_user_claims ... umjesto
/// zadanih AspNetUsers ...; ostatak sheme postavlja IdentityUserContext.
/// </summary>
public class AdminUserConfiguration : IEntityTypeConfiguration<AdminUser>
{
    public void Configure(EntityTypeBuilder<AdminUser> builder)
    {
        builder.ToTable("admin_users");

        builder.HasIndex(u => u.NormalizedUserName)
            .HasDatabaseName("uq_admin_users_normalized_user_name")
            .IsUnique();

        builder.HasIndex(u => u.NormalizedEmail)
            .HasDatabaseName("ix_admin_users_normalized_email");

        builder.Property(u => u.CreatedAt)
            .HasDefaultValueSql("now()")
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
    }
}

public class AdminUserClaimConfiguration : IEntityTypeConfiguration<IdentityUserClaim<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserClaim<Guid>> builder) =>
        builder.ToTable("admin_user_claims");
}

public class AdminUserLoginConfiguration : IEntityTypeConfiguration<IdentityUserLogin<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserLogin<Guid>> builder) =>
        builder.ToTable("admin_user_logins");
}

public class AdminUserTokenConfiguration : IEntityTypeConfiguration<IdentityUserToken<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserToken<Guid>> builder) =>
        builder.ToTable("admin_user_tokens");
}
