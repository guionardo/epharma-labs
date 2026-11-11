namespace Epharma.Labs.Tests;

using System.Text;
using Epharma.Labs.FWFile;
using Xunit;

public class MultiHeader { [FWField(1)] public string Type = ""; [FWField(5)] public int Count; }
public class MultiDetail { [FWField(1)] public string Type = ""; [FWField(4)] public int Id; [FWField(10)] public string Name = ""; }
public class MultiTrailer { [FWField(1)] public string Type = ""; [FWField(5)] public int Total; }

public class MultiFileTests
{
    private static FWMultiFile CreateMulti() =>
        new FWMultiFile()
            .Record<MultiHeader>("0")
            .Record<MultiDetail>("1")
            .Record<MultiTrailer>("9");

    [Fact]
    public void ParseReturnsTypedRecordsInOrder()
    {
        var multi = CreateMulti();
        var rows = "0    2\n1   1Alice     \n1   2Bob       \n9    2\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rows));
        var records = multi.ParseStream(stream).ToList();
        Assert.Equal(4, records.Count);
        Assert.IsType<MultiHeader>(records[0]);
        Assert.IsType<MultiDetail>(records[1]);
        Assert.IsType<MultiDetail>(records[2]);
        Assert.IsType<MultiTrailer>(records[3]);
    }

    [Fact]
    public void WriteRoundTrip()
    {
        var multi = CreateMulti();
        var rows = "0    2\n1   1Alice     \n9    2\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rows));
        var records = multi.ParseStream(stream).ToList();
        using var ms = new MemoryStream();
        multi.Write(ms, records);
        ms.Position = 0;
        var parsed = multi.ParseStream(ms).ToList();
        Assert.Equal(records.Count, parsed.Count);
    }

    [Fact]
    public void TrailerValidatorIsInvoked()
    {
        var called = false;
        var multi = CreateMulti().TrailerValidator(_ => called = true);
        var rows = "0    2\n9    2\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rows));
        _ = multi.ParseStream(stream).ToList();
        Assert.True(called);
    }

    [Fact]
    public void UnknownDiscriminatorThrows()
    {
        var multi = CreateMulti();
        var rows = "X    2\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rows));
        Assert.Throws<FWParseException>(() => multi.ParseStream(stream).ToList());
    }
}