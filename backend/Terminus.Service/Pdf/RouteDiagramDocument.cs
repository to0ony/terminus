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
/// Izgled PDF sheme linije (QuestPDF), položeno: stanice su točke na vodoravnoj
/// crti, a nazivi koso iznad nje. Duga linija se lomi u više crta koje se, kao
/// zmija, nastavljaju zaobljenim zaokretom: druga crta ide zdesna nalijevo, s
/// nazivima ispod, pa su dvije crte blizu i zaokret je mali.
/// </summary>
internal static class RouteDiagramDocument
{
    private const string DefaultPrimary = "1565C0";
    private const string DefaultLine = "37474F";
    private const string Border = "#C9CED6";
    private const string Muted = "#5B6573";
    private const string Ink = "#263238";

    // Nazivi stanica: koso pod kutom, najviše ove duljine (dulji se skraćuju).
    private const float LabelAngle = 55;
    private const float LabelLength = 125;
    private const float LabelHeight = 12;
    private const float LabelOffset = 9;

    /// <summary>Visina mjesta za nazive iznad ili ispod crte.</summary>
    private static readonly float LabelArea = MathF.Ceiling(Sin * LabelLength + LabelOffset + 6);

    /// <summary>Koliko nazivi zadnjih stanica crte izlaze desno od nje.</summary>
    private static readonly float LabelOverhang = MathF.Ceiling(Cos * LabelLength + Sin * LabelHeight / 2 + 2);

    private const float MinSpacing = 18;
    private const float MaxSpacing = 90;
    private const float LineStroke = 5;
    private const float MaxTurnRadius = 16;

    /// <summary>Lijevo od prve stanice mjesto za zaokret na lijevoj strani.</summary>
    private const float LeftPad = MaxTurnRadius + LineStroke;

    private const float DotPad = 9;
    private const float MinutesLine = 9;
    private const float RowGap = 12;

    private static float Sin => MathF.Sin(LabelAngle * MathF.PI / 180);
    private static float Cos => MathF.Cos(LabelAngle * MathF.PI / 180);

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
        var pageSize = (a3 ? PageSizes.A3 : PageSizes.A4).Landscape();
        var margin = (a3 ? 1.6f : 1.2f) * 72 / 2.54f;
        var layout = new Layout(diagram, pageSize.Width - 2 * margin, options.ShowMinutes);

