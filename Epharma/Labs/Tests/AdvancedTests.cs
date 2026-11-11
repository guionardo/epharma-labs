namespace Epharma.Labs.Tests;

using System.Globalization;
using System.Text;
using Epharma.Labs.FWFile;
using Xunit;

public class AdvancedModel
{
    [FWField(4)]
    public int Id;

    [FWField(10, culture: "pt-BR")]
    public decimal Price;

    [FWField(8)]
    public DayOfWeek Day; // enum

    [FWField(36, converterType: typeof(GuidConverter))]
    public Guid Token;
}

public class GuidConverter : IFWConverter
{
    public object? Read(ReadOnlySpan<char> data, Type targetType) => Guid.Parse(data.Trim().ToString());
    public string Write(object? value, Type targetType) => ((Guid)value!).ToString();
}

public class AdvancedTests
{
    [Fact]
    public void FluentMapping()
    {
        var f = new FWFile<FluentModel>()
            .Field(x => x.Id, 4)
            .Field(x => x.Name, 10);
        var row = "   1Alice     ";
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(row));
        var r = f.ParseStream(ms).First();
        Assert.Equal(1, r.Id);
        Assert.Equal("Alice", r.Name);
    }

    [Fact]
    public void AutoHeaderSkip()
    {
        var f = new FWFile<AdvancedModel>();
        var header = "Id  ".PadRight(4) + "Price     ".PadRight(10) + "Day     ".PadRight(8) + "Token".PadRight(36);
        var row = "   1" + "   1234,56" + "Monday  " + "12345678-1234-1234-1234-123456789abc";
        var text = header + "\n" + row + "\n";
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(text));
        var r = f.ParseStream(ms, autoSkipHeader: true).First();
        Assert.Equal(1, r.Id);
    }

    [Fact]
    public void CultureAndEnumAndConverter()
    {
        var row = "   1" + "   1234,56" + "Monday  " + "12345678-1234-1234-1234-123456789abc";
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(row));
        var f = new FWFile<AdvancedModel>();
        var r = f.ParseStream(ms).First();
        Assert.Equal(1, r.Id);
        Assert.Equal(1234.56m, r.Price);
        Assert.Equal(DayOfWeek.Monday, r.Day);
        Assert.Equal(Guid.Parse("12345678-1234-1234-1234-123456789abc"), r.Token);
    }
}

public class FluentModel
{
    public int Id;
    public string Name = default!;
}