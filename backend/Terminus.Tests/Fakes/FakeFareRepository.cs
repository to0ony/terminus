using Terminus.Common;
using Terminus.Model.Common;
using Terminus.Model.Common.Filters;
using Terminus.Repository.Common;

namespace Terminus.Tests.Fakes;

/// <summary>Cijene u memoriji. Bilježi što je servis spremio.</summary>
internal sealed class FakeFareRepository : IFareRepository
{
    public List<IFareModel> Fares { get; } = [];

    public Task<IFareModel?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Fares.FirstOrDefault(f => f.Id == id));

    public Task<IFareModel> InsertAsync(IFareModel fare, CancellationToken cancellationToken = default)
    {
        fare.Id = Guid.NewGuid();
        Fares.Add(fare);
        return Task.FromResult(fare);
    }

    public Task<IFareModel?> UpdateAsync(IFareModel fare, CancellationToken cancellationToken = default)
    {
        var index = Fares.FindIndex(f => f.Id == fare.Id);
        if (index < 0)
        {
            return Task.FromResult<IFareModel?>(null);
        }

        Fares[index] = fare;
        return Task.FromResult<IFareModel?>(fare);
    }

    public Task<PagedResult<IFareModel>> FindAsync(FareFilter filter, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
