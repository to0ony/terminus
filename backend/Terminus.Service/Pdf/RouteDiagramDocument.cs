using System.Globalization;
using System.Text;
using QuestPDF;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Terminus.Model.Common.Enums;
using Terminus.Service.Common;

namespace Terminus.Service.Pdf;

/// <summary>
/// Izgled PDF sheme linije (QuestPDF): stanice odozgo prema dolje, crta linije
/// lijevo od naziva. Gdje staju oba smjera, crta je jedna i deblja; gdje smjerovi
/// idu različitim stanicama, razdvaja se u dvije trake, svaka sa strelicom smjera.
/// </summary>
internal static class RouteDiagramDocument
{
    private const string DefaultPrimary = "1565C0";
    private const string DefaultLine = "37474F";
    private const string Border = "#C9CED6";
    private const string Muted = "#5B6573";
    private const string Ink = "#263238";

    /// <summary>
    /// Najmanja i najveća visina retka stanice. Svi reci su jednako visoki, da se
    /// crta može nacrtati po recima; kratka linija dobiva više mjesta.
    /// </summary>
    private const float MinRowHeight = 15;
    private const float MaxRowHeight = 24;

    /// <summary>Okvirna visina zaglavlja, legende i podnožja.</summary>
    private const float ChromeHeight = 190;

    private const float GraphicWidth = 30;
    private const float MinutesWidth = 24;

    // Zagrade ostalih varijanti desno od naziva: zasebna traka za varijante koje se preklapaju.
    private const float LaneWidth = 9;
    private const float TickLength = 5;
    private const float BracketStroke = 1.2f;
    private const float MinLabelWidth = 70;

    // Trake smjerova: zajedno se dodiruju (jedna debela crta), razdvojene su udaljene.
    private const float Center = GraphicWidth / 2;
    private const float SharedOffset = 1.6f;
    private const float SharedStroke = 3.2f;
    private const float SplitOffset = 7;
    private const float SplitStroke = 2.4f;

    static RouteDiagramDocument()
    {
        Settings.License = LicenseType.Community;
    }

    /// <param name="logo">Logo i je li SVG; null bez loga.</param>
    public static byte[] Generate(
        RouteDiagram diagram,
        RouteDiagramPdfOptions options,
        string displayName,
        string? primaryColor,
        (byte[] Content, bool IsSvg)? logo)
    {
        var texts = RouteDiagramTexts.For(options.Language);
        var timetableTexts = TimetableTexts.For(options.Language);
        var primary = $"#{primaryColor ?? DefaultPrimary}";
        var line = $"#{LineColor(diagram.Route.Color)}";
        var a3 = options.Paper == PaperSize.A3;
        var pageSize = a3 ? PageSizes.A3 : PageSizes.A4;
        var margin = (a3 ? 1.6f : 1.2f) * 72 / 2.54f;

        // Varijante koje ne prolaze nijednom stanicom sheme ostaju popis ispod nje.
        var offDiagram = diagram.OtherVariants.Where(v => v.Range is null).ToList();

        // Stupaca toliko da linija stane na jednu stranicu, najviše tri.
        var available = pageSize.Height - 2 * margin - ChromeHeight - (offDiagram.Count > 0 ? 30 + 11 * offDiagram.Count : 0);
        var capacity = (int)(available / MinRowHeight);
        var columnCount = Math.Clamp((diagram.Stops.Count + capacity - 1) / capacity, 1, 3);
        var perColumn = (diagram.Stops.Count + columnCount - 1) / columnCount;
        var rowHeight = Math.Clamp(MathF.Floor(available / perColumn), MinRowHeight, MaxRowHeight);

        var columnSpacing = a3 ? 24f : 16f;
        var columnWidth = (pageSize.Width - 2 * margin - columnSpacing * (columnCount - 1)) / columnCount;
        var brackets = Brackets.Of(diagram, columnWidth);

        return Document.Create(document => document.Page(page =>
        {
            page.Size(pageSize);
            page.Margin(margin);
            page.DefaultTextStyle(style => style.FontSize(8.5f).FontFamily("Lato").FontColor(Colors.Black));

            page.Content().Column(content =>
            {
                content.Item().Element(c => Header(c, diagram, texts, displayName, primary, logo));
                content.Item().PaddingTop(4).PaddingBottom(8).Height(3).Background(primary);
                content.Item().PaddingBottom(10).Element(c => Legend(c, diagram, texts, line));

                // MultiColumn traži barem dva stupca.
                if (columnCount == 1)
                {
                    content.Item().Element(c => StopRows(c, diagram, brackets, line, rowHeight));
                }
                else
                {
                    content.Item().MultiColumn(columns =>
                    {
                        columns.Columns(columnCount);
                        columns.Spacing(columnSpacing);
                        columns.BalanceHeight();
                        columns.Content().Element(c => StopRows(c, diagram, brackets, line, rowHeight));
                    });
                }

                if (offDiagram.Count > 0)
                {
                    content.Item().PaddingTop(14).Element(c => OtherVariants(c, offDiagram, texts));
                }
            });

            page.Footer().PaddingTop(6).Row(row =>
            {
                var note = diagram.Route.TimeMode == TimeMode.Approximate
                    ? $"{timetableTexts.Disclaimer} {texts.ApproximateTimes}"
                    : timetableTexts.Disclaimer;
                row.RelativeItem().Text($"{displayName} · {note}").FontSize(7).FontColor(Muted);
                row.AutoItem().Text(text =>
                {
                    text.DefaultTextStyle(style => style.FontSize(7).FontColor(Muted));
                    text.Span($"{timetableTexts.Page} ");
                    text.CurrentPageNumber();
                    text.Span("/");
                    text.TotalPages();
                });
            });
        })).GeneratePdf();
    }

