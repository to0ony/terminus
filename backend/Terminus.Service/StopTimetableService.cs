using System.Globalization;
using System.Text.RegularExpressions;
using QRCoder;
using Terminus.Common;
using Terminus.Common.Exceptions;
using Terminus.Model.Common.Enums;
using Terminus.Repository.Common;
using Terminus.Service.Common;
using Terminus.Service.Pdf;

namespace Terminus.Service;

public class StopTimetableService(
    IStopRepository stopRepository,
    IDepartureRepository departureRepository,
    ITimetableService timetableService,
    ISettingsService settingsService) : IStopTimetableService
{
    /// <summary>Najdulje razdoblje jednog voznog reda.</summary>
    public const int MaxDays = 366;

    /// <summary>Gornja granica polazaka sa stanice za sve kalendare, zaštita od pogrešnih podataka.</summary>
    public const int MaxDepartures = 200_000;

    public async Task<StopTimetable> GetAsync(Guid stopId, DateOnly? from, DateOnly? to, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var stop = await stopRepository.GetAsync(stopId, cancellationToken)
            ?? throw new NotFoundException($"Stanica {stopId} ne postoji.");

        var agency = await departureRepository.GetPrimaryAgencyAsync(cancellationToken)
            ?? throw new UnprocessableException("Vozni red se ne može izraditi.", ["Nije unesen nijedan prijevoznik."]);

        // Bez početka vrijedi danas; bez kraja zadnji dan s voznim redom od početka.
        var overview = await timetableService.GetServiceOverviewAsync(from, now, cancellationToken);
        var start = overview.Date;
        var end = to ?? overview.LastServiceDate
            ?? throw new UnprocessableException(
                "Vozni red se ne može izraditi.",
                [$"Od {start.ToString("d. M. yyyy.", CultureInfo.InvariantCulture)} nema voznog reda."]);

        var errors = new ValidationErrors();
        if (end < start)
        {
            errors.Add("To", "Kraj razdoblja ne može biti prije početka.");
        }
        else if (end.DayNumber - start.DayNumber + 1 > MaxDays)
        {
            errors.Add("To", $"Razdoblje može imati najviše {MaxDays} dana.");
        }
        errors.ThrowIfAny();

        // Vožnje staju na peronima, ne na kolodvoru.
        var stopIds = stop.LocationType == LocationType.Station
            ? (await departureRepository.GetPlatformsAsync(stop.Id, cancellationToken)).Select(p => p.Id).ToList()
            : [stop.Id];

        var calendars = await departureRepository.GetCalendarsAsync(cancellationToken);
        var calendarDates = await departureRepository.GetCalendarDatesAsync(start, end, cancellationToken);

        var departures = stopIds.Count == 0 || calendars.Count == 0
            ? []
            : await departureRepository.GetScheduledAsync(
                stopIds, calendars.Select(c => c.Id).ToList(), 0, GtfsTime.MaxSeconds, MaxDepartures, cancellationToken);

        var sections = StopTimetableBuilder.Build(departures, calendars, calendarDates, start, end);
        if (sections.Count == 0)
        {
            throw new UnprocessableException("Vozni red se ne može izraditi.", ["Sa stanice u tom razdoblju nema polazaka."]);
        }

        return new StopTimetable(stop, agency.Name, start, end, sections);
    }

    public async Task<TimetablePdf> GetPdfAsync(Guid stopId, TimetablePdfOptions options, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var timetable = await GetAsync(stopId, options.From, options.To, now, cancellationToken);
        var settings = await settingsService.GetAsync(cancellationToken);

        (byte[], bool)? logo = null;
        if (settings.LogoUpdatedAt is not null)
        {
            var image = await settingsService.GetLogoAsync(cancellationToken);
            logo = (image.Content, image.ContentType == "image/svg+xml");
        }

        var content = StopTimetableDocument.Generate(
            timetable,
            options,
            settings.DisplayName ?? timetable.AgencyName,
            settings.PrimaryColor,
            logo,
            settings.PublicUrl is { } publicUrl ? BoardQrCode(publicUrl, timetable.Stop.Code) : null);

        var texts = TimetableTexts.For(options.Language);
        var code = Regex.Replace(timetable.Stop.Code, "[^A-Za-z0-9_-]", "_");
        var fileName = $"{texts.FilePrefix}-{code}-{timetable.From.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}.pdf";

        return new TimetablePdf(content, fileName);
    }

    /// <summary>QR kod koji vodi na zaslon polazaka stanice (/board/{kod}).</summary>
    private static string BoardQrCode(string publicUrl, string stopCode)
    {
        var url = $"{publicUrl.TrimEnd('/')}/board/{Uri.EscapeDataString(stopCode)}";
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.M);
        return new SvgQRCode(data).GetGraphic(4);
    }
}
