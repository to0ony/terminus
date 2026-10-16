using Terminus.Service.Gtfs.Import;

namespace Terminus.Tests.Service;

public class GtfsCsvReaderTests
{
    private static List<string[]> ReadAll(string text)
    {
        var reader = new GtfsCsvReader(new StringReader(text));
        var records = new List<string[]>();
        while (reader.ReadRecord() is { } record)
        {
            records.Add(record);
        }
        return records;
    }

    [Fact]
    public void ReadRecord_PlainFieldsAndLineEndings()
    {
        var records = ReadAll("a,b,c\r\n1,2,3\n4,5,6\r7,8,9");

        Assert.Equal([["a", "b", "c"], ["1", "2", "3"], ["4", "5", "6"], ["7", "8", "9"]], records);
    }

    [Fact]
    public void ReadRecord_QuotedFieldsWithCommaQuoteAndNewline()
    {
        var records = ReadAll("name,desc\r\n\"Osijek, AK\",\"Kaže \"\"peron 3\"\"\nkod ulaza\"\r\n");

        Assert.Equal(["Osijek, AK", "Kaže \"peron 3\"\nkod ulaza"], records[1]);
    }

    [Fact]
    public void ReadRecord_TrimsSpacesAndKeepsEmptyFields()
    {
        var records = ReadAll(" a , b ,\r\n 1 ,,\" 3\"");

        Assert.Equal(["a", "b", ""], records[0]);
        Assert.Equal(["1", "", "3"], records[1]);
    }

    [Fact]
    public void ReadRecord_SkipsEmptyLines_AndReportsLineNumbers()
    {
        var reader = new GtfsCsvReader(new StringReader("h\r\n\r\nx\r\n\"multi\nline\"\r\ny\r\n"));

        reader.ReadRecord();
        Assert.Equal(["x"], reader.ReadRecord()!);
        Assert.Equal(3, reader.RecordLine);
        Assert.Equal(["multi\nline"], reader.ReadRecord()!);
        Assert.Equal(["y"], reader.ReadRecord()!);
        Assert.Equal(6, reader.RecordLine);
        Assert.Null(reader.ReadRecord());
    }
}
