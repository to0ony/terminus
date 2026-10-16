using Terminus.Common;
using Terminus.Model.Common;
using Terminus.Model.Common.Filters;
using Terminus.Repository.Common;

namespace Terminus.Tests.Fakes;

/// <summary>Trase u memoriji. Bilježi što je servis spremio.</summary>
internal sealed class FakeShapeRepository : IShapeRepository
{
    public List<IShapeModel> Shapes { get; } = [];

    public Task<IShapeModel?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Shapes.FirstOrDefault(s => s.Id == id));

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Shapes.Any(s => s.Id == id));

    public Task<IShapeModel> InsertAsync(IShapeModel shape, CancellationToken cancellationToken = default)
    {
        shape.Id = Guid.NewGuid();
        Shapes.Add(shape);
        return Task.FromResult(shape);
    }

    public Task<IShapeModel?> UpdateAsync(IShapeModel shape, CancellationToken cancellationToken = default)
    {
        var index = Shapes.FindIndex(s => s.Id == shape.Id);
        if (index < 0)
        {
            return Task.FromResult<IShapeModel?>(null);
        }

        Shapes[index] = shape;
        return Task.FromResult<IShapeModel?>(shape);
    }

    public Task<PagedResult<IShapeModel>> FindAsync(ShapeFilter filter, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
