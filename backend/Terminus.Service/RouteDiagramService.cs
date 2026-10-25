using System.Text.RegularExpressions;
using Terminus.Common.Exceptions;
using Terminus.Repository.Common;
using Terminus.Service.Common;
using Terminus.Service.Pdf;

namespace Terminus.Service;

public class RouteDiagramService(
    IRouteRepository routeRepository,
    IRouteVariantRepository variantRepository,
    IStopRepository stopRepository,
    IDepartureRepository departureRepository,
    ISettingsService settingsService) : IRouteDiagramService
{
    public async Task<RouteDiagram> GetAsync(Guid routeId, CancellationToken cancellationToken = default)
    {
        var route = await routeRepository.GetAsync(routeId, cancellationToken)
            ?? throw new NotFoundException($"Linija {routeId} ne postoji.");

        var agency = await departureRepository.GetPrimaryAgencyAsync(cancellationToken)
            ?? throw new UnprocessableException("Shema linije se ne može izraditi.", ["Nije unesen nijedan prijevoznik."]);

        var variants = await variantRepository.ListByRouteAsync(routeId, cancellationToken);
        var stopIds = variants.SelectMany(v => v.Stops).Select(s => s.StopId).Distinct().ToList();
        var stops = (await stopRepository.GetManyAsync(stopIds, cancellationToken)).ToDictionary(s => s.Id);

        return RouteDiagramBuilder.Build(route, agency.Name, variants, stops)
            ?? throw new UnprocessableException("Shema linije se ne može izraditi.", ["Linija nema varijantu sa stanicama."]);
    }

    public async Task<TimetablePdf> GetPdfAsync(Guid routeId, RouteDiagramPdfOptions options, CancellationToken cancellationToken = default)
    {
        var diagram = await GetAsync(routeId, cancellationToken);
        var settings = await settingsService.GetAsync(cancellationToken);

        (byte[], bool)? logo = null;
        if (settings.LogoUpdatedAt is not null)
        {
            var image = await settingsService.GetLogoAsync(cancellationToken);
            logo = (image.Content, image.ContentType == "image/svg+xml");
        }

        var content = RouteDiagramDocument.Generate(
            diagram,
            options,
            settings.DisplayName ?? diagram.AgencyName,
            settings.PrimaryColor,
            logo);

        var texts = RouteDiagramTexts.For(options.Language);
        var code = Regex.Replace(diagram.Route.ShortName ?? diagram.Route.Code, "[^A-Za-z0-9_-]", "_");
        return new TimetablePdf(content, $"{texts.FilePrefix}-{code}.pdf");
    }
}
