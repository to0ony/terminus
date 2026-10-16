using Terminus.Common;
using Terminus.Model.Common;
using Terminus.Model.Common.Filters;

namespace Terminus.Repository.Common;

public interface IAgencyRepository
{
    Task<IAgencyModel?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<IAgencyModel>> FindAsync(AgencyFilter filter, CancellationToken cancellationToken = default);

    Task<IAgencyModel> InsertAsync(IAgencyModel agency, CancellationToken cancellationToken = default);

    /// <returns>Ažurirana agencija ili null ako ne postoji.</returns>
    Task<IAgencyModel?> UpdateAsync(IAgencyModel agency, CancellationToken cancellationToken = default);

    /// <returns>false ako agencija ne postoji.</returns>
    /// <exception cref="Terminus.Common.Exceptions.ConflictException">Agencija ima linije.</exception>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <returns>Vremenska zona nekog drugog prijevoznika koja se razlikuje od zadane, ili null.</returns>
    Task<string?> GetOtherTimezoneAsync(Guid exceptId, string timezone, CancellationToken cancellationToken = default);

    /// <returns>Prvi uneseni prijevoznik ili null ako ih nema.</returns>
    Task<IAgencyModel?> GetFirstAsync(CancellationToken cancellationToken = default);

    /// <returns>true ako postoji barem jedna agencija.</returns>
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);
}