    private static void Header(
        IContainer container,
        RouteDiagram diagram,
        RouteDiagramTexts texts,
        string displayName,
        string primary,
        (byte[] Content, bool IsSvg)? logo)
    {
        var ends = $"{diagram.Forward.From} – {diagram.Forward.Headsign}";

        container.Column(column =>
        {
            if (logo is { } image)
            {
                var box = column.Item().PaddingBottom(4).Height(28).AlignLeft();
                if (image.IsSvg)
                {
                    box.Svg(SvgImage.FromText(Encoding.UTF8.GetString(image.Content))).FitHeight();
                }
                else
                {
                    box.Image(image.Content).FitHeight();
                }
            }
            else
            {
                column.Item().Text(displayName).FontSize(10).SemiBold().FontColor(primary);
            }

            column.Item().Row(row =>
            {
                row.Spacing(8);
                row.AutoItem().AlignMiddle().Element(c => RouteBadge(c, diagram));
                // Naslov je naziv linije; krajevi glavne trase samo kad naziva nema.
                var title = string.IsNullOrWhiteSpace(diagram.Route.LongName) ? ends : diagram.Route.LongName.Trim();
                row.RelativeItem().AlignMiddle().Text(title).FontSize(20).Bold();
            });

            column.Item().PaddingTop(2).Text($"{ends} · {texts.StopCount(diagram.Stops.Count)}").FontColor(Muted);
        });
    }

    /// <summary>Značenje stupaca minuta i crta.</summary>
    private static void Legend(IContainer container, RouteDiagram diagram, RouteDiagramTexts texts, string line)
    {
        container.Row(row =>
        {
            row.Spacing(16);

            row.AutoItem().Column(column =>
            {
                column.Spacing(2);
                column.Item().Element(c => LegendItem(c, ArrowSvg(down: true), texts.MinutesFrom(diagram.Forward.From)));
                if (diagram.Backward is { } backward)
                {
                    column.Item().Element(c => LegendItem(c, ArrowSvg(down: false), texts.MinutesFrom(backward.From)));
                }
            });

            if (diagram.Backward is not null)
            {
                row.AutoItem().Column(column =>
                {
                    column.Spacing(2);
                    column.Item().Element(c => LegendItem(c, SampleSvg(line, shared: true), texts.BothDirections));
                    column.Item().Element(c => LegendItem(c, SampleSvg(line, shared: false), texts.OneDirection));
                });
            }
        });
    }

    private static void LegendItem(IContainer container, string svg, string label)
    {
        container.Row(row =>
        {
            row.Spacing(4);
            row.ConstantItem(18).Height(10).Svg(svg).FitArea();
            row.AutoItem().AlignMiddle().Text(label).FontSize(7.5f).FontColor(Muted);
        });
    }

    private static void StopRows(IContainer container, RouteDiagram diagram, Brackets brackets, string line, float rowHeight)
    {
        container.Column(rows =>
        {
            for (var i = 0; i < diagram.Stops.Count; i++)
            {
                var index = i;
                rows.Item().Height(rowHeight).Element(c => StopRow(c, diagram, brackets, index, line, rowHeight));
            }
        });
    }

