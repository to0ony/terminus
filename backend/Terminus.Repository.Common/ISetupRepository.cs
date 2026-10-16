using Terminus.Model.Common;

namespace Terminus.Repository.Common;

/// <summary>Čitanje stanja instance za čarobnjak prvog pokretanja.</summary>
public interface ISetupRepository
{
    /// <returns>Broj prijevoznika, stanica, linija i vožnji.</returns>
    Task<TimetableCounts> GetCountsAsync(CancellationToken cancellationToken = default);
}
