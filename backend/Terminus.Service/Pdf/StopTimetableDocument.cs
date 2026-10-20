using QuestPDF;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Terminus.Service.Common;

namespace Terminus.Service.Pdf;

/// <summary>Izgled PDF voznog reda stanice (QuestPDF).</summary>
internal static class StopTimetableDocument
{
    private const string DefaultPrimary = "1565C0";
    private const string Line = "#C9CED6";
    private const string Muted = "#5B6573";
    private const string HeaderBackground = "#EEF1F5";

    // Polasci s napomenom su zaokruženi; susjedni jedan ispod drugog dijele isti okvir.
    private const string MarkBorder = "#37474F";
    private const float MarkBorderWidth = 0.75f;
    private const float MarkRadius = 5;

    /// <summary>Širina jednog polaska u rešetki; stalna, da polasci stoje jedan ispod drugog.</summary>
    private const float SlotWidth = 18;

    /// <summary>Ikona upozorenja (Tabler alert-triangle) uz napomene koje se ne odnose na zaokružene polaske.</summary>
    private const string WarningIcon =
        """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="#37474F" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 9v4"/><path d="M10.363 3.591l-8.106 13.534a1.914 1.914 0 0 0 1.636 2.871h16.214a1.914 1.914 0 0 0 1.636 -2.87l-8.106 -13.536a1.914 1.914 0 0 0 -3.274 0z"/><path d="M12 16h.01"/></svg>""";

    private const float HourWidth = 26;
    private const float CellPadding = 4;

    static StopTimetableDocument()
    {
        // Community licenca; uvjeti su u docs/departures-and-timetables.md. Font Lato
        // dolazi s QuestPDF-om, pa ne ovisi o fontovima poslužitelja.
        Settings.License = LicenseType.Community;
    }

