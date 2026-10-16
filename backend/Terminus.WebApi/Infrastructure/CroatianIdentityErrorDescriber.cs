using Microsoft.AspNetCore.Identity;

namespace Terminus.WebApi.Infrastructure;

/// <summary>Poruke Identity provjera na hrvatskom (lozinka, e-mail).</summary>
public sealed class CroatianIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError() =>
        Error(nameof(DefaultError), "Došlo je do greške.");

    public override IdentityError ConcurrencyFailure() =>
        Error(nameof(ConcurrencyFailure), "Podaci su u međuvremenu promijenjeni. Pokušaj ponovno.");

    public override IdentityError PasswordMismatch() =>
        Error(nameof(PasswordMismatch), "Lozinka nije ispravna.");

    public override IdentityError InvalidUserName(string? userName) =>
        Error(nameof(InvalidUserName), $"Korisničko ime '{userName}' nije ispravno.");

    public override IdentityError InvalidEmail(string? email) =>
        Error(nameof(InvalidEmail), $"E-mail adresa '{email}' nije ispravna.");

    public override IdentityError DuplicateUserName(string userName) =>
        Error(nameof(DuplicateUserName), $"Administrator '{userName}' već postoji.");

    public override IdentityError DuplicateEmail(string email) =>
        Error(nameof(DuplicateEmail), $"Administrator s e-mailom '{email}' već postoji.");

    public override IdentityError PasswordTooShort(int length) =>
        Error(nameof(PasswordTooShort), $"Lozinka mora imati barem {length} znakova.");

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
        Error(nameof(PasswordRequiresUniqueChars), $"Lozinka mora imati barem {uniqueChars} različitih znakova.");

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        Error(nameof(PasswordRequiresNonAlphanumeric), "Lozinka mora imati barem jedan znak koji nije slovo ni broj.");

    public override IdentityError PasswordRequiresDigit() =>
        Error(nameof(PasswordRequiresDigit), "Lozinka mora imati barem jednu znamenku.");

    public override IdentityError PasswordRequiresLower() =>
        Error(nameof(PasswordRequiresLower), "Lozinka mora imati barem jedno malo slovo.");

    public override IdentityError PasswordRequiresUpper() =>
        Error(nameof(PasswordRequiresUpper), "Lozinka mora imati barem jedno veliko slovo.");

    private static IdentityError Error(string code, string description) => new() { Code = code, Description = description };
}
