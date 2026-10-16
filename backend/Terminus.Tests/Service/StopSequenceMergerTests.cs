using Terminus.Service;

namespace Terminus.Tests.Service;

public class StopSequenceMergerTests
{
    private static readonly Guid A = Guid.NewGuid(), B = Guid.NewGuid(), C = Guid.NewGuid(),
        D = Guid.NewGuid(), E = Guid.NewGuid(), X = Guid.NewGuid();

    [Fact]
    public void Merge_SameSequence_KeepsIt()
    {
        var rows = StopSequenceMerger.Merge([[A, B, C], [A, B, C]]);

        Assert.Equal([A, B, C], rows);
    }

    [Fact]
    public void Merge_ShortTurnAndSkippedStop_UseRowsOfFullRoute()
    {
        // Puna vožnja, vožnja koja završava ranije i brza vožnja koja preskače B.
        var rows = StopSequenceMerger.Merge([[A, B], [A, B, C, D], [A, C, D]]);

        Assert.Equal([A, B, C, D], rows);
    }

    [Fact]
    public void Merge_Detour_InsertsStopWhereTripVisitsIt()
    {
        // Varijanta skreće preko X između B i C.
        var rows = StopSequenceMerger.Merge([[A, B, C, D], [A, B, X, C, D]]);

        Assert.Equal([A, B, X, C, D], rows);
    }

    [Fact]
    public void Merge_LoopVisitingStopTwice_GetsTwoRows()
    {
        var rows = StopSequenceMerger.Merge([[A, B, C, A], [A, B]]);

        Assert.Equal([A, B, C, A], rows);
    }

    [Fact]
    public void Merge_DifferentBranches_KeepsBothInOrder()
    {
        // Dvije varijante se razilaze nakon B i spajaju na E.
        var rows = StopSequenceMerger.Merge([[A, B, C, E], [A, B, D, E]]);

        Assert.Equal([A, B, C, D, E], rows);
    }

    [Fact]
    public void Map_ReturnsRowOfEachStop()
    {
        List<Guid> rows = [A, B, X, C, D];

        Assert.Equal([0, 3, 4], StopSequenceMerger.Map(rows, [A, C, D]));
    }

    [Fact]
    public void Map_LoopUsesBothRows()
    {
        List<Guid> rows = [A, B, C, A];

        Assert.Equal([0, 1, 2, 3], StopSequenceMerger.Map(rows, [A, B, C, A]));
    }

    [Fact]
    public void Map_EverySequenceFitsMergedRows()
    {
        List<IReadOnlyList<Guid>> sequences = [[A, B, C, E], [A, B, D, E], [B, X, D], [E, A]];

        var rows = StopSequenceMerger.Merge(sequences);

        foreach (var sequence in sequences)
        {
            var indexes = StopSequenceMerger.Map(rows, sequence);
            Assert.Equal(sequence, indexes.Select(i => rows[i]));
            Assert.Equal(indexes.Order(), indexes);
        }
    }
}
