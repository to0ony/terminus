using Terminus.Model.Common;
using Terminus.Repository.Common;

namespace Terminus.Tests.Fakes;

/// <summary>Sezone u memoriji; zadnje spremljene iznimke kalendara su u <see cref="CalendarDates"/>.</summary>
internal sealed class FakeSeasonRepository : ISeasonRepository
{
    public Dictionary<Guid, SeasonState> Seasons { get; } = [];
    public Dictionary<Guid, int> TripCounts { get; } = [];
    public IReadOnlyList<ICalendarDateModel> CalendarDates { get; private set; } = [];

    public Task<IReadOnlyList<SeasonState>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SeasonState>>(Seasons.Values.OrderBy(s => s.Season.StartDate).ToList());

    public Task<SeasonState?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Seasons.GetValueOrDefault(id));

    public Task<IReadOnlyDictionary<Guid, int>> CountTripsAsync(IReadOnlyCollection<Guid> calendarIds, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyDictionary<Guid, int>>(
            TripCounts.Where(kv => calendarIds.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value));

    public Task<ITimetableSeasonModel?> FindOverlappingAsync(DateOnly start, DateOnly end, Guid? exceptId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Seasons.Values
            .Select(s => s.Season)
            .FirstOrDefault(s => s.Id != exceptId && s.StartDate <= end && s.EndDate >= start));

    public Task SaveAsync(SeasonState state, IReadOnlyList<ICalendarDateModel> calendarDates, CancellationToken cancellationToken = default)
    {
        Seasons[state.Season.Id] = state;
        CalendarDates = calendarDates;
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Seasons.Remove(id));
}
