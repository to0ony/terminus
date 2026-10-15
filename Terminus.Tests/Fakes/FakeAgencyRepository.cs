using Terminus.Common;
using Terminus.Model.Common;
using Terminus.Model.Common.Filters;
using Terminus.Repository.Common;

namespace Terminus.Tests.Fakes;

/// <summary>Prijevoznici u memoriji; dovoljno za provjere postojanja u servisima.</summary>
internal sealed class FakeAgencyRepository(params IAgencyModel[] agencies) : IAgencyRepository
{
    private readonly List<IAgencyModel> _agencies = [.. agencies];

    public Task<IAgencyModel?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_agencies.FirstOrDefault(a => a.Id == id));

    public Task<bool> AnyAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_agencies.Count > 0);

    public Task<PagedResult<IAgencyModel>> FindAsync(AgencyFilter filter, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<IAgencyModel> InsertAsync(IAgencyModel agency, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<IAgencyModel?> UpdateAsync(IAgencyModel agency, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
