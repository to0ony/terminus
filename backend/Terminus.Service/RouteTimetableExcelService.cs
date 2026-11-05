using System.Text.RegularExpressions;
using Terminus.Common.Exceptions;
using Terminus.Model.Common.Enums;
using Terminus.Service.Common;
using Terminus.Service.Excel;

namespace Terminus.Service;

public class RouteTimetableExcelService(ITimetableService timetableService) : IRouteTimetableExcelService
{
    public async Task<TimetableFile> GetXlsxAsync(
        Guid routeId,
        DateOnly? date,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        // Dan se određuje jednom, da svi listovi budu za isti dan i oko ponoći.
        var all = await timetableService.GetRouteTimetableAsync(routeId, new RouteTimetableQuery(date, null, null), now, cancellationToken);
        if (all.Trips.Count == 0)
        {
            throw new UnprocessableException("Vozni red se ne može izvesti.", ["Linija taj dan ne vozi."]);
        }

        // Linija bez smjerova u GTFS-u ima jedan list, inače svaki smjer svoj.
        var directions = all.Trips
            .Select(t => t.Trip.DirectionId)
            .OfType<TripDirection>()
            .Distinct()
            .Order()
            .ToList();
        var sheets = new List<RouteTimetable>();
        if (directions.Count == 0)
        {
            sheets.Add(all);
        }
        foreach (var direction in directions)
        {
            sheets.Add(await timetableService.GetRouteTimetableAsync(
                routeId, new RouteTimetableQuery(all.Date, null, direction), now, cancellationToken));
        }

        var code = Regex.Replace(all.Route.ShortName ?? all.Route.Code, "[^A-Za-z0-9_-]", "_");
        return new TimetableFile(
            RouteTimetableWorkbook.Create(sheets),
            $"vozni-red-{code}-{all.Date:yyyy-MM-dd}.xlsx");
    }
}
