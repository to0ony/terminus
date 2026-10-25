using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;
using Terminus.Service;
using Terminus.Service.Common;
using Terminus.Service.Pdf;

namespace Terminus.Tests.Service;

public class RouteDiagramBuilderTests
{
    private readonly RouteModel _route = new() { Id = Guid.NewGuid(), Code = "L1", ShortName = "1", Color = "D32F2F" };
    private readonly Dictionary<Guid, IStopModel> _stops = [];
    private readonly List<IRouteVariantModel> _variants = [];

    [Fact]
    public void Build_JoinsOppositeStopsWithTheSameName()
    {
        Variant(TripDirection.Outbound, 0, "Kolodvor", "Trg", "Bolnica");
        Variant(TripDirection.Inbound, 0, "Bolnica", "Trg", "Kolodvor");

        var diagram = Build();

        Assert.Equal(["Kolodvor", "Trg", "Bolnica"], diagram.Stops.Select(s => s.Name));
        Assert.All(diagram.Stops, s => Assert.True(s.Forward && s.Backward));
        Assert.Equal("Bolnica", diagram.Forward.Headsign);
        Assert.Equal("Kolodvor", diagram.Backward!.Headsign);
    }

    [Fact]
    public void Build_KeepsStopsOfOneDirectionOnly()
    {
        Variant(TripDirection.Outbound, 0, "Kolodvor", "Ilica", "Bolnica");
        Variant(TripDirection.Inbound, 0, "Bolnica", "Frankopanska", "Kolodvor");

        var diagram = Build();

        Assert.Equal(["Kolodvor", "Ilica", "Frankopanska", "Bolnica"], diagram.Stops.Select(s => s.Name));
        Assert.True(diagram.Stops[1] is { Forward: true, Backward: false });
        Assert.True(diagram.Stops[2] is { Forward: false, Backward: true });
    }

    [Fact]
    public void Build_CountsMinutesFromTheStartOfEachDirection()
    {
        Variant(TripDirection.Outbound, 0, "A", "B", "C");
        Variant(TripDirection.Inbound, 0, "C", "B", "A");

        var diagram = Build();

        // Svaka vožnja traje 2 min uz 30 s stajanja.
        Assert.Equal([0, 3, 5], diagram.Stops.Select(s => s.ForwardMinutes));
        Assert.Equal([5, 3, 0], diagram.Stops.Select(s => s.BackwardMinutes));
    }

    [Fact]
    public void Build_UsesTheMainVariantAndListsTheOthers()
    {
        Variant(TripDirection.Outbound, 1, "Kolodvor", "Trg");
        Variant(TripDirection.Outbound, 0, "Kolodvor", "Trg", "Bolnica");

        var diagram = Build();

        Assert.Null(diagram.Backward);
        Assert.Equal(3, diagram.Stops.Count);
        var other = Assert.Single(diagram.OtherVariants);
        Assert.Equal(("Kolodvor", "Trg", 2), (other.From, other.To, other.StopCount));
    }

    [Fact]
    public void Build_MarksEndsOfOtherVariantsThatAreNotMainEnds()
    {
        Variant(TripDirection.Outbound, 0, "Kolodvor", "Trg", "Bolnica", "Tvrđa");
        Variant(TripDirection.Outbound, 1, "Kolodvor", "Trg", "Bolnica");
        Variant(TripDirection.Inbound, 0, "Tvrđa", "Bolnica", "Trg", "Kolodvor");
        Variant(TripDirection.Inbound, 1, "Trg", "Kolodvor");

        var diagram = Build();

        Assert.Equal(["Trg", "Bolnica"], diagram.Stops.Where(s => s.VariantTerminus).Select(s => s.Name));
        Assert.Equal(["Kolodvor", "Tvrđa"], diagram.Stops.Where(s => s.Terminus).Select(s => s.Name));
    }

    [Fact]
    public void Build_WithoutStops_ReturnsNull()
    {
        _variants.Add(new RouteVariantModel { Id = Guid.NewGuid(), Name = "Prazna", DirectionId = TripDirection.Outbound, Stops = [] });

        Assert.Null(RouteDiagramBuilder.Build(_route, "Prijevoznik", _variants, _stops));
    }

    [Fact]
    public void Generate_ProducesPdf()
    {
        Variant(TripDirection.Outbound, 0, "Kolodvor", "Ilica", "Trg", "Bolnica");
        Variant(TripDirection.Inbound, 0, "Bolnica", "Trg", "Frankopanska", "Kolodvor");
        Variant(TripDirection.Inbound, 1, "Trg", "Kolodvor");

        var pdf = RouteDiagramDocument.Generate(
            Build(), new RouteDiagramPdfOptions(PaperSize.A4, TimetableLanguage.Hr), "Prijevoznik", null, null);

        Assert.Equal("%PDF"u8.ToArray(), pdf[..4]);
    }

    private RouteDiagram Build() => RouteDiagramBuilder.Build(_route, "Prijevoznik", _variants, _stops)!;

    private void Variant(TripDirection direction, int sortOrder, params string[] names)
    {
        var stops = names.Select((name, i) =>
        {
            // Svaki smjer ima svoje stanice (dvije strane ulice), istog naziva.
            var stop = new StopModel { Id = Guid.NewGuid(), Code = $"{name}-{direction}-{sortOrder}", Name = name };
            _stops[stop.Id] = stop;
            return new VariantStop(stop.Id, i == 0 ? 0 : 120, 30);
        }).ToList();

        _variants.Add(new RouteVariantModel
        {
            Id = Guid.NewGuid(),
            RouteId = _route.Id,
            Name = $"Varijanta {direction} {sortOrder}",
            DirectionId = direction,
            SortOrder = sortOrder,
            Stops = stops
        });
    }
}
