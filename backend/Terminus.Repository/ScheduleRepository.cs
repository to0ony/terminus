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

    public async Task<IReadOnlyList<TripStopTimes>> GetDepartureTripsAsync(IReadOnlyCollection<Guid> departureIds, CancellationToken cancellationToken = default)
    {
        if (departureIds.Count == 0)
        {
            return [];
        }

        var trips = await context.Trips
            .AsNoTracking()
            .Where(t => t.DepartureId != null && departureIds.Contains(t.DepartureId.Value))
            .Include(t => t.StopTimes)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return trips
            .Select(t => new TripStopTimes(
                TripMapper.ToModel(t),
                t.StopTimes.OrderBy(st => st.StopSequence).Select(TripMapper.ToModel).ToList()))
            .ToList();
    }

    public async Task SaveAsync(IReadOnlyList<TripStopTimes> upserts, IReadOnlyCollection<Guid> deletes, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        // Polasci obrisanih i izmijenjenih vožnji: na kraju se ukidaju oni s jednom vožnjom.
        var touched = await context.Trips
            .Where(t => t.DepartureId != null && deletes.Contains(t.Id))
            .Select(t => t.DepartureId!.Value)
            .ToListAsync(cancellationToken);
        touched.AddRange(upserts.Select(u => u.Trip.DepartureId).OfType<Guid>());

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
            // Polazak mijenja samo grafikon (Apply ga ne dira, da ga opći API ne prebriše).
            trip.DepartureId = upsert.Trip.DepartureId;
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

        if (touched.Count > 0)
        {
            var single = await context.Trips
                .Where(t => t.DepartureId != null && touched.Contains(t.DepartureId.Value))
                .GroupBy(t => t.DepartureId)
                .Where(g => g.Count() == 1)
                .Select(g => g.Key)
                .ToListAsync(cancellationToken);
            if (single.Count > 0)
            {
                await context.Trips
                    .Where(t => single.Contains(t.DepartureId))
                    .ExecuteUpdateAsync(set => set.SetProperty(t => t.DepartureId, (Guid?)null), cancellationToken);
            }
        }

        await transaction.CommitAsync(cancellationToken);
    }
}
