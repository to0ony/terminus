using System.Text.RegularExpressions;
using Terminus.Common.Exceptions;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;
using Terminus.Repository.Common;
using Terminus.Service.Common;
using Terminus.Service.Pdf;

namespace Terminus.Service;

public class RouteDiagramService(
    IRouteRepository routeRepository,
    IRouteVariantRepository variantRepository,
    IStopRepository stopRepository,
    IDepartureRepository departureRepository,
    ITimetableService timetableService,
    ISettingsService settingsService) : IRouteDiagramService
{
    private const string CannotCreate = "Shema linije se ne može izraditi.";

    public async Task<RouteDiagram> GetAsync(Guid routeId, CancellationToken cancellationToken = default)
    {
        var route = await routeRepository.GetAsync(routeId, cancellationToken)
            ?? throw new NotFoundException($"Linija {routeId} ne postoji.");

        var agency = await AgencyNameAsync(cancellationToken);
        return await BuildAsync(route, agency, cancellationToken)
            ?? throw new UnprocessableException(CannotCreate, ["Linija nema varijantu sa stanicama."]);
    }

    public async Task<TimetablePdf> GetPdfAsync(Guid routeId, RouteDiagramPdfOptions options, CancellationToken cancellationToken = default)
    {
        var diagram = await GetAsync(routeId, cancellationToken);
        var content = await GenerateAsync([diagram], options, [], diagram.AgencyName, cancellationToken);

        var texts = RouteDiagramTexts.For(options.Language);
        return new TimetablePdf(content, $"{texts.FilePrefix}-{FileCode(diagram.Route.ShortName ?? diagram.Route.Code)}.pdf");
    }

    public async Task<TimetablePdf> GetStopPdfAsync(Guid stopId, RouteDiagramPdfOptions options, CancellationToken cancellationToken = default)
    {
        var stop = await stopRepository.GetAsync(stopId, cancellationToken)
            ?? throw new NotFoundException($"Stanica {stopId} ne postoji.");
        var routes = await timetableService.GetStopRoutesAsync(stopId, cancellationToken);
        var agency = await AgencyNameAsync(cancellationToken);

        var diagrams = new List<RouteDiagram>();
        foreach (var route in routes)
        {
            if (await BuildAsync(route, agency, cancellationToken) is { } diagram)
            {
                diagrams.Add(diagram);
            }
        }
        if (diagrams.Count == 0)
        {
            throw new UnprocessableException(CannotCreate, ["Na stanici ne staje nijedna linija sa stanicama."]);
        }

        // Na shemi se ističe stanica, a kod kolodvora i njegovi peroni (po nazivu).
        IEnumerable<IStopModel> highlighted = stop.LocationType == LocationType.Station
            ? [stop, .. await departureRepository.GetPlatformsAsync(stop.Id, cancellationToken)]
            : [stop];
        var content = await GenerateAsync(diagrams, options, highlighted.Select(s => s.Name).ToList(), agency, cancellationToken);

        var texts = RouteDiagramTexts.For(options.Language);
        return new TimetablePdf(content, $"{texts.StopFilePrefix}-{FileCode(stop.PublicCode ?? stop.Code)}.pdf");
    }

    private async Task<string> AgencyNameAsync(CancellationToken cancellationToken) =>
        (await departureRepository.GetPrimaryAgencyAsync(cancellationToken)
            ?? throw new UnprocessableException(CannotCreate, ["Nije unesen nijedan prijevoznik."])).Name;

    private async Task<RouteDiagram?> BuildAsync(IRouteModel route, string agencyName, CancellationToken cancellationToken)
    {
        var variants = await variantRepository.ListByRouteAsync(route.Id, cancellationToken);
        var stopIds = variants.SelectMany(v => v.Stops).Select(s => s.StopId).Distinct().ToList();
        var stops = (await stopRepository.GetManyAsync(stopIds, cancellationToken)).ToDictionary(s => s.Id);
        return RouteDiagramBuilder.Build(route, agencyName, variants, stops);
    }

    /// <summary>PDF s nazivom, bojama i logom iz postavki.</summary>
    private async Task<byte[]> GenerateAsync(
        IReadOnlyList<RouteDiagram> diagrams,
        RouteDiagramPdfOptions options,
        IReadOnlyCollection<string> highlighted,
        string agencyName,
        CancellationToken cancellationToken)
    {
        var settings = await settingsService.GetAsync(cancellationToken);

        (byte[], bool)? logo = null;
        if (settings.LogoUpdatedAt is not null)
        {
            var image = await settingsService.GetLogoAsync(cancellationToken);
            logo = (image.Content, image.ContentType == "image/svg+xml");
        }

        return RouteDiagramDocument.Generate(
            diagrams,
            options,
            settings.DisplayName ?? agencyName,
            settings.PrimaryColor,
            logo,
            highlighted);
    }

    private static string FileCode(string code) => Regex.Replace(code, "[^A-Za-z0-9_-]", "_");
}