    private static void StopRow(IContainer container, RouteDiagram diagram, Brackets brackets, int index, string line, float rowHeight)
    {
        var stop = diagram.Stops[index];
        var approximate = diagram.Route.TimeMode == TimeMode.Approximate && !stop.Terminus;

        container.Row(row =>
        {
            row.ConstantItem(MinutesWidth).AlignRight().AlignMiddle().PaddingRight(3)
                .Text(Minutes(stop.ForwardMinutes, approximate)).FontSize(8).FontColor(Muted);
            if (diagram.Backward is not null)
            {
                row.ConstantItem(MinutesWidth).AlignRight().AlignMiddle().PaddingRight(3)
                    .Text(Minutes(stop.BackwardMinutes, approximate)).FontSize(8).FontColor(Muted);
            }

            row.ConstantItem(GraphicWidth).Height(rowHeight).Svg(RowSvg(diagram, index, line, rowHeight)).FitArea();

            var nameCell = brackets.LaneCount == 0 ? row.RelativeItem() : row.ConstantItem(brackets.NameWidth);
            var name = nameCell.AlignMiddle().PaddingLeft(4).Text(stop.Name).ClampLines(1);
            if (stop.Terminus)
            {
                name.FontSize(10).Bold();
            }
            else if (stop.VariantTerminus)
            {
                // Početak ili kraj neke od ostalih varijanti (npr. skraćene).
                name.FontSize(9).Bold();
            }
            else
            {
                name.FontSize(9);
            }

            // Svaka traka: zagrada, a uz njezin gornji kraj naziv varijante.
            for (var lane = 0; lane < brackets.LaneCount; lane++)
            {
                row.ConstantItem(LaneWidth).Height(rowHeight).Svg(BracketSvg(brackets, lane, index, rowHeight)).FitArea();

                var label = brackets.Items.FirstOrDefault(b => b.Lane == lane && b.Range.Top == index).Variant?.Name;
                row.ConstantItem(brackets.LabelWidths[lane]).AlignMiddle().PaddingLeft(2)
                    .Text(label ?? "").FontSize(7.5f).Italic().FontColor(Ink).ClampLines(1);
            }
            if (brackets.LaneCount > 0)
            {
                row.RelativeItem();
            }
        });
    }

    /// <summary>
    /// Zagrada jedne trake u jednom retku, kao zatvoreni interval položen okomito:
    /// crta uz stanice kojima varijanta prolazi, s kvačicom prema nazivu gdje
    /// varijanta počinje ili završava. Bez kvačice crta ide do ruba retka, jer
    /// varijanta nastavlja do stanica izvan sheme.
    /// </summary>
    private static string BracketSvg(Brackets brackets, int lane, int index, float rowHeight)
    {
        var svg = new SvgBuilder(LaneWidth, rowHeight);
        var mid = rowHeight / 2;

        foreach (var (_, range, _) in brackets.Items.Where(b => b.Lane == lane))
        {
            if (index < range.Top || index > range.Bottom)
            {
                continue;
            }

            var x = TickLength + 1;
            var top = index > range.Top || !range.TopClosed ? 0 : mid;
            var bottom = index < range.Bottom || !range.BottomClosed ? rowHeight : mid;
            if (bottom > top)
            {
                svg.Line(x, top, x, bottom, Ink, BracketStroke);
            }
            if (index == range.Top && range.TopClosed || index == range.Bottom && range.BottomClosed)
            {
                svg.Line(x - TickLength, mid, x + BracketStroke / 2, mid, Ink, BracketStroke);
            }
        }

        return svg.ToString();
    }

    private static string Minutes(int? minutes, bool approximate) => minutes switch
    {
        null => "",
        _ when approximate => $"~{minutes}",
        _ => minutes.Value.ToString(CultureInfo.InvariantCulture)
    };

