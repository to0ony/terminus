using Microsoft.AspNetCore.Identity;

namespace Terminus.DAL.Entities;

/// <summary>
/// Administrator instance (ASP.NET Core Identity). Korisničko ime je e-mail adresa.
/// Uloga zasad nema: svi administratori imaju iste ovlasti.
/// </summary>
public class AdminUser : IdentityUser<Guid>
{
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
