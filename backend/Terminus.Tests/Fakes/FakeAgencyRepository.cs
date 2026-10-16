using Terminus.Common;
using Terminus.Model.Common;
using Terminus.Model.Common.Filters;
using Terminus.Repository.Common;

namespace Terminus.Tests.Fakes;

/// <summary>Prijevoznici u memoriji.</summary>
internal sealed class FakeAgencyRepository(params IAgencyModel[] agencies) : IAgencyRepository
{
    private readonly List<IAgencyModel> _agencies = [.. agencies];

    public Task<IAgencyModel?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_agencies.FirstOrDefault(a => a.Id == id));

    public Task<string?> GetOtherTimezoneAsync(Guid exceptId, string timezone, CancellationToken cancellationToken = default) =>
        Task.FromResult(_agencies.FirstOrDefault(a => a.Id != exceptId && a.Timezone != timezone)?.Timezone);

    public Task<IAgencyModel?> GetFirstAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_agencies.FirstOrDefault());

    public Task<bool> AnyAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_agencies.Count > 0);

    public Task<PagedResult<IAgencyModel>> FindAsync(AgencyFilter filter, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<IAgencyModel> InsertAsync(IAgencyModel agency, CancellationToken cancellationToken = default)
    {
        agency.Id = Guid.NewGuid();
        _agencies.Add(agency);
        return Task.FromResult(agency);
    }

    public Task<IAgencyModel?> UpdateAsync(IAgencyModel agency, CancellationToken cancellationToken = default)
    {
        var index = _agencies.FindIndex(a => a.Id == agency.Id);
        if (index < 0)
        {
            return Task.FromResult<IAgencyModel?>(null);
        }

        _agencies[index] = agency;
        return Task.FromResult<IAgencyModel?>(agency);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
