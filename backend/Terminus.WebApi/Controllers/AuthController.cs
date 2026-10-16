using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Terminus.Common.Exceptions;
using Terminus.DAL.Entities;
using Terminus.WebApi.Contracts.Auth;

namespace Terminus.WebApi.Controllers;

/// <summary>
/// Prijava administratora kolačićem (HttpOnly, SameSite=Strict). Registracije nema:
/// prvi administrator dolazi iz konfiguracije (Admin:Email, Admin:Password).
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(
    SignInManager<AdminUser> signInManager,
    UserManager<AdminUser> userManager,
    TimeProvider timeProvider) : ControllerBase
{
    /// <summary>
    /// Prijava e-mailom i lozinkom; postavlja kolačić. Nakon 5 neuspjelih pokušaja
    /// račun je zaključan 15 minuta (429).
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<CurrentUserResponse>> Login(LoginRequest request)
    {
        // Ista poruka za nepostojeći e-mail i krivu lozinku, da se ne otkrije koji računi postoje.
        var user = await userManager.FindByEmailAsync(request.Email!.Trim());
        if (user is null)
        {
            return InvalidCredentials();
        }

        var result = await signInManager.PasswordSignInAsync(user, request.Password!, request.RememberMe, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            return Problem(
                title: "Previše neuspjelih prijava.",
                detail: "Račun je privremeno zaključan. Pokušaj ponovno za 15 minuta.",
                statusCode: StatusCodes.Status429TooManyRequests);
        }

        if (!result.Succeeded)
        {
            return InvalidCredentials();
        }

        user.LastLoginAt = timeProvider.GetUtcNow().UtcDateTime;
        await userManager.UpdateAsync(user);

        return Ok(user.ToResponse());
    }

    /// <summary>Odjava: briše kolačić.</summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return NoContent();
    }

    /// <summary>Prijavljeni administrator; 401 ako prijave nema.</summary>
    [HttpGet("me")]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserResponse>> Me()
    {
        var user = await userManager.GetUserAsync(User);
        return user is null ? Unauthorized() : Ok(user.ToResponse());
    }

    /// <summary>
    /// Promjena vlastite lozinke. Ostale prijave tog administratora (drugi
    /// preglednici) prestaju vrijediti, a ova ostaje.
    /// </summary>
    [HttpPut("password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword!, request.NewPassword!);
        if (!result.Succeeded)
        {
            throw new ValidationException(result.Errors
                .GroupBy(e => e.Code == nameof(IdentityErrorDescriber.PasswordMismatch)
                    ? nameof(request.CurrentPassword)
                    : nameof(request.NewPassword))
                .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray()));
        }

        // Nova sigurnosna oznaka poništava ostale kolačiće; ovaj se izdaje ponovno.
        await signInManager.RefreshSignInAsync(user);
        return NoContent();
    }

    private ObjectResult InvalidCredentials() => Problem(
        title: "Prijava nije uspjela.",
        detail: "Neispravan e-mail ili lozinka.",
        statusCode: StatusCodes.Status401Unauthorized);
}
