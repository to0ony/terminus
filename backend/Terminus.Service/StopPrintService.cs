using System.Text.RegularExpressions;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Terminus.Common.Exceptions;
using Terminus.Repository.Common;
using Terminus.Service.Common;
using Terminus.Service.Pdf;

namespace Terminus.Service;

public class StopPrintService(
    IStopRepository stopRepository,
    StopTimetableService timetableService,
    RouteDiagramService routeDiagramService) : IStopPrintService
{
    /// <summary>Najviše stanica u jednom PDF-u.</summary>
    public const int MaxStops = 100;

    public async Task<StopPrintPdf> GetPdfAsync(
        IReadOnlyCollection<Guid> stopIds,
        StopPrintOptions options,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var ids = stopIds.Distinct().ToList();
        var errors = new ValidationErrors();
        if (ids.Count == 0)
        {
            errors.Add("Ids", "Odaberi barem jednu stanicu.");
        }
        else if (ids.Count > MaxStops)
        {
            errors.Add("Ids", $"U jedan PDF stane najviše {MaxStops} stanica.");
        }
        if (!options.Timetable && !options.Routes)
        {
            errors.Add("Timetable", "Odaberi vozni red, sheme linija ili oboje.");
        }
        errors.ThrowIfAny();

        var documents = new List<IDocument>();
        var skipped = new List<string>();
        string? code = null;
        foreach (var id in ids)
        {
            var stop = await stopRepository.GetAsync(id, cancellationToken)
                ?? throw new NotFoundException($"Stanica {id} ne postoji.");
            code ??= stop.PublicCode ?? stop.Code;

            var timetable = options.Timetable
                ? await timetableService.ComposeAsync(stop.Id, options.TimetableOptions, now, cancellationToken)
                : null;
            var routes = options.Routes
                ? await routeDiagramService.ComposeStopAsync(stop, options.RouteOptions, cancellationToken)
                : null;

            if (timetable is null && routes is null)
            {
                skipped.Add(stop.Name);
                continue;
            }
            documents.AddRange(new[] { timetable, routes }.OfType<IDocument>());
        }

        if (documents.Count == 0)
        {
            throw new UnprocessableException(
                "PDF se ne može izraditi.",
                [ids.Count == 1
                    ? "Stanica u tom razdoblju nema polazaka ni linija za ispis."
                    : "Nijedna odabrana stanica u tom razdoblju nema polazaka ni linija za ispis."]);
        }

        var content = Document.Merge(documents).UseOriginalPageNumbers().GeneratePdf();
        var printed = ids.Count - skipped.Count;
        var name = printed == 1 ? Regex.Replace(code!, "[^A-Za-z0-9_-]", "_") : printed.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return new StopPrintPdf(content, $"{FilePrefix(options)}-{name}.pdf", skipped);
    }

    private static string FilePrefix(StopPrintOptions options)
    {
        var language = options.TimetableOptions.Language;
        return (options.Timetable, options.Routes) switch
        {
            (true, false) => TimetableTexts.For(language).FilePrefix,
            (false, true) => RouteDiagramTexts.For(language).StopFilePrefix,
            _ => language == TimetableLanguage.En ? "stops" : "stanice"
        };
    }
}