    /// <param name="logo">Logo i je li SVG; null bez loga.</param>
    /// <param name="qrSvg">QR kod kao SVG; null bez javne adrese.</param>
    public static byte[] Generate(
        StopTimetable timetable,
        TimetablePdfOptions options,
        string displayName,
        string? primaryColor,
        (byte[] Content, bool IsSvg)? logo,
        string? qrSvg)
    {
        var texts = TimetableTexts.For(options.Language);
        var primary = $"#{primaryColor ?? DefaultPrimary}";
        var a3 = options.Paper == PaperSize.A3;
        var pageSize = a3 ? PageSizes.A3 : PageSizes.A4;
        var margin = (a3 ? 1.6f : 1.2f) * 72 / 2.54f;
        var columnCount = a3 ? 3 : 2;
        var columnSpacing = a3 ? 24f : 18f;
        // Širina stupca stranice, da se zna koliko polazaka stane u redak rešetke.
        var sectionWidth = (pageSize.Width - 2 * margin - columnSpacing * (columnCount - 1)) / columnCount;
        // Napomene bez godine, osim kad razdoblje prelazi u novu godinu.
        var withYear = timetable.From.Year != timetable.To.Year;

        return Document.Create(document => document.Page(page =>
        {
            page.Size(pageSize);
            page.Margin(margin);
            page.DefaultTextStyle(style => style.FontSize(8.5f).FontFamily("Lato").FontColor(Colors.Black));

            page.Content().Column(content =>
            {
                content.Item().Element(c => Header(c, timetable, texts, displayName, primary, logo, qrSvg));
                content.Item().PaddingTop(4).PaddingBottom(10).Height(3).Background(primary);

                // Odjeljci teku po stupcima stranice, kao u novinama.
                content.Item().MultiColumn(columns =>
                {
                    columns.Columns(columnCount);
                    columns.Spacing(columnSpacing);
                    columns.Content().Element(c => Sections(c, timetable, texts, withYear, sectionWidth));
                });
            });

            page.Footer().PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Text($"{displayName} · {texts.ScheduledTimes}").FontSize(7).FontColor(Muted);
                row.AutoItem().Text(text =>
                {
                    text.DefaultTextStyle(style => style.FontSize(7).FontColor(Muted));
                    text.Span($"{texts.Page} ");
                    text.CurrentPageNumber();
                    text.Span("/");
                    text.TotalPages();
                });
            });
        })).GeneratePdf();
    }

    private static void Header(
        IContainer container,
        StopTimetable timetable,
        TimetableTexts texts,
        string displayName,
        string primary,
        (byte[] Content, bool IsSvg)? logo,
        string? qrSvg)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                if (logo is { } image)
                {
                    var box = column.Item().PaddingBottom(4).Height(28).AlignLeft();
                    if (image.IsSvg)
                    {
                        box.Svg(SvgImage.FromText(System.Text.Encoding.UTF8.GetString(image.Content))).FitHeight();
                    }
                    else
                    {
                        box.Image(image.Content).FitHeight();
                    }
                }
                column.Item().Text(displayName).FontSize(10).SemiBold().FontColor(primary);
                column.Item().Text(timetable.Stop.Name).FontSize(22).Bold();
                column.Item().PaddingTop(2).Text(texts.Period(timetable.From, timetable.To)).FontColor(Muted);
            });

            if (qrSvg is not null)
            {
                row.ConstantItem(76).Column(column =>
                {
                    column.Item().Width(76).Height(76).Svg(SvgImage.FromText(qrSvg)).FitArea();
                    column.Item().AlignCenter().Text(texts.LiveDepartures).FontSize(7).FontColor(Muted);
                });
            }
        });
    }

    private static void Sections(IContainer container, StopTimetable timetable, TimetableTexts texts, bool withYear, float width)
    {
        container.Column(column =>
        {
            column.Spacing(14);
            foreach (var section in timetable.Sections)
            {
                // Naslov odjeljka ne ostaje sam na dnu stranice.
                column.Item().EnsureSpace(90).Element(c => Section(c, section, texts, withYear, width));
            }
        });
    }

    private static void Section(IContainer container, TimetableSection section, TimetableTexts texts, bool withYear, float width)
    {
        var labels = section.Columns.Select(c => texts.Column(c, withYear)).ToList();

        container.Column(column =>
        {
            column.Item().PaddingBottom(4).Row(row =>
            {
                row.Spacing(8);
                row.AutoItem().Element(c => RouteBadge(c, section));
                row.RelativeItem().AlignMiddle().Text(text =>
                {
                    text.Span(texts.Direction(section.Headsign)).FontSize(12).SemiBold();
                    if (section.Platform is not null)
                    {
                        text.Span($"  ·  {texts.Platform(section.Platform)}").FontColor(Muted);
                    }
                });
            });

            column.Item().Element(c => Grid(c, section, labels, texts, width));

            var notes = Notes(section, labels, texts, withYear);
            if (notes.Count > 0)
            {
                column.Item().PaddingTop(4).Column(list =>
                {
                    list.Spacing(1);
                    foreach (var (mark, note) in notes)
                    {
                        list.Item().Row(row =>
                        {
                            row.Spacing(3);
                            if (mark is not null && note.StartsWith(mark, StringComparison.Ordinal))
                            {
                                // Slovo u krugu, kao uz polaske u rešetki.
                                row.AutoItem()
                                    .Border(MarkBorderWidth)
                                    .BorderColor(MarkBorder)
                                    .CornerRadius(MarkRadius)
                                    .PaddingHorizontal(3)
                                    .Text(mark)
                                    .FontSize(7)
                                    .Bold();
                                row.RelativeItem().AlignMiddle().Text(note[mark.Length..].TrimStart()).FontSize(7.5f).FontColor(Muted);
                            }
                            else
                            {
                                // Napomena cijelog odjeljka nema polaske u krugu, pa je ističe ikona.
                                row.ConstantItem(10).Height(10).Svg(WarningIcon).FitArea();
                                row.RelativeItem().AlignMiddle().Text(note).FontSize(7.5f).FontColor(Muted);
                            }
                        });
                    }
                });
            }
        });
    }

    private static void RouteBadge(IContainer container, TimetableSection section)
    {
        var background = $"#{section.RouteColor ?? "37474F"}";
        var foreground = $"#{section.RouteTextColor ?? "FFFFFF"}";

        container
            .MinWidth(30)
            .Background(background)
            .Border(0.5f)
            .BorderColor(Line)
            .PaddingHorizontal(6)
            .PaddingVertical(2)
            .AlignCenter()
            .Text(section.RouteShortName ?? section.RouteLongName ?? "")
            .FontSize(12)
            .Bold()
            .FontColor(foreground);
    }

    /// <summary>
    /// Rešetka sat | minute; svaki redak je jedan sat dana voznog reda. Polasci imaju
    /// stalnu širinu, pa polasci s istim slovom jedan ispod drugog dijele jedan okvir.
    /// </summary>
    private static void Grid(IContainer container, TimetableSection section, IReadOnlyList<string> labels, TimetableTexts texts, float width)
    {
        var hours = section.Columns
            .SelectMany(c => c.Times)
            .Select(t => t.Seconds / 3600)
            .Distinct()
            .Order()
            .ToList();

        // Stupac s više polazaka u satu dobiva više mjesta, da se minute manje prelamaju.
        var weights = section.Columns
            .Select(c => (float)Math.Max(3, c.Times.CountBy(t => t.Seconds / 3600).Max(x => x.Value)))
            .ToList();
        var perLine = weights
            .Select(w => Math.Max(1, (int)(((width - HourWidth) * w / weights.Sum() - 2 * CellPadding) / SlotWidth)))
            .ToList();

        // [stupac][redak]: polasci tog sata.
        var grid = section.Columns
            .Select(c => hours.Select(h => c.Times.Where(t => t.Seconds / 3600 == h).ToList()).ToList())
            .ToList();

        // Redaka teksta u ćeliji; okvir prelazi u idući sat samo iz najviše ćelije retka.
        int Lines(int column, int row) => Math.Max(1, (grid[column][row].Count + perLine[column] - 1) / perLine[column]);
        var rowLines = hours.Select((_, row) => section.Columns.Select((_, column) => Lines(column, row)).Max()).ToList();

        // Polazak u zadnjem retku ćelije spaja se s polaskom s istim slovom na istom mjestu u idućem satu.
        bool JoinsNext(int column, int row, int index)
        {
            var times = grid[column][row];
            if (row + 1 >= hours.Count || times[index].Mark is not { } mark || Lines(column, row) != rowLines[row]
                || index / perLine[column] != Lines(column, row) - 1)
            {
                return false;
            }
            var below = index % perLine[column];
            var next = grid[column][row + 1];
            return below < next.Count && next[below].Mark == mark;
        }

        bool JoinsPrevious(int column, int row, int index)
        {
            if (row == 0 || index >= perLine[column])
            {
                return false;
            }
            var above = grid[column][row - 1];
            var last = (Lines(column, row - 1) - 1) * perLine[column] + index;
            return last < above.Count && JoinsNext(column, row - 1, last);
        }

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(HourWidth);
                foreach (var weight in weights)
                {
                    columns.RelativeColumn(weight);
                }
            });

            table.Header(header =>
            {
                header.Cell().Element(HourDivider).Element(HeaderCell).Text(texts.Hour);
                foreach (var label in labels)
                {
                    header.Cell().Element(HeaderCell).Text(label);
                }
            });

            for (var row = 0; row < hours.Count; row++)
            {
                // Vožnje iza ponoći (24:10) ispisuju se kao 0 h, iza 23 h.
                table.Cell().Element(HourDivider).Element(Cell).PaddingVertical(2).AlignRight().Text((hours[row] % 24).ToString()).Bold();
                for (var column = 0; column < section.Columns.Count; column++)
                {
                    var (c, r) = (column, row);
                    table.Cell().Element(Cell).Inlined(inlined =>
                    {
                        for (var index = 0; index < grid[c][r].Count; index++)
                        {
                            Slot(inlined.Item(), grid[c][r][index], JoinsPrevious(c, r, index), JoinsNext(c, r, index));
                        }
                    });
                }
            }
        });

        static IContainer HeaderCell(IContainer cell) => cell
            .Background(HeaderBackground)
            .BorderBottom(0.75f)
            .BorderColor(Line)
            .PaddingVertical(3)
            .PaddingHorizontal(CellPadding)
            .DefaultTextStyle(style => style.SemiBold());

        // Crta odvaja sate od minuta.
        static IContainer HourDivider(IContainer cell) => cell
            .BorderRight(0.75f)
            .BorderColor(Line);

        // Bez okomitog razmaka: okvir spojenih polazaka mora doći do ruba ćelije.
        static IContainer Cell(IContainer cell) => cell
            .BorderBottom(0.5f)
            .BorderColor(Line)
            .PaddingHorizontal(CellPadding);
    }

    /// <param name="joinsPrevious">Okvir se nastavlja iz sata iznad, pa nema gornji rub.</param>
    /// <param name="joinsNext">Okvir se nastavlja u sat ispod, pa nema donji rub.</param>
    private static void Slot(IContainer container, TimetableTime time, bool joinsPrevious, bool joinsNext)
    {
        var minutes = (time.Seconds / 60 % 60).ToString("00");
        var slot = container.Width(SlotWidth);

        if (time.Mark is null)
        {
            slot.PaddingVertical(2).PaddingLeft(2).Text(minutes);
            return;
        }

        // Razmak iznad i ispod teksta je 2 pt, kao kod polaska bez okvira.
        var box = slot
            .PaddingTop(joinsPrevious ? 0 : 0.5f)
            .PaddingBottom(joinsNext ? 0 : 0.5f)
            .PaddingRight(1)
            .BorderLeft(MarkBorderWidth)
            .BorderRight(MarkBorderWidth)
            .BorderTop(joinsPrevious ? 0 : MarkBorderWidth)
            .BorderBottom(joinsNext ? 0 : MarkBorderWidth)
            .BorderColor(MarkBorder);

        if (!joinsPrevious)
        {
            box = box.CornerRadiusTopLeft(MarkRadius).CornerRadiusTopRight(MarkRadius);
        }
        if (!joinsNext)
        {
            box = box.CornerRadiusBottomLeft(MarkRadius).CornerRadiusBottomRight(MarkRadius);
        }

        box
            .PaddingTop(joinsPrevious ? 2 : 1.5f)
            .PaddingBottom(joinsNext ? 2 : 1.5f)
            .PaddingLeft(1)
            .Text(text =>
            {
                text.Span(minutes);
                text.Span(time.Mark).Superscript();
            });
    }

    /// <returns>Napomene i slovo polazaka na koje se odnose (null za napomene cijelog odjeljka).</returns>
    private static List<(string? Mark, string Text)> Notes(TimetableSection section, IReadOnlyList<string> labels, TimetableTexts texts, bool withYear)
    {
        var notes = section.Footnotes
            .Select(f => ((string?)f.Mark, f.RunsOnly
                ? texts.OnlyOn(f.Mark, texts.Dates(f.Description, withYear), f.Description.Named.Count > 0)
                : texts.NotOn(f.Mark, texts.Dates(f.Description, withYear), f.Description.Named.Count > 0)))
            .ToList();

        notes.AddRange(section.RunsAs.Select(r => ((string?)null, texts.RunsAs(texts.Dates(r.Description, withYear), labels[r.ColumnIndex]))));

        if (!section.NoService.IsEmpty)
        {
            notes.Add((null, texts.NoService(texts.Dates(section.NoService, withYear), section.NoService.Named.Count > 0)));
        }

        return notes;
    }
}
