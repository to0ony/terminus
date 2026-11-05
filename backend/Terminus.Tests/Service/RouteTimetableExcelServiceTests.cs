using ClosedXML.Excel;
using Terminus.Common.Exceptions;
using Terminus.Model;
using Terminus.Model.Common;
using Terminus.Model.Common.Enums;
using Terminus.Service;
using Terminus.Service.Common;
using Terminus.Tests.Fakes;

namespace Terminus.Tests.Service;

public class RouteTimetableExcelServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 12, 23, 8, 0, 0, TimeSpan.FromHours(1));

    // Srijeda.
    private static readonly DateOnly Wednesday = new(2026, 12, 23);

    private readonly FakeTimetableRepository _timetable = new();
    private readonly FakeRouteRepository _routes = new();
    private readonly FakeDepartureRepository _departures = new();
    private readonly List<IStopModel> _stops = [];

    private readonly RouteModel _route = new() { Id = Guid.NewGuid(), Code = "12", ShortName = "12", LongName = "Centar – Bolnica" };
    private readonly ServiceCalendarModel _workdays;
    private readonly ServiceCalendarModel _saturday;
    private readonly StopModel _a, _b, _c;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public RouteTimetableExcelServiceTests()
    {
        _routes.Routes.Add(_route);
        _timetable.Routes.Add(_route);

        _workdays = Calendar("RD", workdays: true);
        _saturday = Calendar("SUB", saturday: true);
        _departures.Calendars.AddRange([_workdays, _saturday]);

        _a = AddStop("Centar");
        _b = AddStop("Tržnica");
        _c = AddStop("Bolnica");
    }

    private RouteTimetableExcelService Service =>
        new(new TimetableService(_timetable, _routes, new FakeStopRepository(_stops), _departures));

    [Fact]
    public async Task GetXlsxAsync_PutsEachDirectionOnItsOwnSheet()
    {
        AddTrip(_workdays, TripDirection.Outbound, "Bolnica", (_a, "07:00"), (_b, "07:10"), (_c, "07:20"));
        AddTrip(_workdays, TripDirection.Outbound, "Bolnica", (_a, "06:00"), (_c, "06:15"));
        AddTrip(_workdays, TripDirection.Inbound, "Centar", (_c, "08:00"), (_b, "08:10"), (_a, "08:20"));
        AddTrip(_saturday, TripDirection.Outbound, "Bolnica", (_a, "09:00"), (_c, "09:15"));

        var file = await Service.GetXlsxAsync(_route.Id, Wednesday, Now, Ct);

        Assert.Equal("vozni-red-12-2026-12-23.xlsx", file.FileName);
        using var workbook = new XLWorkbook(new MemoryStream(file.Content));
        Assert.Equal(["Smjer Bolnica", "Smjer Centar"], workbook.Worksheets.Select(s => s.Name));

        var outbound = workbook.Worksheet(1);
        Assert.Equal("Linija 12 Centar – Bolnica", outbound.Cell(1, 1).GetString());
        Assert.Equal("Srijeda, 23. 12. 2026. · smjer Bolnica · kalendari: RD", outbound.Cell(2, 1).GetString());

        // Subotnja vožnja nije u tablici; vožnje su poredane po polasku.
        Assert.Equal("Stanica", outbound.Cell(4, 1).GetString());
        Assert.Equal(2, outbound.Cell(4, 3).GetValue<int>());
        Assert.True(outbound.Cell(4, 4).IsEmpty());
        Assert.Equal(["Centar", "Tržnica", "Bolnica"], [outbound.Cell(5, 1).GetString(), outbound.Cell(6, 1).GetString(), outbound.Cell(7, 1).GetString()]);
        Assert.Equal(TimeSpan.FromHours(6), Time(outbound.Cell(5, 2)));
        Assert.True(outbound.Cell(6, 2).IsEmpty());
        Assert.Equal(new TimeSpan(7, 10, 0), Time(outbound.Cell(6, 3)));
        Assert.Equal("[h]:mm", outbound.Cell(5, 2).Style.NumberFormat.Format);

        Assert.Equal(new TimeSpan(8, 20, 0), Time(workbook.Worksheet(2).Cell(7, 2)));
    }

    [Fact]
    public async Task GetXlsxAsync_RouteWithoutDirections_UsesOneSheet()
    {
        AddTrip(_workdays, null, null, (_a, "07:00"), (_c, "07:20"));

        var file = await Service.GetXlsxAsync(_route.Id, Wednesday, Now, Ct);

        using var workbook = new XLWorkbook(new MemoryStream(file.Content));
        Assert.Equal(["Vožnje"], workbook.Worksheets.Select(s => s.Name));
    }

    [Fact]
    public async Task GetXlsxAsync_TripsAfterMidnight_KeepHoursAbove24()
    {
        AddTrip(_workdays, TripDirection.Outbound, null, (_a, "23:50"), (_c, "24:10"));

        var file = await Service.GetXlsxAsync(_route.Id, Wednesday, Now, Ct);

        using var workbook = new XLWorkbook(new MemoryStream(file.Content));
        Assert.Equal(new TimeSpan(24, 10, 0), Time(workbook.Worksheet(1).Cell(6, 2)));
    }

    [Fact]
    public async Task GetXlsxAsync_NoTripsThatDay_Throws422()
    {
        AddTrip(_saturday, TripDirection.Outbound, null, (_a, "09:00"), (_c, "09:15"));

        await Assert.ThrowsAsync<UnprocessableException>(() => Service.GetXlsxAsync(_route.Id, Wednesday, Now, Ct));
    }

    /// <summary>Excel čuva vrijeme kao dio dana (6:00 je 0,25), a ClosedXML ga čita kao datum od 31. 12. 1899.</summary>
    private static TimeSpan Time(IXLCell cell) => cell.GetDateTime() - new DateTime(1899, 12, 31);

    private StopModel AddStop(string name)
    {
        var stop = new StopModel { Id = Guid.NewGuid(), Code = name, Name = name };
        _stops.Add(stop);
        return stop;
    }

    private void AddTrip(
        ServiceCalendarModel calendar,
        TripDirection? direction,
        string? headsign,
        params (StopModel Stop, string Time)[] stops)
    {
        var trip = new TripModel
        {
            Id = Guid.NewGuid(),
            RouteId = _route.Id,
            ServiceCalendarId = calendar.Id,
            Code = $"T{_timetable.Trips.Count + 1}",
            DirectionId = direction,
            Headsign = headsign
        };

        var stopTimes = stops
            .Select((s, i) =>
            {
                var parts = s.Time.Split(':');
                var seconds = int.Parse(parts[0]) * 3600 + int.Parse(parts[1]) * 60;
                return (IStopTimeModel)new StopTimeModel
                {
                    TripId = trip.Id,
                    StopSequence = i + 1,
                    StopId = s.Stop.Id,
                    ArrivalTime = seconds,
                    DepartureTime = seconds,
                    Timepoint = true
                };
            })
            .ToList();

        _timetable.Trips.Add(new TripStopTimes(trip, stopTimes));
    }

    private static ServiceCalendarModel Calendar(string code, bool workdays = false, bool saturday = false) => new()
    {
        Id = Guid.NewGuid(),
        Code = code,
        Name = code,
        Monday = workdays,
        Tuesday = workdays,
        Wednesday = workdays,
        Thursday = workdays,
        Friday = workdays,
        Saturday = saturday,
        StartDate = new DateOnly(2026, 1, 1),
        EndDate = new DateOnly(2026, 12, 31)
    };
}
