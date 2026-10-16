using Terminus.Common;
using Terminus.Common.Exceptions;
using Terminus.Model;
using Terminus.Repository.Common;
using Terminus.Service.Common;

namespace Terminus.WebApi.Infrastructure;

/// <summary>
/// Pri pokretanju stvara prijevoznika iz konfiguracije (sekcija "Agency") ako tablica
/// agencies nema nijedan redak. Postojeće prijevoznike nikad ne mijenja.
/// </summary>
public static class AgencySeeder
{
    public const string SectionName = "Agency";

    /// <summary>
    /// Ne ruši aplikaciju: nedostajuća ili neispravna konfiguracija i nedostupna baza
    /// samo se zapisuju u log.
    /// </summary>
    public static async Task SeedAgencyAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(AgencySeeder));
        var section = app.Configuration.GetSection(SectionName);

        await using var scope = app.Services.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAgencyRepository>();
        var service = scope.ServiceProvider.GetRequiredService<IAgencyService>();

        try
        {
            if (await repository.AnyAsync(cancellationToken))
            {
                return;
            }

            var missing = new[] { "Name", "Url" }
                .Where(key => string.IsNullOrWhiteSpace(section[key]))
                .Select(key => $"{SectionName}:{key}")
                .ToList();

            if (missing.Count > 0)
            {
                logger.LogWarning(
                    "Nema nijednog prijevoznika, a konfiguracija za njegovo stvaranje nije potpuna (nedostaje {MissingKeys}). " +
                    "Postavi Agency:Name i Agency:Url (te po želji Agency:Timezone i Agency:Lang), npr. varijablama okruženja " +
                    "Agency__Name i Agency__Url, ili unesi prijevoznika kroz POST /api/agencies. Bez prijevoznika se ne mogu unositi linije.",
                    string.Join(", ", missing));
                return;
            }

            // Code (GTFS agency_id), Timezone i Lang su opcionalni. Bez koda se izvodi iz naziva,
            // a servis postavlja zadanu zonu i jezik (Europe/Zagreb, hr).
            var created = await service.CreateAsync(new AgencyModel
            {
                Code = section["Code"] ?? Slug.From(section["Name"]!, fallback: "1"),
                Name = section["Name"]!,
                Url = section["Url"]!,
                Timezone = section["Timezone"] ?? string.Empty,
                Lang = section["Lang"] ?? string.Empty
            }, cancellationToken);

            logger.LogInformation("Stvoren prijevoznik '{AgencyName}' ({AgencyId}) iz konfiguracije.", created.Name, created.Id);
        }
        catch (ValidationException ex)
        {
            logger.LogError(
                "Prijevoznik iz konfiguracije (sekcija {Section}) nije ispravan pa nije stvoren: {Errors}",
                SectionName,
                string.Join("; ", ex.Errors.SelectMany(e => e.Value.Select(message => $"{SectionName}:{e.Key}: {message}"))));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Provjera ili stvaranje prijevoznika nije uspjelo. Je li baza dostupna i jesu li migracije primijenjene?");
        }
    }
}
