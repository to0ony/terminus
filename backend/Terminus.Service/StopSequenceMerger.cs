namespace Terminus.Service;

/// <summary>
/// Spaja redoslijede stanica više vožnji u jedan popis redaka za vozni red
/// linije (tablica vožnja × stanica). Vožnje mogu preskakati stanice, kretati ili
/// završavati usput i ići različitim varijantama trase; zajedničke stanice dijele
/// redak, a ostale se umeću na mjesto gdje ih vožnja obilazi. Stanica koju vožnja
/// obilazi dvaput (kružne linije) dobiva dva retka.
/// </summary>
internal static class StopSequenceMerger
{
    /// <summary>
    /// Najdulji redoslijed je osnova; ostali se redom spajaju preko najduljeg
    /// zajedničkog podniza. Između dvije zajedničke stanice prvo dolaze stanice
    /// osnove, zatim nove.
    /// </summary>
    /// <returns>Redci: Id-evi (ili drugi ključevi) stanica redom kojim se prikazuju.</returns>
    public static List<T> Merge<T>(IEnumerable<IReadOnlyList<T>> sequences) where T : notnull
    {
        var rows = new List<T>();

        foreach (var sequence in sequences.OrderByDescending(s => s.Count))
        {
            var matches = Match(rows, sequence);
            var merged = new List<T>(rows.Count + sequence.Count - matches.Count);
            int r = 0, s = 0;

            foreach (var (rowIndex, sequenceIndex) in matches.Append((rows.Count, sequence.Count)))
            {
                while (r < rowIndex) merged.Add(rows[r++]);
                while (s < sequenceIndex) merged.Add(sequence[s++]);

                if (rowIndex < rows.Count)
                {
                    merged.Add(rows[r++]);
                    s++;
                }
            }

            rows = merged;
        }

        return rows;
    }

    /// <summary>Za svaku stanicu redoslijeda indeks retka u <paramref name="rows"/>.</summary>
    /// <exception cref="ArgumentException">Redoslijed nije sadržan u redcima.</exception>
    public static int[] Map<T>(IReadOnlyList<T> rows, IReadOnlyList<T> sequence) where T : notnull
    {
        var matches = Match(rows, sequence);
        if (matches.Count != sequence.Count)
        {
            throw new ArgumentException("Redoslijed stanica nije sadržan u redcima.", nameof(sequence));
        }

        return matches.Select(m => m.RowIndex).ToArray();
    }

    /// <summary>Najdulji zajednički podniz (dinamičko programiranje), kao parovi indeksa.</summary>
    private static List<(int RowIndex, int SequenceIndex)> Match<T>(IReadOnlyList<T> rows, IReadOnlyList<T> sequence) where T : notnull
    {
        var comparer = EqualityComparer<T>.Default;
        var n = rows.Count;
        var m = sequence.Count;

        // lengths[i, j] = duljina najduljeg zajedničkog podniza rows[i..] i sequence[j..].
        var lengths = new int[n + 1, m + 1];
        for (var i = n - 1; i >= 0; i--)
        {
            for (var j = m - 1; j >= 0; j--)
            {
                lengths[i, j] = comparer.Equals(rows[i], sequence[j])
                    ? lengths[i + 1, j + 1] + 1
                    : Math.Max(lengths[i + 1, j], lengths[i, j + 1]);
            }
        }

        var matches = new List<(int, int)>(lengths[0, 0]);
        for (int i = 0, j = 0; i < n && j < m;)
        {
            if (comparer.Equals(rows[i], sequence[j]))
            {
                matches.Add((i++, j++));
            }
            else if (lengths[i + 1, j] >= lengths[i, j + 1])
            {
                i++;
            }
            else
            {
                j++;
            }
        }

        return matches;
    }
}
