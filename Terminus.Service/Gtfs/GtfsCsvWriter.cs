using System.IO.Compression;
using System.Text;

namespace Terminus.Service.Gtfs;

/// <summary>Zapisuje GTFS CSV datoteke u ZIP: UTF-8 bez BOM-a, CRLF, navodnici po RFC 4180.</summary>
internal static class GtfsCsvWriter
{
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    public static void WriteEntry(
        ZipArchive archive,
        string fileName,
        IReadOnlyList<string> header,
        IEnumerable<IReadOnlyList<string?>> rows)
    {
        var entry = archive.CreateEntry(fileName, CompressionLevel.Optimal);

        using var writer = new StreamWriter(entry.Open(), Utf8WithoutBom) { NewLine = "\r\n" };

        WriteLine(writer, header);
        foreach (var row in rows)
        {
            WriteLine(writer, row);
        }
    }

    public static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.AsSpan().IndexOfAny(",\"\r\n") >= 0
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;
    }

    private static void WriteLine(TextWriter writer, IReadOnlyList<string?> values)
    {
        for (var i = 0; i < values.Count; i++)
        {
            if (i > 0)
            {
                writer.Write(',');
            }

            writer.Write(Escape(values[i]));
        }

        writer.WriteLine();
    }
}
