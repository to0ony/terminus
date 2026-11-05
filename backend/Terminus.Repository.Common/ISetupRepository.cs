using Terminus.Model.Common;

namespace Terminus.Repository.Common;

/// <summary>Čitanje stanja instance za čarobnjak prvog pokretanja.</summary>
public interface ISetupRepository
{
    /// <returns>Broj prijevoznika, stanica, linija, vožnji i sezona.</returns>
    Task<TimetableCounts> GetCountsAsync(CancellationToken cancellationToken = default);
}