    /// <summary>
    /// Crta jednog retka: od sredine prema susjednim recima vodi pola spoja, pa
    /// se reci nastavljaju jedan na drugi. Spoj počinje i završava na prvoj i
    /// zadnjoj stanici svakog smjera.
    /// </summary>
    private static string RowSvg(RouteDiagram diagram, int index, string line, float rowHeight)
    {
        var stops = diagram.Stops;
        var stop = stops[index];
        var twoWay = diagram.Backward is not null;
        var mid = rowHeight / 2;
        var svg = new SvgBuilder(GraphicWidth, rowHeight);

        bool Shared(int i) => stops[i].Forward && stops[i].Backward;
        float X(int i, bool forward) => !twoWay
            ? Center
            : Shared(i) ? Center + (forward ? -SharedOffset : SharedOffset) : Center + (forward ? -SplitOffset : SplitOffset);

        foreach (var forward in twoWay ? new[] { true, false } : [true])
        {
            var (first, last) = Span(stops, forward);
            if (index < first || index > last)
            {
                continue;
            }

            // Zajednička dionica je jedna debela crta (dvije trake jedna uz drugu
            // ostavljale bi šav), pa je crta samo prvi smjer.
            if (index > first)
            {
                if (twoWay && Shared(index - 1) && Shared(index))
                {
                    if (forward) svg.Line(Center, 0, Center, mid, line, 2 * SharedStroke);
                }
                else
                {
                    svg.Line((X(index - 1, forward) + X(index, forward)) / 2, 0, X(index, forward), mid, line, twoWay ? SplitStroke : 2 * SharedStroke);
                }
            }
            if (index < last)
            {
                if (twoWay && Shared(index) && Shared(index + 1))
                {
                    if (forward) svg.Line(Center, mid, Center, rowHeight, line, 2 * SharedStroke);
                }
                else
                {
                    svg.Line(X(index, forward), mid, (X(index, forward) + X(index + 1, forward)) / 2, rowHeight, line, twoWay ? SplitStroke : 2 * SharedStroke);
                }
            }

            // Traka koja ovdje ne staje nosi strelicu smjera.
            var stopsHere = forward ? stop.Forward : stop.Backward;
            if (!stopsHere)
            {
                svg.Arrow(X(index, forward), mid, down: forward, line);
            }
        }

        // Kružić stanice; gdje staju oba smjera, jedan ovalni preko obje trake.
        var radius = stop.Terminus ? 4.2f : 3f;
        var width = stop.Terminus || stop.VariantTerminus ? 1.8f : 1.2f;
        if (twoWay && Shared(index))
        {
            svg.Pill(Center, mid, SharedOffset + radius, radius, width);
        }
        else
        {
            svg.Pill(X(index, stop.Forward), mid, radius, radius, width);
        }

        return svg.ToString();
    }

    /// <summary>Prvi i zadnji redak u kojem smjer staje.</summary>
    private static (int First, int Last) Span(IReadOnlyList<RouteDiagramStop> stops, bool forward)
    {
        var first = -1;
        var last = -1;
        for (var i = 0; i < stops.Count; i++)
        {
            if (forward ? stops[i].Forward : stops[i].Backward)
            {
                if (first < 0) first = i;
                last = i;
            }
        }
        return (first, last);
    }

    private static string ArrowSvg(bool down)
    {
        var svg = new SvgBuilder(18, 10);
        svg.Arrow(9, 5, down, Ink, scale: 1.4f);
        return svg.ToString();
    }

    /// <summary>Uzorak crte za legendu: zajednička dionica ili dvije trake.</summary>
    private static string SampleSvg(string line, bool shared)
    {
        var svg = new SvgBuilder(18, 10);
        if (shared)
        {
            svg.Line(0, 5, 18, 5, line, 2 * SharedStroke);
            svg.Pill(9, 5, 3, 3 + SharedOffset, 1.2f);
        }
        else
        {
            svg.Line(0, 5, 18, 5, line, SplitStroke);
            svg.Pill(6, 5, 3, 3, 1.2f);
            svg.Arrow(14, 5, down: true, line, horizontal: true);
        }
        return svg.ToString();
    }

    private static void OtherVariants(IContainer container, IReadOnlyList<RouteDiagramVariant> variants, RouteDiagramTexts texts)
    {
        container.Column(column =>
        {
            column.Spacing(1);
            column.Item().PaddingBottom(2).Text(texts.OtherVariants).FontSize(9).SemiBold();
            foreach (var variant in variants)
            {
                column.Item().Text(text =>
                {
                    text.DefaultTextStyle(style => style.FontSize(8).FontColor(Muted));
                    text.Span($"{variant.Name}: ").FontColor(Colors.Black);
                    text.Span($"{variant.From} – {variant.To} ({texts.StopCount(variant.StopCount)})");
                });
            }
        });
    }

