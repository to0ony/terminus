using Terminus.Common;
using Terminus.Model.Common;
using Terminus.Model.Common.Filters;

namespace Terminus.Repository.Common;

public interface IShapeRepository
{
    /// <returns>Trasa s točkama ili null.</returns>
    Task<IShapeModel?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <returns>Trase bez točaka (samo broj točaka), poredane po kodu.</returns>
    Task<PagedResult<IShapeModel>> FindAsync(ShapeFilter filter, CancellationToken cancellationToken = default);

    /// <returns>true ako trasa postoji.</returns>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Sprema trasu i njezine točke.</summary>
    Task<IShapeModel> InsertAsync(IShapeModel shape, CancellationToken cancellationToken = default);

    /// <summary>Mijenja kod i zamjenjuje sve točke u jednoj transakciji.</summary>
    /// <returns>Ažurirana trasa ili null ako ne postoji.</returns>
    Task<IShapeModel?> UpdateAsync(IShapeModel shape, CancellationToken cancellationToken = default);

    /// <summary>Vožnje koje koriste trasu ostaju bez nje.</summary>
    /// <returns>false ako trasa ne postoji.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
