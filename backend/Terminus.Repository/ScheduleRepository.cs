using Microsoft.EntityFrameworkCore;
using Npgsql;
using Terminus.Common.Exceptions;
using Terminus.DAL;
using Terminus.DAL.Entities;
using Terminus.Model.Common;
using Terminus.Repository.Common;
using Terminus.Repository.Mapping;

namespace Terminus.Repository;

public class ScheduleRepository(TerminusDbContext context) : IScheduleRepository
{
    public async Task<IReadOnlyDictionary<Guid, ITripModel>> GetTripsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        var trips = await context.Trips
            .AsNoTracking()
            .Where(t => ids.Contains(t.Id))
            .ToListAsync(cancellationToken);

        return trips.ToDictionary(t => t.Id, TripMapper.ToModel);
    }

    public async Task SaveAsync(IReadOnlyList<TripStopTimes> upserts, IReadOnlyCollection<Guid> deletes, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        // Vremena se brišu kaskadno s vožnjom.
        if (deletes.Count > 0)
        {
            await context.Trips.Where(t => deletes.Contains(t.Id)).ExecuteDeleteAsync(cancellationToken);
        }

        var ids = upserts.Select(u => u.Trip.Id).ToList();
        var existing = await context.Trips
            .Where(t => ids.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, cancellationToken);

        foreach (var upsert in upserts)
        {
            if (!existing.TryGetValue(upsert.Trip.Id, out var trip))
            {
                trip = new Trip { Id = upsert.Trip.Id };
                context.Trips.Add(trip);
            }
            TripMapper.Apply(upsert.Trip, trip);
        }

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.GetBaseException() is PostgresException { ConstraintName: "uq_trips_code" })
        {
            throw new ConflictException("Vožnja s istim kodom već postoji.", ex);
        }

        // Vremena izmijenjenih i novih vožnji pišu se ispočetka.
        await context.StopTimes.Where(st => ids.Contains(st.TripId)).ExecuteDeleteAsync(cancellationToken);
        context.StopTimes.AddRange(upserts.SelectMany(u => u.StopTimes.Select(TripMapper.ToEntity)));
        await context.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}
