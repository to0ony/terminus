using Terminus.Model.Common;

namespace Terminus.Service.Common;

/// <summary>
/// Čarobnjak prvog pokretanja: nakon prve prijave administrator bira uvoz
/// postojećeg GTFS-a ili početak od nule. Do tada instanca nema prijevoznika.
/// </summary>
public interface ISetupService
{
    Task<SetupStatus> GetStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Početak od nule: stvara prvog prijevoznika i bilježi da je instanca
    /// postavljena. Bez koda se kod izvodi iz naziva; zona i jezik imaju zadane
    /// vrijednosti kao kod stvaranja prijevoznika.
    /// </summary>
    /// <exception cref="Terminus.Common.Exceptions.ConflictException">Instanca je već postavljena.</exception>
    Task<IAgencyModel> StartBlankAsync(IAgencyModel agency, CancellationToken cancellationToken = default);
}

/// <param name="Completed">true kad je čarobnjak završen; dashboard ga tada više ne prikazuje.</param>
/// <param name="CompletedAt">Kad je instanca postavljena (UTC).</param>
public sealed record SetupStatus(bool Completed, DateTime? CompletedAt, TimetableCounts Counts);
