using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Terminus.DAL.Entities;

namespace Terminus.WebApi.Infrastructure;

/// <summary>
/// Pri pokretanju stvara administratora iz konfiguracije (Admin:Email,
/// Admin:Password) ako administrator s tim e-mailom ne postoji. Postojeće
/// administratore nikad ne mijenja, pa promjena Admin:Password ne mijenja lozinku.
/// </summary>
public static class AdminSeeder
{
    public const string SectionName = "Admin";

    /// <summary>
    /// Ne ruši aplikaciju: nedostajuća ili neispravna konfiguracija i nedostupna baza
    /// samo se zapisuju u log.
    /// </summary>
    public static async Task SeedAdminAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(AdminSeeder));
        var section = app.Configuration.GetSection(SectionName);
        var email = section["Email"]?.Trim();
        var password = section["Password"];

        await using var scope = app.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AdminUser>>();

        try
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                if (!await userManager.Users.AnyAsync(cancellationToken))
                {
                    logger.LogWarning(
                        "Nema nijednog administratora, a Admin:Email i Admin:Password nisu postavljeni. " +
                        "Bez administratora se nitko ne može prijaviti; postavi ih (npr. varijablama okruženja " +
                        "Admin__Email i Admin__Password) i ponovno pokreni aplikaciju.");
                }
                return;
            }

            if (await userManager.FindByEmailAsync(email) is not null)
            {
                return;
            }

            var result = await userManager.CreateAsync(new AdminUser { UserName = email, Email = email }, password);
            if (!result.Succeeded)
            {
                logger.LogError(
                    "Administrator iz konfiguracije (sekcija {Section}) nije stvoren: {Errors}",
                    SectionName,
                    string.Join("; ", result.Errors.Select(e => e.Description)));
                return;
            }

            logger.LogInformation("Stvoren administrator {Email} iz konfiguracije.", email);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Provjera ili stvaranje administratora nije uspjelo. Je li baza dostupna i jesu li migracije primijenjene?");
        }
    }
}
