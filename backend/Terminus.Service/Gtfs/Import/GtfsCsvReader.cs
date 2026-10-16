using System.Text;

namespace Terminus.Service.Gtfs.Import;

/// <summary>
/// Čita GTFS CSV (RFC 4180): polja u navodnicima smiju sadržavati zarez, navodnik
/// ("") i prijelom retka; retci završavaju s CRLF, LF ili CR. UTF-8 BOM se
/// preskače. Vrijednosti se vraćaju bez okolnih razmaka.
/// </summary>
internal sealed class GtfsCsvReader(TextReader reader)
{
    private readonly StringBuilder _field = new();
    private int _line = 1;

    /// <summary>Redak u datoteci na kojem počinje zadnji pročitani zapis (zaglavlje je 1).</summary>
    public int RecordLine { get; private set; }

    /// <returns>Polja sljedećeg zapisa ili null na kraju datoteke. Prazni retci se preskaču.</returns>
    public string[]? ReadRecord()
    {
        while (true)
        {
            RecordLine = _line;
            var record = ReadRawRecord();
            if (record is null)
            {
                return null;
            }

            if (record.Count == 1 && record[0].Length == 0)
            {
                continue;
            }

            return record.ToArray();
        }
    }

    private List<string>? ReadRawRecord()
    {
        var c = reader.Read();
        if (c < 0)
        {
            return null;
        }

        var fields = new List<string>();
        _field.Clear();
        var inQuotes = false;

        while (true)
        {
            if (c < 0)
            {
                fields.Add(_field.ToString().Trim());
                return fields;
            }

            var ch = (char)c;

            if (inQuotes)
            {
                if (ch == '"')
                {
                    if (reader.Peek() == '"')
                    {
                        reader.Read();
                        _field.Append('"');
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    if (ch == '\n')
                    {
                        _line++;
                    }
                    _field.Append(ch);
                }
            }
            else if (ch == '"' && _field.ToString().Trim().Length == 0)
            {
                // Navodnik na početku polja (razmaci prije njega se zanemaruju).
                _field.Clear();
                inQuotes = true;
            }
            else if (ch == ',')
            {
                fields.Add(_field.ToString().Trim());
                _field.Clear();
            }
            else if (ch is '\r' or '\n')
            {
                if (ch == '\r' && reader.Peek() == '\n')
                {
                    reader.Read();
                }
                _line++;
                fields.Add(_field.ToString().Trim());
                return fields;
            }
            else
            {
                _field.Append(ch);
            }

            c = reader.Read();
        }
    }
}
