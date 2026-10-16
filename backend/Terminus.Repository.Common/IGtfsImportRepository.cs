using Terminus.Model.Common;

namespace Terminus.Repository.Common;

/// <summary>Upis uvezenog GTFS feeda.</summary>
public interface IGtfsImportRepository
{
    /// <summary>
    /// U jednoj transakciji briše sve podatke voznog reda (prijevoznici, stanice,
    /// linije, kalendari, vožnje, trase, cijene, feed_info) i upisuje zadane.
    /// Postavke instance i administratori ostaju.
    /// </summary>
    Task ReplaceAllAsync(GtfsFeedData feed, CancellationToken cancellationToken = default);
}
