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

    // Ostale varijante: tanke crte lijevo od glavne, sa zaobljenim spojem na stanicu gdje počinju ili završavaju.
    private const float LaneSpacing = 6;
    private const float LaneStroke = 2;
    private const float LaneCorner = 3;

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
        var lanes = new Lanes(diagram, line);

        return Document.Create(document => document.Page(page =>
        {
            page.Size(pageSize);
            page.Margin(margin);
            page.DefaultTextStyle(style => style.FontSize(8.5f).FontFamily("Lato").FontColor(Colors.Black));

            page.Content().Column(content =>
            {
                content.Item().Element(c => Header(c, diagram, texts, displayName, primary, logo));
                content.Item().PaddingTop(4).PaddingBottom(8).Height(3).Background(primary);
                content.Item().PaddingBottom(10).Element(c => Legend(c, diagram, lanes, texts, line));

                // MultiColumn traži barem dva stupca.
                if (columnCount == 1)
                {
                    content.Item().Element(c => StopRows(c, diagram, lanes, line, rowHeight));
                }
                else
                {
                    content.Item().MultiColumn(columns =>
                    {
                        columns.Columns(columnCount);
                        columns.Spacing(columnSpacing);
                        columns.BalanceHeight();
                        columns.Content().Element(c => StopRows(c, diagram, lanes, line, rowHeight));
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
    private static void Legend(IContainer container, RouteDiagram diagram, Lanes lanes, RouteDiagramTexts texts, string line)
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

            if (diagram.Backward is not null || lanes.Count > 0)
            {
                row.AutoItem().Column(column =>
                {
                    column.Spacing(2);
                    if (diagram.Backward is not null)
                    {
                        column.Item().Element(c => LegendItem(c, SampleSvg(line, shared: true), texts.BothDirections));
                        column.Item().Element(c => LegendItem(c, SampleSvg(line, shared: false), texts.OneDirection));
                    }
                    if (lanes.Count > 0)
                    {
                        column.Item().Element(c => LegendItem(c, VariantSampleSvg(lanes), texts.VariantLane));
                    }
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

    private static void StopRows(IContainer container, RouteDiagram diagram, Lanes lanes, string line, float rowHeight)
    {
        container.Column(rows =>
        {
            for (var i = 0; i < diagram.Stops.Count; i++)
            {
                var index = i;
                rows.Item().Height(rowHeight).Element(c => StopRow(c, diagram, lanes, index, line, rowHeight));
            }
        });
    }

    private static void StopRow(IContainer container, RouteDiagram diagram, Lanes lanes, int index, string line, float rowHeight)
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

            row.ConstantItem(lanes.Width + GraphicWidth).Height(rowHeight)
                .Svg(RowSvg(diagram, lanes, index, line, rowHeight)).FitArea();

            // Uz naziv stanice oznake varijanti koje tu počinju ili završavaju.
            var tags = lanes.Tags(index);
            row.RelativeItem().AlignMiddle().PaddingLeft(4).Text(text =>
            {
                text.ClampLines(1);
                var name = text.Span(stop.Name);
                if (stop.Terminus)
                {
                    name.FontSize(10).Bold();
                }
                else if (stop.VariantTerminus)
                {
                    name.FontSize(9).Bold();
                }
                else
                {
                    name.FontSize(9);
                }

                foreach (var tag in tags)
                {
                    // Sredina oznake je inače malo iznad sredine slova naziva.
                    text.Element(TextInjectedElementAlignment.Middle).TranslateY(1).PaddingLeft(5)
                        .Background(lanes.TagColor).CornerRadius(3).PaddingHorizontal(4).PaddingVertical(0.5f)
                        .Text(tag).FontSize(7).SemiBold().FontColor(Ink);
                }
            });
        });
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
    private static string RowSvg(RouteDiagram diagram, Lanes lanes, int index, string line, float rowHeight)
    {
        var stops = diagram.Stops;
        var stop = stops[index];
        var twoWay = diagram.Backward is not null;
        var mid = rowHeight / 2;
        var svg = new SvgBuilder(lanes.Width + GraphicWidth, rowHeight);
        var center = lanes.Width + Center;

        bool Shared(int i) => stops[i].Forward && stops[i].Backward;
        float X(int i, bool forward) => !twoWay
            ? center
            : Shared(i) ? center + (forward ? -SharedOffset : SharedOffset) : center + (forward ? -SplitOffset : SplitOffset);

        var radius = stop.Terminus ? 4.2f : 3f;
        var stopLeft = twoWay && Shared(index) ? center - SharedOffset - radius : X(index, stop.Forward) - radius;

        // Ostale varijante, ispod glavne crte i kružića.
        foreach (var (_, range, lane) in lanes.Items)
        {
            if (index < range.Top || index > range.Bottom)
            {
                continue;
            }

            var x = lanes.X(lane);
            var startsHere = index == range.Top;
            var endsHere = index == range.Bottom;
            if (startsHere && endsHere)
            {
                svg.Line(x, mid, stopLeft, mid, lanes.Color, LaneStroke);
                continue;
            }

            // Kraj na stanici: zaobljeni spoj u kružić. Kraj izvan sheme: crta se nastavlja isprekidano.
            if (startsHere && range.TopClosed)
            {
                svg.Path(string.Create(CultureInfo.InvariantCulture,
                    $"M{x},{rowHeight} V{mid + LaneCorner} Q{x},{mid} {x + LaneCorner},{mid} H{stopLeft}"), lanes.Color, LaneStroke);
            }
            else if (startsHere)
            {
                svg.Line(x, 0, x, mid, lanes.Color, LaneStroke, dashed: true);
                svg.Line(x, mid, x, rowHeight, lanes.Color, LaneStroke);
                svg.Line(x, mid, stopLeft, mid, lanes.Color, LaneStroke);
            }
            else if (endsHere && range.BottomClosed)
            {
                svg.Path(string.Create(CultureInfo.InvariantCulture,
                    $"M{x},0 V{mid - LaneCorner} Q{x},{mid} {x + LaneCorner},{mid} H{stopLeft}"), lanes.Color, LaneStroke);
            }
            else if (endsHere)
            {
                svg.Line(x, 0, x, mid, lanes.Color, LaneStroke);
                svg.Line(x, mid, x, rowHeight, lanes.Color, LaneStroke, dashed: true);
                svg.Line(x, mid, stopLeft, mid, lanes.Color, LaneStroke);
            }
            else
            {
                svg.Line(x, 0, x, rowHeight, lanes.Color, LaneStroke);
            }
        }

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
                    if (forward) svg.Line(center, 0, center, mid, line, 2 * SharedStroke);
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
                    if (forward) svg.Line(center, mid, center, rowHeight, line, 2 * SharedStroke);
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
        var width = stop.Terminus || stop.VariantTerminus ? 1.8f : 1.2f;
        if (twoWay && Shared(index))
        {
            svg.Pill(center, mid, SharedOffset + radius, radius, width);
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

    /// <summary>Uzorak za legendu: tanka crta varijante zaobljeno ulazi u kružić stanice.</summary>
    private static string VariantSampleSvg(Lanes lanes)
    {
        var svg = new SvgBuilder(18, 10);
        svg.Path("M2,10 V7 Q2,4 5,4 H12", lanes.Color, LaneStroke);
        svg.Pill(13, 4, 3, 3, 1.2f);
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

    /// <summary>
    /// Ostale varijante kao tanke crte uz glavnu, lijevo od nje: svaka u svojoj
    /// traci, a varijante koje se ne preklapaju dijele traku.
    /// </summary>
    private sealed class Lanes
    {
        private readonly List<(RouteDiagramVariant Variant, RouteDiagramRange Range, int Lane)> _items = [];
        private readonly HashSet<string> _stopNames;

        public Lanes(RouteDiagram diagram, string line)
        {
            _stopNames = diagram.Stops.Select(s => s.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            Color = Blend(line, 0.55);
            TagColor = Blend(line, 0.16);

            // Varijanta dobiva prvu traku u kojoj se ne preklapa s drugom; ranije i duže idu
            // prve, pa je najbliža glavnoj crti najdulja varijanta.
            var bottoms = new List<int>();
            foreach (var variant in diagram.OtherVariants
                .Where(v => v.Range is not null)
                .OrderBy(v => v.Range!.Top)
                .ThenByDescending(v => v.Range!.Bottom))
            {
                var range = variant.Range!;
                var lane = bottoms.FindIndex(bottom => bottom < range.Top);
                if (lane < 0)
                {
                    lane = bottoms.Count;
                    bottoms.Add(range.Bottom);
                }
                else
                {
                    bottoms[lane] = range.Bottom;
                }
                _items.Add((variant, range, lane));
            }
            Count = bottoms.Count;
        }

        public IReadOnlyList<(RouteDiagramVariant Variant, RouteDiagramRange Range, int Lane)> Items => _items;
        public int Count { get; }
        public string Color { get; }
        public string TagColor { get; }

        /// <summary>Širina traka lijevo od glavne crte.</summary>
        public float Width => Count == 0 ? 0 : Count * LaneSpacing + 2;

        /// <summary>Vodoravni položaj trake; traka 0 je najbliža glavnoj crti.</summary>
        public float X(int lane) => Width - 1 - lane * LaneSpacing;

        /// <summary>
        /// Oznake varijanti koje u retku počinju ili završavaju. Kraj izvan sheme
        /// (npr. garaža) piše se u zagradi uz oznaku na zadnjoj stanici sheme.
        /// </summary>
        public IReadOnlyList<string> Tags(int index) => _items
            .Where(i => i.Range.Top == index || i.Range.Bottom == index)
            .Select(i =>
            {
                var open = i.Range.Top == index && !i.Range.TopClosed || i.Range.Bottom == index && !i.Range.BottomClosed;
                var outside = new[] { i.Variant.From, i.Variant.To }.FirstOrDefault(n => !_stopNames.Contains(n));
                return open && outside is not null ? $"{i.Variant.Name} ({outside})" : i.Variant.Name;
            })
            .ToList();

        /// <summary>Boja pomiješana s bijelom; <paramref name="amount"/> je udio boje.</summary>
        private static string Blend(string color, double amount)
        {
            var rgb = int.Parse(color.TrimStart('#'), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            int Mix(int shift) => (int)Math.Round(((rgb >> shift) & 0xFF) * amount + 255 * (1 - amount));
            return $"#{Mix(16):X2}{Mix(8):X2}{Mix(0):X2}";
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
        private const string DashArray = " stroke-dasharray=\"2 1.5\"";

        private readonly StringBuilder _content = new();

        public void Line(float x1, float y1, float x2, float y2, string color, float stroke, bool dashed = false) =>
            _content.Append(CultureInfo.InvariantCulture,
                $"""<line x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}" stroke="{color}" stroke-width="{stroke}" stroke-linecap="butt"{(dashed ? DashArray : "")}/>""");

        public void Path(string data, string color, float stroke) =>
            _content.Append(CultureInfo.InvariantCulture,
                $"""<path d="{data}" fill="none" stroke="{color}" stroke-width="{stroke}"/>""");

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
