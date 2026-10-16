using Terminus.Common;
using Terminus.Common.Exceptions;
using Terminus.Model.Common;
using Terminus.Model.Common.Filters;
using Terminus.Repository.Common;
using Terminus.Service.Common;

namespace Terminus.Service;

public class ShapeService(IShapeRepository repository) : IShapeService
{
    /// <summary>GTFS: trasa ima barem dvije točke.</summary>
    private const int MinPoints = 2;

    public async Task<IShapeModel> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        await repository.GetAsync(id, cancellationToken) ?? throw ShapeNotFound(id);

    public Task<PagedResult<IShapeModel>> FindAsync(ShapeFilter filter, CancellationToken cancellationToken = default)
    {
        var errors = new ValidationErrors();
        errors.AddPagingErrors(filter.Page, filter.PageSize);
        errors.ThrowIfAny();

        return repository.FindAsync(filter, cancellationToken);
    }

    public async Task<IShapeModel> CreateAsync(IShapeModel shape, CancellationToken cancellationToken = default)
    {
        shape.Id = Guid.Empty;
        Normalize(shape);
        Validate(shape);

        return await repository.InsertAsync(shape, cancellationToken);
    }

    public async Task<IShapeModel> UpdateAsync(Guid id, IShapeModel shape, CancellationToken cancellationToken = default)
    {
        shape.Id = id;
        Normalize(shape);
        Validate(shape);

        return await repository.UpdateAsync(shape, cancellationToken) ?? throw ShapeNotFound(id);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!await repository.DeleteAsync(id, cancellationToken))
        {
            throw ShapeNotFound(id);
        }
    }

    private static void Normalize(IShapeModel shape)
    {
        shape.Code = shape.Code?.Trim() ?? string.Empty;

        for (var i = 0; i < shape.Points.Count; i++)
        {
            shape.Points[i].Sequence = i + 1;
        }

        shape.PointCount = shape.Points.Count;
    }

    private static void Validate(IShapeModel shape)
    {
        var errors = new ValidationErrors();

        if (shape.Code.Length == 0)
        {
            errors.Add(nameof(shape.Code), "Kod je obavezan.");
        }

        if (shape.Points.Count < MinPoints)
        {
            errors.Add(nameof(shape.Points), $"Trasa mora imati barem {MinPoints} točke.");
        }

        for (var i = 0; i < shape.Points.Count; i++)
        {
            var point = shape.Points[i];
            var prefix = $"Points[{i}]";

            if (!double.IsFinite(point.Latitude) || point.Latitude is < -90 or > 90)
            {
                errors.Add($"{prefix}.Latitude", "Geografska širina mora biti između -90 i 90.");
            }

            if (!double.IsFinite(point.Longitude) || point.Longitude is < -180 or > 180)
            {
                errors.Add($"{prefix}.Longitude", "Geografska dužina mora biti između -180 i 180.");
            }

            if (point.DistTraveled is { } distance)
            {
                if (!double.IsFinite(distance) || distance < 0)
                {
                    errors.Add($"{prefix}.DistTraveled", "Prijeđena udaljenost ne smije biti negativna.");
                }
                else if (i > 0 && shape.Points[i - 1].DistTraveled is { } previous && distance < previous)
                {
                    errors.Add($"{prefix}.DistTraveled", "Prijeđena udaljenost ne smije biti manja nego u prethodnoj točki.");
                }
            }
        }

        errors.ThrowIfAny();
    }

    private static NotFoundException ShapeNotFound(Guid id) => new($"Trasa {id} ne postoji.");
}
