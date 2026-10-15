using Microsoft.EntityFrameworkCore;
using Npgsql;
using Terminus.Common.Exceptions;
using Terminus.DAL;
using Terminus.Model.Common;
using Terminus.Repository.Common;
using Terminus.Repository.Mapping;

namespace Terminus.Repository;

public class StopTimeRepository(TerminusDbContext context) : IStopTimeRepository
{
    public async Task<IReadOnlyList<IStopTimeModel>> GetByTripAsync(Guid tripId, CancellationToken cancellationToken = default)
    {
        var entities = await context.StopTimes
            .AsNoTracking()
            .Where(st => st.TripId == tripId)
            .OrderBy(st => st.StopSequence)
            .ToListAsync(cancellationToken);

        return entities.Select(TripMapper.ToModel).ToList();
    }

    public async Task<IReadOnlyList<IStopTimeModel>> ReplaceForTripAsync(
        Guid tripId,
        IReadOnlyList<IStopTimeModel> stopTimes,
        CancellationToken cancellationToken = default)
    {
        // ExecuteDelete/ExecuteUpdate se izvršavaju odmah, izvan SaveChanges,
        // pa sve tri operacije moraju biti u istoj transakciji.
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        await context.StopTimes
            .Where(st => st.TripId == tripId)
            .ExecuteDeleteAsync(cancellationToken);

        var entities = stopTimes.Select(TripMapper.ToEntity).ToList();
        context.StopTimes.AddRange(entities);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.GetBaseException() is PostgresException { ConstraintName: "fk_stop_times_stops_stop_id" })
        {
            throw new NotFoundException("Jedna od stanica u međuvremenu je obrisana.", ex);
        }
        catch (DbUpdateException ex) when (ex.GetBaseException() is PostgresException { ConstraintName: "fk_stop_times_trips_trip_id" })
        {
            throw new NotFoundException($"Vožnja {tripId} ne postoji.", ex);
        }

        // Promjena vremena je promjena vožnje (bitno npr. za GTFS izvoz).
        await context.Trips
            .Where(t => t.Id == tripId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UpdatedAt, DateTime.UtcNow), cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return entities.Select(TripMapper.ToModel).ToList();
    }
}
