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
        // Napomene bez godine, osim kad razdoblje prelazi u novu godinu.
        var withYear = timetable.From.Year != timetable.To.Year;

        return Document.Create(document => document.Page(page =>
        {
            page.Size(a3 ? PageSizes.A3 : PageSizes.A4);
            page.Margin(a3 ? 1.6f : 1.2f, Unit.Centimetre);
            page.DefaultTextStyle(style => style.FontSize(8.5f).FontFamily("Lato").FontColor(Colors.Black));

            page.Content().Column(content =>
            {
                content.Item().Element(c => Header(c, timetable, texts, displayName, primary, logo, qrSvg));
                content.Item().PaddingTop(4).PaddingBottom(10).Height(3).Background(primary);

                // Odjeljci teku po stupcima stranice, kao u novinama.
                content.Item().MultiColumn(columns =>
                {
                    columns.Columns(a3 ? 3 : 2);
                    columns.Spacing(a3 ? 24 : 18);
                    columns.Content().Element(c => Sections(c, timetable, texts, withYear));
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

    private static void Sections(IContainer container, StopTimetable timetable, TimetableTexts texts, bool withYear)
    {
        container.Column(column =>
        {
            column.Spacing(14);
            foreach (var section in timetable.Sections)
            {
                // Naslov odjeljka ne ostaje sam na dnu stranice.
                column.Item().EnsureSpace(90).Element(c => Section(c, section, texts, withYear));
            }
        });
    }

    private static void Section(IContainer container, TimetableSection section, TimetableTexts texts, bool withYear)
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

            column.Item().Element(c => Grid(c, section, labels, texts));

            var notes = Notes(section, labels, texts, withYear);
            if (notes.Count > 0)
            {
                column.Item().PaddingTop(4).Column(list =>
                {
                    foreach (var note in notes)
                    {
                        list.Item().Text(note).FontSize(7.5f).FontColor(Muted);
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

    /// <summary>Rešetka sat | minute; svaki redak je jedan sat dana voznog reda.</summary>
    private static void Grid(IContainer container, TimetableSection section, IReadOnlyList<string> labels, TimetableTexts texts)
    {
        var hours = section.Columns
            .SelectMany(c => c.Times)
            .Select(t => t.Seconds / 3600)
            .Distinct()
            .Order()
            .ToList();

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(26);
                // Stupac s više polazaka u satu dobiva više mjesta, da se minute manje prelamaju.
                foreach (var timetableColumn in section.Columns)
                {
                    var busiest = timetableColumn.Times.CountBy(t => t.Seconds / 3600).Max(x => x.Value);
                    columns.RelativeColumn(Math.Max(3, busiest));
                }
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderCell).Text(texts.Hour);
                foreach (var label in labels)
                {
                    header.Cell().Element(HeaderCell).Text(label);
                }
            });

            foreach (var hour in hours)
            {
                // Vožnje iza ponoći (24:10) ispisuju se kao 0 h, iza 23 h.
                table.Cell().Element(Cell).AlignRight().Text((hour % 24).ToString()).Bold();
                foreach (var timetableColumn in section.Columns)
                {
                    table.Cell().Element(Cell).Text(text =>
                    {
                        foreach (var time in timetableColumn.Times.Where(t => t.Seconds / 3600 == hour))
                        {
                            text.Span((time.Seconds / 60 % 60).ToString("00"));
                            if (time.Mark is not null)
                            {
                                text.Span(time.Mark).Superscript();
                            }
                            text.Span("  ");
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
            .PaddingHorizontal(4)
            .DefaultTextStyle(style => style.SemiBold());

        static IContainer Cell(IContainer cell) => cell
            .BorderBottom(0.5f)
            .BorderColor(Line)
            .PaddingVertical(2)
            .PaddingHorizontal(4);
    }

    private static List<string> Notes(TimetableSection section, IReadOnlyList<string> labels, TimetableTexts texts, bool withYear)
    {
        var notes = section.Footnotes
            .Select(f => f.RunsOnly
                ? texts.OnlyOn(f.Mark, texts.Dates(f.Description, withYear), f.Description.Named.Count > 0)
                : texts.NotOn(f.Mark, texts.Dates(f.Description, withYear), f.Description.Named.Count > 0))
            .ToList();

        notes.AddRange(section.RunsAs.Select(r => texts.RunsAs(texts.Dates(r.Description, withYear), labels[r.ColumnIndex])));

        if (!section.NoService.IsEmpty)
        {
            notes.Add(texts.NoService(texts.Dates(section.NoService, withYear), section.NoService.Named.Count > 0));
        }

        return notes;
    }
}
