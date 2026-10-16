using Microsoft.EntityFrameworkCore;
using Npgsql;
using Terminus.Common.Exceptions;
using Terminus.DAL;
using Terminus.DAL.Entities;
using Terminus.Model.Common;
using Terminus.Repository.Common;
using Terminus.Repository.Mapping;

namespace Terminus.Repository;

public class FeedInfoRepository(TerminusDbContext context) : IFeedInfoRepository
{
    public async Task<IFeedInfoModel?> GetAsync(CancellationToken cancellationToken = default)
    {
        var entity = await context.FeedInfo
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == FeedInfo.SingletonId, cancellationToken);

        return entity is null ? null : FeedInfoMapper.ToModel(entity);
    }

    public async Task<IFeedInfoModel> SaveAsync(IFeedInfoModel feedInfo, CancellationToken cancellationToken = default)
    {
        var entity = await context.FeedInfo.FirstOrDefaultAsync(f => f.Id == FeedInfo.SingletonId, cancellationToken);

        if (entity is null)
        {
            entity = new FeedInfo();
            context.FeedInfo.Add(entity);
        }

        FeedInfoMapper.Apply(feedInfo, entity);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.GetBaseException() is PostgresException { ConstraintName: "pk_feed_info" })
        {
            // Netko je istovremeno stvorio zapis.
            throw new ConflictException("Podaci o izdavaču su upravo spremljeni. Pokušaj ponovno.", ex);
        }

        return FeedInfoMapper.ToModel(entity);
    }

    public async Task<bool> DeleteAsync(CancellationToken cancellationToken = default) =>
        await context.FeedInfo.ExecuteDeleteAsync(cancellationToken) > 0;
}
