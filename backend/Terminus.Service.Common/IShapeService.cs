using Terminus.Common;
using Terminus.Model.Common;
using Terminus.Model.Common.Filters;

namespace Terminus.Service.Common;

/// <summary>Trase vožnji (GTFS shapes).</summary>
public interface IShapeService
{
    /// <returns>Trasa s točkama.</returns>
    Task<IShapeModel> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <returns>Trase bez točaka.</returns>
    Task<PagedResult<IShapeModel>> FindAsync(ShapeFilter filter, CancellationToken cancellationToken = default);

    /// <summary>Redni brojevi točaka dodjeljuju se prema redoslijedu u popisu (1, 2, 3 ...).</summary>
    Task<IShapeModel> CreateAsync(IShapeModel shape, CancellationToken cancellationToken = default);

    /// <summary>Mijenja kod i zamjenjuje sve točke.</summary>
    Task<IShapeModel> UpdateAsync(Guid id, IShapeModel shape, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
