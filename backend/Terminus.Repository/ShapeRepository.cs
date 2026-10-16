using Microsoft.EntityFrameworkCore;
using Npgsql;
using Terminus.Common;
using Terminus.Common.Exceptions;
using Terminus.DAL;
using Terminus.DAL.Entities;
using Terminus.Model.Common;
using Terminus.Model.Common.Filters;
using Terminus.Repository.Common;
using Terminus.Repository.Mapping;

namespace Terminus.Repository;

public class ShapeRepository(TerminusDbContext context) : IShapeRepository
{
    public async Task<IShapeModel?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await context.Shapes
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        var points = await context.ShapePoints
            .AsNoTracking()
            .Where(p => p.ShapeId == id)
            .OrderBy(p => p.Sequence)
            .ToListAsync(cancellationToken);

        return ShapeMapper.ToModel(entity, points.Count, points);
    }

    public async Task<PagedResult<IShapeModel>> FindAsync(ShapeFilter filter, CancellationToken cancellationToken = default)
    {
        var query = context.Shapes.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var pattern = LikePattern.Contains(filter.Search);
            query = query.Where(s => EF.Functions.ILike(s.Code, pattern, LikePattern.EscapeCharacter));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var rows = await query
            .OrderBy(s => s.Code)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(s => new { Shape = s, PointCount = s.Points.Count })
            .ToListAsync(cancellationToken);

        return new PagedResult<IShapeModel>(
            rows.Select(r => ShapeMapper.ToModel(r.Shape, r.PointCount, [])).ToList(),
            filter.Page,
            filter.PageSize,
            totalCount);
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Shapes.AnyAsync(s => s.Id == id, cancellationToken);

    public async Task<IShapeModel> InsertAsync(IShapeModel shape, CancellationToken cancellationToken = default)
    {
        var entity = new Shape { Code = shape.Code };
        context.Shapes.Add(entity);

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        // Id trase vraća baza, pa se točke spremaju nakon nje.
        await SaveChangesAsync(shape, cancellationToken);

        var points = shape.Points.Select(p => ShapeMapper.ToEntity(entity.Id, p)).ToList();
        context.ShapePoints.AddRange(points);
        await context.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return ShapeMapper.ToModel(entity, points.Count, points);
    }

    public async Task<IShapeModel?> UpdateAsync(IShapeModel shape, CancellationToken cancellationToken = default)
    {
        var entity = await context.Shapes.FirstOrDefaultAsync(s => s.Id == shape.Id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        // ExecuteDelete se izvršava odmah, izvan SaveChanges, pa sve mora biti u istoj transakciji.
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        await context.ShapePoints
            .Where(p => p.ShapeId == shape.Id)
            .ExecuteDeleteAsync(cancellationToken);

        entity.Code = shape.Code;

        // Promjena točaka je promjena trase, i kad se kod ne mijenja.
        context.Entry(entity).Property(e => e.UpdatedAt).IsModified = true;

        var points = shape.Points.Select(p => ShapeMapper.ToEntity(entity.Id, p)).ToList();
        context.ShapePoints.AddRange(points);

        await SaveChangesAsync(shape, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return ShapeMapper.ToModel(entity, points.Count, points);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Točke se brišu kaskadno, a vožnje dobivaju shape_id = NULL.
        var deleted = await context.Shapes
            .Where(s => s.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return deleted > 0;
    }

    private async Task SaveChangesAsync(IShapeModel shape, CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.GetBaseException() is PostgresException { ConstraintName: "uq_shapes_code" })
        {
            throw new ConflictException($"Trasa s kodom '{shape.Code}' već postoji.", ex);
        }
    }
}
