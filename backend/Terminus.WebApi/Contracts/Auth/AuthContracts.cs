using System.ComponentModel.DataAnnotations;
using Terminus.DAL.Entities;

namespace Terminus.WebApi.Contracts.Auth;

public class LoginRequest
{
    [Required]
    public string? Email { get; set; }

    [Required]
    public string? Password { get; set; }

    /// <summary>
    /// true: prijava ostaje i nakon zatvaranja preglednika (dok ne istekne);
    /// false: odjava zatvaranjem preglednika.
    /// </summary>
    public bool RememberMe { get; set; }
}

public class ChangePasswordRequest
{
    [Required]
    public string? CurrentPassword { get; set; }

    [Required]
    public string? NewPassword { get; set; }
}

/// <summary>Prijavljeni administrator.</summary>
/// <param name="LastLoginAt">Zadnja uspješna prijava (UTC).</param>
public record CurrentUserResponse(Guid Id, string Email, DateTime CreatedAt, DateTime? LastLoginAt);

public static class AuthMappings
{
    public static CurrentUserResponse ToResponse(this AdminUser user) =>
        new(user.Id, user.Email!, user.CreatedAt, user.LastLoginAt);
}