        return Document.Create(document => document.Page(page =>
        {
            page.Size(pageSize);
            page.Margin(margin);
            page.DefaultTextStyle(style => style.FontSize(8.5f).FontFamily("Lato").FontColor(Colors.Black));

            page.Content().Column(content =>
            {
                content.Item().Element(c => Header(c, diagram, texts, displayName, primary, logo));
                content.Item().PaddingTop(4).PaddingBottom(8).Height(3).Background(primary);

                // Shema u sredini preostalog mjesta; preduga se smanji da stane na stranicu.
                content.Item().Extend().AlignMiddle().ScaleToFit()
                    .Element(c => Diagram(c, diagram, layout, line, options.ShowMinutes));
            });

            page.Footer().PaddingTop(6).Column(footer =>
            {
                footer.Item().PaddingBottom(8).Element(c => Legend(c, diagram, texts, line, options.ShowMinutes));
                footer.Item().Element(c => FooterNote(c, diagram, options, texts, timetableTexts, displayName));
            });
        })).GeneratePdf();
    }

    /// <summary>Shema je uvijek na jednoj stranici; napomena o vremenima samo uz minute.</summary>
    private static void FooterNote(
        IContainer container,
        RouteDiagram diagram,
        RouteDiagramPdfOptions options,
        RouteDiagramTexts texts,
        TimetableTexts timetableTexts,
        string displayName)
    {
        var note = !options.ShowMinutes
            ? displayName
            : diagram.Route.TimeMode == TimeMode.Approximate
                ? $"{displayName} · {timetableTexts.Disclaimer} {texts.ApproximateTimes}"
                : $"{displayName} · {timetableTexts.Disclaimer}";
        container.Text(note).FontSize(7).FontColor(Muted);
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

    /// <summary>Crta sa stanicama (jedan SVG) i nazivi i minute postavljeni preko njega.</summary>
    private static void Diagram(IContainer container, RouteDiagram diagram, Layout layout, string line, bool showMinutes)
    {
        container.Width(layout.Width).Height(layout.Height).Layers(layers =>
        {
            layers.PrimaryLayer().Svg(DiagramSvg(diagram, layout, line)).FitArea();

            for (var i = 0; i < diagram.Stops.Count; i++)
            {
                var stop = diagram.Stops[i];
                var (x, y, above) = layout.Position(i);
                var label = new StopLabel(stop.Name, stop.Terminus, stop.VariantTerminus);
                layers.Layer().Element(c => Label(c, label, x + 1, above ? y - LabelOffset : y + LabelOffset, above));

                if (showMinutes)
                {
                    var approximate = diagram.Route.TimeMode == TimeMode.Approximate && !stop.Terminus;
                    var top = above ? y + DotPad : y - DotPad - layout.MinutesArea;
                    layers.Layer().Element(c => MinutesText(c, Minutes(stop.ForwardMinutes, approximate), x, top, Ink));
                    if (diagram.Backward is not null)
                    {
                        layers.Layer().Element(c => MinutesText(c, Minutes(stop.BackwardMinutes, approximate), x, top + MinutesLine, Muted));
                    }
                }
            }
        });
    }

    private sealed record StopLabel(string Name, bool Terminus, bool VariantTerminus);

    /// <summary>
    /// Naziv stanice koso od točke (x, y): iznad crte prema gore desno, ispod nje
    /// prema dolje desno. Rotate okreće oko središta, pa se okvir naziva postavi
    /// tako da mu je sredina lijevog ruba u (x, y).
    /// </summary>
    private static void Label(IContainer container, StopLabel label, float x, float y, bool above)
    {
        var angle = above ? -LabelAngle : LabelAngle;
        var left = x + LabelLength / 2 * Cos - LabelLength / 2;
        var top = y + LabelLength / 2 * (above ? -Sin : Sin) - LabelHeight / 2;

        container.TranslateX(left).TranslateY(top).Unconstrained()
            .Rotate(angle).Width(LabelLength).Height(LabelHeight).AlignMiddle()
            .Text(text =>
            {
                text.ClampLines(1);
                var name = text.Span(label.Name).FontColor(Ink);
                if (label.Terminus)
                {
                    name.FontSize(9.5f).Bold();
                }
                else if (label.VariantTerminus)
                {
                    name.Bold();
                }
            });
    }

    private static void MinutesText(IContainer container, string minutes, float x, float top, string color) =>
        container.TranslateX(x - 15).TranslateY(top).Unconstrained().Width(30).AlignCenter()
            .Text(minutes).FontSize(7).FontColor(color);

    private static string Minutes(int? minutes, bool approximate) => minutes switch
    {
        null => "",
        _ when approximate => $"~{minutes}",
        _ => minutes.Value.ToString(CultureInfo.InvariantCulture)
    };

    /// <summary>Crta linije sa zaokretima između redaka i točke stanica.</summary>
    private static string DiagramSvg(RouteDiagram diagram, Layout layout, string line)
    {
        var svg = new SvgBuilder(layout.Width, layout.Height);
        var stops = diagram.Stops;

        var path = new StringBuilder();
        var (startX, startY, _) = layout.Position(0);
        path.Append(CultureInfo.InvariantCulture, $"M{startX},{startY}");
        for (var row = 0; row < layout.RowCount; row++)
        {
            var last = layout.LastOf(row);
            var (x1, y1, _) = layout.Position(last);
            path.Append(CultureInfo.InvariantCulture, $" H{x1}");
            if (row == layout.RowCount - 1)
            {
                break;
            }

            // Zaokret: desno na kraju parnih redaka, lijevo na kraju neparnih.
            var (x2, y2, _) = layout.Position(last + 1);
            var radius = Math.Min(MaxTurnRadius, (y2 - y1) / 2);
            var right = row % 2 == 0;
            var side = right ? x1 + radius : x1 - radius;
            var sweep = right ? 1 : 0;
            path.Append(CultureInfo.InvariantCulture,
                $" A{radius},{radius} 0 0 {sweep} {side},{y1 + radius} V{y2 - radius} A{radius},{radius} 0 0 {sweep} {x2},{y2}");
        }
        svg.Path(path.ToString(), line, LineStroke);

        var twoWay = diagram.Backward is not null;
        for (var i = 0; i < stops.Count; i++)
        {
            var stop = stops[i];
            var (x, y, above) = layout.Position(i);
            if (stop.Terminus)
            {
                svg.Circle(x, y, 5.2f, 2);
            }
            else if (stop.VariantTerminus)
            {
                svg.Circle(x, y, 4.6f, 1.8f);
            }
            else if (twoWay && stop.Forward != stop.Backward)
            {
                // Staje samo u jednom smjeru: strelica u točki. Parni redci idu
                // nadesno, neparni nalijevo; smjer 1 je obrnut.
                svg.Circle(x, y, 3.9f, 1.1f);
                var rightwards = above == stop.Forward;
                svg.Arrow(x, y, rightwards);
            }
            else
            {
                svg.Circle(x, y, 3.3f, 1.1f);
            }
        }

        return svg.ToString();
    }

    /// <summary>Značenje oznaka, u jednom retku ispod sheme.</summary>
    private static void Legend(IContainer container, RouteDiagram diagram, RouteDiagramTexts texts, string line, bool showMinutes)
    {
        var oneWay = diagram.Backward is not null && diagram.Stops.Any(s => !s.Terminus && !s.VariantTerminus && s.Forward != s.Backward);

        container.Row(row =>
        {
            row.Spacing(18);
            row.AutoItem().Element(c => LegendItem(c, SampleSvg(line, s => s.Circle(9, 5, 4.2f, 1.8f)), texts.Terminus));
            if (oneWay)
            {
                row.AutoItem().Element(c => LegendItem(c, SampleSvg(line, s =>
                {
                    s.Circle(9, 5, 3.9f, 1.1f);
                    s.Arrow(9, 5, rightwards: true);
                }), texts.OneDirection));
            }

            if (showMinutes)
            {
                row.AutoItem().Element(c => MinutesLegend(c, Ink, texts.MinutesFrom(diagram.Forward.From)));
                if (diagram.Backward is { } backward)
                {
                    row.AutoItem().Element(c => MinutesLegend(c, Muted, texts.MinutesFrom(backward.From)));
                }
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

    private static void MinutesLegend(IContainer container, string color, string label)
    {
        container.Row(row =>
        {
            row.Spacing(4);
            row.AutoItem().AlignMiddle().Text("12").FontSize(7).FontColor(color);
            row.AutoItem().AlignMiddle().Text(label).FontSize(7.5f).FontColor(Muted);
        });
    }

    private static string SampleSvg(string line, Action<SvgBuilder> dot)
    {
        var svg = new SvgBuilder(18, 10);
        svg.Path("M0,5 H18", line, LineStroke);
        dot(svg);
        return svg.ToString();
    }

    /// <summary>
    /// Raspored stanica po crtama: koliko ih stane u red uz najmanji razmak,
    /// ravnomjerno raspoređeno; parni redci s lijeva nadesno s nazivima iznad,
    /// neparni zdesna nalijevo s nazivima ispod.
    /// </summary>
    private sealed class Layout
    {
        private readonly int _count;
        private readonly int _perRow;
        private readonly float _spacing;
        private readonly float _offset;
        private readonly float[] _lineY;

        public Layout(RouteDiagram diagram, float width, bool showMinutes)
        {
            Width = width;
            _count = diagram.Stops.Count;
            MinutesArea = showMinutes ? (diagram.Backward is null ? 1 : 2) * MinutesLine : 0;

            var usable = width - LeftPad - LabelOverhang;
            var maxPerRow = (int)(usable / MinSpacing) + 1;
            RowCount = (_count + maxPerRow - 1) / maxPerRow;
            _perRow = (_count + RowCount - 1) / RowCount;
            _spacing = _perRow > 1 ? Math.Min(MaxSpacing, usable / (_perRow - 1)) : 0;
            // Kratka linija ne treba cijelu širinu, pa je u sredini.
            _offset = (usable - (_perRow - 1) * _spacing) / 2;

            // Mjesto ispod crte (iznad kod neparnih): točke i minute.
            var below = DotPad + MinutesArea;
            _lineY = new float[RowCount];
            var y = 0f;
            for (var row = 0; row < RowCount; row++)
            {
                if (row > 0)
                {
                    y += RowGap;
                }
                var above = row % 2 == 0;
                _lineY[row] = y + (above ? LabelArea : below);
                y = _lineY[row] + (above ? below : LabelArea);
            }
            Height = MathF.Ceiling(y);
        }

        public float Width { get; }
        public float Height { get; }
        public int RowCount { get; }
        public float MinutesArea { get; }

        /// <summary>Zadnja stanica reda.</summary>
        public int LastOf(int row) => Math.Min(_count, (row + 1) * _perRow) - 1;

        /// <summary>Točka stanice i jesu li nazivi njezina reda iznad crte.</summary>
        public (float X, float Y, bool Above) Position(int index)
        {
            var row = index / _perRow;
            var column = index % _perRow;
            var above = row % 2 == 0;
            var x = LeftPad + _offset + (above ? column : _perRow - 1 - column) * _spacing;
            return (x, _lineY[row], above);
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

    /// <summary>SVG crteži sheme i legende.</summary>
    private sealed class SvgBuilder(float width, float height)
    {
        private readonly StringBuilder _content = new();

        public void Path(string data, string color, float stroke) =>
            _content.Append(CultureInfo.InvariantCulture,
                $"""<path d="{data}" fill="none" stroke="{color}" stroke-width="{stroke}" stroke-linecap="round" stroke-linejoin="round"/>""");

        /// <summary>Točka stanice (bijela s tamnim rubom).</summary>
        public void Circle(float cx, float cy, float r, float stroke) =>
            _content.Append(CultureInfo.InvariantCulture,
                $"""<circle cx="{cx}" cy="{cy}" r="{r}" fill="#FFFFFF" stroke="{Ink}" stroke-width="{stroke}"/>""");

        /// <summary>Mali trokutić smjera u točki stanice.</summary>
        public void Arrow(float cx, float cy, bool rightwards)
        {
            var d = rightwards ? 1 : -1;
            var points = new[] { (cx + d * 1.9f, cy), (cx - d * 1.4f, cy - 2), (cx - d * 1.4f, cy + 2) };
            var text = string.Join(' ', points.Select(p => string.Create(CultureInfo.InvariantCulture, $"{p.Item1},{p.Item2}")));
            _content.Append(CultureInfo.InvariantCulture, $"""<polygon points="{text}" fill="{Ink}"/>""");
        }

        public override string ToString() => string.Create(CultureInfo.InvariantCulture,
            $"""<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}">{_content}</svg>""");
    }
}
