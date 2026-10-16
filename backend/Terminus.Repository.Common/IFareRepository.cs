using Terminus.Common;
using Terminus.Model.Common;
using Terminus.Model.Common.Filters;

namespace Terminus.Repository.Common;

public interface IFareRepository
{
    /// <returns>Cijena s pravilima ili null.</returns>
    Task<IFareModel?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <returns>Cijene s pravilima, poredane po kodu.</returns>
    Task<PagedResult<IFareModel>> FindAsync(FareFilter filter, CancellationToken cancellationToken = default);

    /// <summary>Sprema cijenu i njezina pravila.</summary>
    Task<IFareModel> InsertAsync(IFareModel fare, CancellationToken cancellationToken = default);

    /// <summary>Mijenja cijenu i zamjenjuje sva pravila u jednoj transakciji.</summary>
    /// <returns>Ažurirana cijena ili null ako ne postoji.</returns>
    Task<IFareModel?> UpdateAsync(IFareModel fare, CancellationToken cancellationToken = default);

    /// <returns>false ako cijena ne postoji.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
