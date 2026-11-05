using ClosedXML.Excel;
using Terminus.Service.Common;

namespace Terminus.Service.Excel;

/// <summary>
/// Excel vozni red linije: svaki smjer na svom listu, stanice su redci, a vožnje
/// stupci poredani po polasku. Vremena su prava vremena (ne tekst), pa se po
/// njima može računati i sortirati.
/// </summary>
internal static class RouteTimetableWorkbook
{
    private static readonly string[] Days =
        ["Nedjelja", "Ponedjeljak", "Utorak", "Srijeda", "Četvrtak", "Petak", "Subota"];

    private const int HeaderRow = 4;

    /// <summary>Najdulji naziv lista u Excelu.</summary>
    private const int MaxSheetName = 31;

    public static byte[] Create(IReadOnlyList<RouteTimetable> timetables)
    {
        using var workbook = new XLWorkbook();
        foreach (var timetable in timetables)
        {
            var target = Target(timetable);
            var name = SheetName(target is null ? "Vožnje" : $"Smjer {target}", workbook);
            AddSheet(workbook.Worksheets.Add(name), timetable, target);
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void AddSheet(IXLWorksheet sheet, RouteTimetable timetable, string? target)
    {
        var route = timetable.Route;
        var title = string.Join(" ", new[] { route.ShortName, route.LongName }.Where(s => !string.IsNullOrWhiteSpace(s)));
        sheet.Cell(1, 1).SetValue($"Linija {(title.Length > 0 ? title : route.Code)}")
            .Style.Font.SetBold().Font.SetFontSize(14);

        var subtitle = new List<string>();
        if (timetable.Date is { } date)
        {
            subtitle.Add($"{Days[(int)date.DayOfWeek]}, {date.Day}. {date.Month}. {date.Year}.");
        }
        if (target is not null)
        {
            subtitle.Add($"smjer {target}");
        }
        subtitle.Add($"kalendari: {string.Join(", ", timetable.Calendars.Select(c => c.Code))}");
        sheet.Cell(2, 1).SetValue(string.Join(" · ", subtitle));

        var trips = timetable.Trips;
        var header = sheet.Row(HeaderRow);
        sheet.Cell(HeaderRow, 1).SetValue("Stanica");
        for (var i = 0; i < trips.Count; i++)
        {
            sheet.Cell(HeaderRow, i + 2).SetValue(i + 1);
        }

        // Vozilo (GTFS block_id) samo ako ga ima neka vožnja.
        var firstStopRow = HeaderRow + 1;
        if (trips.Any(t => t.Trip.BlockId is not null))
        {
            sheet.Cell(firstStopRow, 1).SetValue("Vozilo");
            for (var i = 0; i < trips.Count; i++)
            {
                sheet.Cell(firstStopRow, i + 2).SetValue(trips[i].Trip.BlockId ?? string.Empty);
            }
            sheet.Row(firstStopRow).Style.Font.SetFontColor(XLColor.Gray);
            firstStopRow++;
        }

        var approximate = false;
        for (var row = 0; row < timetable.Stops.Count; row++)
        {
            var stop = timetable.Stops[row];
            var excelRow = firstStopRow + row;
            sheet.Cell(excelRow, 1).SetValue(stop.PlatformCode is null ? stop.Name : $"{stop.Name} · {stop.PlatformCode}");

            for (var i = 0; i < trips.Count; i++)
            {
                if (trips[i].Times[row] is not { } time)
                {
                    continue;
                }
                var cell = sheet.Cell(excelRow, i + 2);
                cell.SetValue(TimeSpan.FromSeconds(time.DepartureTime));
                // [h] jer vremena voznog reda idu i preko 24 h (vožnje iza ponoći).
                cell.Style.NumberFormat.SetFormat("[h]:mm");
                if (!time.Timepoint)
                {
                    cell.Style.Font.SetItalic();
                    approximate = true;
                }
            }
        }

        var lastRow = firstStopRow + timetable.Stops.Count - 1;
        var lastColumn = trips.Count + 1;
        var table = sheet.Range(HeaderRow, 1, lastRow, lastColumn);
        table.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin)
            .Border.SetInsideBorder(XLBorderStyleValues.Thin)
            .Border.SetOutsideBorderColor(XLColor.LightGray)
            .Border.SetInsideBorderColor(XLColor.LightGray);
        sheet.Range(HeaderRow, 2, lastRow, lastColumn).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        header.Style.Font.SetBold();
        sheet.Range(HeaderRow, 1, HeaderRow, lastColumn).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#EEF1F5"));

        if (approximate)
        {
            sheet.Cell(lastRow + 2, 1).SetValue("Ukošena vremena su okvirna.")
                .Style.Font.SetItalic().Font.SetFontColor(XLColor.Gray);
        }

        sheet.Column(1).AdjustToContents(HeaderRow, lastRow);
        sheet.Columns(2, lastColumn).Width = 7;

        // Stanice i brojevi vožnji ostaju vidljivi pri pomicanju i na svakoj ispisanoj stranici.
        sheet.SheetView.Freeze(HeaderRow, 1);
        sheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        sheet.PageSetup.PaperSize = XLPaperSize.A4Paper;
        sheet.PageSetup.FitToPages(0, 1);
        sheet.PageSetup.SetRowsToRepeatAtTop(HeaderRow, HeaderRow);
        sheet.PageSetup.SetColumnsToRepeatAtLeft(1, 1);
    }

    /// <summary>Cilj smjera: najčešći natpis vožnji, a bez njega zadnja stanica; null bez smjera.</summary>
    private static string? Target(RouteTimetable timetable)
    {
        if (timetable.DirectionId is null)
        {
            return null;
        }
        return timetable.Trips
            .Select(t => t.Trip.Headsign)
            .OfType<string>()
            .CountBy(h => h)
            .OrderByDescending(h => h.Value)
            .Select(h => h.Key)
            .FirstOrDefault()
            ?? timetable.Stops.LastOrDefault()?.Name;
    }

    /// <summary>Naziv lista bez znakova koje Excel ne dopušta, skraćen i jedinstven.</summary>
    private static string SheetName(string name, XLWorkbook workbook)
    {
        var clean = new string(name.Select(c => "[]:*?/\\".Contains(c) ? '-' : c).ToArray()).Trim('\'');
        var candidate = clean.Length > MaxSheetName ? clean[..MaxSheetName] : clean;
        for (var i = 2; workbook.Worksheets.Contains(candidate); i++)
        {
            var suffix = $" ({i})";
            candidate = (clean.Length > MaxSheetName - suffix.Length ? clean[..(MaxSheetName - suffix.Length)] : clean) + suffix;
        }
        return candidate;
    }
}
