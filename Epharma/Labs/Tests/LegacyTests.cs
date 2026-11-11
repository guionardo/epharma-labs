namespace Epharma.Labs.Tests;

using System.Text;
using Epharma.Labs.FWFile;
using Xunit;

public class LegacyTestsModel
{
    [FWField(6, Decimals = 2)]
    public decimal Amount;

    [FWField(4, Start = 6)]
    public int Code;
}

public class LegacyTests
{
    [Fact]
    public void ImplicitDecimalAndStart()
    {
        var f = new FWFile<LegacyTestsModel>();
        var row = "001234  42";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(row));
        var r = f.ParseStream(stream).First();
        Assert.Equal(12.34m, r.Amount);
        Assert.Equal(42, r.Code);
    }

    [Fact]
    public void WriteZeroPadsNumbers()
    {
        var f = new FWFile<LegacyTestsModel>();
        var original = new LegacyTestsModel { Amount = 5.5m, Code = 7 };
        using var ms = new MemoryStream();
        f.Write(ms, new[] { original });
        ms.Position = 0;
        var text = new StreamReader(ms).ReadToEnd().TrimEnd('\r', '\n');
        Assert.StartsWith("000550", text);
    }
}