    /// <summary>Zagrade ostalih varijanti: traka svake, širine naziva traka i širina stupca naziva stanica.</summary>
    private sealed record Brackets(
        IReadOnlyList<(RouteDiagramVariant Variant, RouteDiagramRange Range, int Lane)> Items,
        IReadOnlyList<float> LabelWidths,
        float NameWidth)
    {
        public int LaneCount => LabelWidths.Count;

        public static Brackets Of(RouteDiagram diagram, float columnWidth)
        {
            // Varijanta dobiva prvu traku u kojoj se ne preklapa s drugom; ranije i duže idu prve.
            var items = new List<(RouteDiagramVariant, RouteDiagramRange, int)>();
            var laneBottoms = new List<int>();
            foreach (var variant in diagram.OtherVariants
                .Where(v => v.Range is not null)
                .OrderBy(v => v.Range!.Top)
                .ThenByDescending(v => v.Range!.Bottom))
            {
                var range = variant.Range!;
                var lane = laneBottoms.FindIndex(bottom => bottom < range.Top);
                if (lane < 0)
                {
                    lane = laneBottoms.Count;
                    laneBottoms.Add(range.Bottom);
                }
                else
                {
                    laneBottoms[lane] = range.Bottom;
                }
                items.Add((variant, range, lane));
            }

            // Širine po broju slova (procjena): zagrade stoje odmah iza najduljeg
            // naziva stanice, a ne uz rub stranice. Kad ne stane, nazivi se skraćuju.
            var labelWidths = Enumerable.Range(0, laneBottoms.Count)
                .Select(lane => items.Where(i => i.Item3 == lane).Max(i => i.Item1.Name.Length * 4.2f) + 6)
                .ToList();
            var space = columnWidth - 2 * MinutesWidth - GraphicWidth - laneBottoms.Count * LaneWidth;
            var longest = diagram.Stops.Max(s => s.Name.Length * (s.Terminus ? 5.6f : 5f)) + 10;
            var nameWidth = Math.Max(Math.Min(longest, space - laneBottoms.Count * MinLabelWidth), 60);
            var labelSpace = space - nameWidth;
            var labelTotal = labelWidths.Sum();
            if (labelTotal > labelSpace)
            {
                labelWidths = labelWidths.Select(w => Math.Max(w * labelSpace / labelTotal, 1)).ToList();
            }
            return new Brackets(items, labelWidths, nameWidth);
        }
    }

    private static void RouteBadge(IContainer container, RouteDiagram diagram)
    {
        var route = diagram.Route;
        container
            .MinWidth(34)
            .Background($"#{route.Color ?? DefaultLine}")
            .Border(0.5f)
            .BorderColor(Border)
            .PaddingHorizontal(7)
            .PaddingVertical(3)
            .AlignCenter()
            .Text(route.ShortName ?? route.Code)
            .FontSize(16)
            .Bold()
            .FontColor($"#{route.TextColor ?? "FFFFFF"}");
    }

    /// <summary>Boja linije; preblijeda boja (npr. bijela) ne bi se vidjela na papiru.</summary>
    private static string LineColor(string? color)
    {
        if (color is not { Length: 6 }
            || !int.TryParse(color, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
        {
            return DefaultLine;
        }

        var luminance = (0.299 * (rgb >> 16 & 0xFF) + 0.587 * (rgb >> 8 & 0xFF) + 0.114 * (rgb & 0xFF)) / 255;
        return luminance > 0.85 ? DefaultLine : color;
    }

    /// <summary>Mali SVG crteži za jedan redak sheme i legendu.</summary>
    private sealed class SvgBuilder(float width, float height)
    {
        private readonly StringBuilder _content = new();

        public void Line(float x1, float y1, float x2, float y2, string color, float stroke) =>
            _content.Append(CultureInfo.InvariantCulture,
                $"""<line x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}" stroke="{color}" stroke-width="{stroke}" stroke-linecap="butt"/>""");

        /// <summary>Kružić ili ovalni kružić stanice (bijeli s tamnim rubom).</summary>
        public void Pill(float cx, float cy, float w, float h, float stroke) =>
            _content.Append(CultureInfo.InvariantCulture,
                $"""<rect x="{cx - w}" y="{cy - h}" width="{2 * w}" height="{2 * h}" rx="{Math.Min(w, h)}" fill="#FFFFFF" stroke="{Ink}" stroke-width="{stroke}"/>""");

        /// <summary>Trokutić smjera, malo širi od trake da se vidi preko nje.</summary>
        public void Arrow(float cx, float cy, bool down, string color, float scale = 1, bool horizontal = false)
        {
            var w = 3.2f * scale;
            var h = 2.6f * scale;
            var d = down ? 1 : -1;
            var points = horizontal
                ? new[] { (cx + d * h, cy), (cx - d * h, cy - w), (cx - d * h, cy + w) }
                : new[] { (cx, cy + d * h), (cx - w, cy - d * h), (cx + w, cy - d * h) };
            var text = string.Join(' ', points.Select(p => string.Create(CultureInfo.InvariantCulture, $"{p.Item1},{p.Item2}")));
            _content.Append(CultureInfo.InvariantCulture, $"""<polygon points="{text}" fill="{color}"/>""");
        }

        public override string ToString() => string.Create(CultureInfo.InvariantCulture,
            $"""<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}">{_content}</svg>""");
    }
}
