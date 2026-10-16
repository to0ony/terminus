using System.IO.Compression;
using System.Text;

namespace Terminus.Service.Gtfs.Import;

/// <summary>
/// GTFS ZIP. Datoteke mogu biti u korijenu arhive ili u jednoj podmapi (čest
/// slučaj kad se mapa zapakira cijela). Ukupna nekomprimirana veličina je
/// ograničena, kao zaštita od "zip bombi".
/// </summary>
internal sealed class GtfsArchive : IDisposable
{
    public const long MaxUncompressedBytes = 2L * 1024 * 1024 * 1024;

    private readonly ZipArchive _zip;
    private readonly Dictionary<string, ZipArchiveEntry> _entries;

    private GtfsArchive(ZipArchive zip, Dictionary<string, ZipArchiveEntry> entries)
    {
        _zip = zip;
        _entries = entries;
    }

    /// <summary>Nazivi .txt (i ostalih) datoteka u GTFS mapi, malim slovima.</summary>
    public IReadOnlyCollection<string> FileNames => _entries.Keys;

    /// <returns>Arhiva ili null ako sadržaj nije ispravan ZIP; razlog je u <paramref name="error"/>.</returns>
    public static GtfsArchive? Open(Stream stream, out string? error)
    {
        ZipArchive zip;
        try
        {
            zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        }
        catch (InvalidDataException)
        {
            error = "Datoteka nije ispravna ZIP arhiva.";
            return null;
        }

        var files = zip.Entries.Where(e => !e.FullName.EndsWith('/') && e.Length >= 0).ToList();

        if (files.Sum(e => e.Length) > MaxUncompressedBytes)
        {
            zip.Dispose();
            error = $"Raspakirani feed je veći od {MaxUncompressedBytes / 1024 / 1024 / 1024} GB.";
            return null;
        }

        // Mapa u kojoj je agency.txt (ili stops.txt) je korijen feeda.
        var marker = files.FirstOrDefault(e => FileName(e) is "agency.txt" or "stops.txt");
        var root = marker is null ? string.Empty : marker.FullName[..^marker.Name.Length];

        var entries = new Dictionary<string, ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in files.Where(e => e.FullName.StartsWith(root, StringComparison.Ordinal)
                                              && e.FullName.IndexOf('/', root.Length) < 0))
        {
            entries.TryAdd(FileName(entry), entry);
        }

        error = null;
        return new GtfsArchive(zip, entries);
    }

    public bool Contains(string fileName) => _entries.ContainsKey(fileName);

    /// <returns>Tablica ili null ako datoteke nema.</returns>
    public GtfsTable? Open(string fileName)
    {
        if (!_entries.TryGetValue(fileName, out var entry))
        {
            return null;
        }

        var reader = new StreamReader(entry.Open(), new UTF8Encoding(false), detectEncodingFromByteOrderMarks: true);
        return new GtfsTable(fileName, reader);
    }

    public void Dispose() => _zip.Dispose();

    private static string FileName(ZipArchiveEntry entry) => entry.Name.ToLowerInvariant();
}

/// <summary>
/// Jedna GTFS datoteka. Zaglavlje se čita odmah, a retci jednom, redom
/// (datoteke poput stop_times.txt imaju milijune redaka).
/// </summary>
internal sealed class GtfsTable : IDisposable
{
    private readonly TextReader _reader;
    private readonly GtfsCsvReader _csv;
    private readonly Dictionary<string, int> _columns;

    public GtfsTable(string fileName, TextReader reader)
    {
        FileName = fileName;
        _reader = reader;
        _csv = new GtfsCsvReader(reader);

        var header = _csv.ReadRecord() ?? [];
        _columns = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < header.Length; i++)
        {
            _columns.TryAdd(header[i].Trim('﻿', ' ').ToLowerInvariant(), i);
        }
    }

    public string FileName { get; }

    public bool Has(string column) => _columns.ContainsKey(column);

    /// <returns>Obavezni stupci kojih nema u zaglavlju.</returns>
    public IReadOnlyList<string> Missing(params string[] columns) =>
        columns.Where(c => !_columns.ContainsKey(c)).ToList();

    public IEnumerable<GtfsRow> Rows()
    {
        while (_csv.ReadRecord() is { } values)
        {
            yield return new GtfsRow(this, values, _csv.RecordLine);
        }
    }

    internal string? Value(string[] values, string column) =>
        _columns.TryGetValue(column, out var index) && index < values.Length && values[index].Length > 0
            ? values[index]
            : null;

    public void Dispose() => _reader.Dispose();
}

/// <summary>Jedan redak; prazno polje i stupac koji ne postoji vraćaju null.</summary>
internal readonly struct GtfsRow(GtfsTable table, string[] values, int line)
{
    public int Line { get; } = line;

    public string? this[string column] => table.Value(values, column);
}
