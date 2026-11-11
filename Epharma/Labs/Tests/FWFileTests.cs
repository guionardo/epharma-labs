namespace Epharma.Labs.Tests;

using System.Linq;
using System.Text;
using Epharma.Labs.FWFile;
using Xunit.Abstractions;

public class RoundTripModel
{
    [FWField(4)]
    public int Id;

    [FWField(10)]
    public decimal Price;

    [FWField(10, dateTimeFormat: "yyyy-MM-dd")]
    public DateTime Creation;

    [FWField(8)]
    public TimeOnly StartTime;

    [FWField(10)]
    public DateOnly BirthDay;

    [FWField(5)]
    public bool Enabled;

    public string? Name { get; set; }
}

public class FWFileTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void ParseTimeOnly()
    {
        var f = new FWFile<RoundTripModel>();
        // Build a properly sized row manually
        var row = "   1" + "     10.00" + "1977-02-05" + "01:02:03" + "2001-01-01" + "S    ";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(row));
        var r = f.ParseStream(stream).First();
        Assert.Equal(new TimeOnly(1, 2, 3), r.StartTime);
        Assert.Equal(new DateOnly(2001, 1, 1), r.BirthDay);
        Assert.True(r.Enabled);
    }

    [Fact]
    public void WriteRoundTrip()
    {
        var f = new FWFile<RoundTripModel>();
        var original = new RoundTripModel
        {
            Id = 42,
            Price = 99.5m,
            Creation = new DateTime(2024, 6, 15),
            StartTime = new TimeOnly(12, 30, 0),
            BirthDay = new DateOnly(1990, 1, 1),
            Enabled = true,
            Name = "Alice"
        };
        // Name field not annotated? It is property without FWField -> not serialized. Remove expectation.
        original.Name = null;

        using var ms = new MemoryStream();
        f.Write(ms, new[] { original }, writeHeader: false);
        ms.Position = 0;
        var text = new StreamReader(ms).ReadToEnd();
        _output.WriteLine(text);

        ms.Position = 0;
        var parsed = f.ParseStream(ms).First();
        Assert.Equal(original.Id, parsed.Id);
        Assert.Equal(original.Price, parsed.Price);
        Assert.Equal(original.Creation, parsed.Creation);
        Assert.Equal(original.StartTime, parsed.StartTime);
        Assert.Equal(original.BirthDay, parsed.BirthDay);
        Assert.Equal(original.Enabled, parsed.Enabled);
    }

    [Fact]
    public async Task ParseAsync()
    {
        var f = new FWFile<RoundTripModel>();
        var row = "   1" + "     10.00" + "1977-02-05" + "01:02:03" + "2001-01-01" + "S    ";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(row));
        var list = new List<RoundTripModel>();
        await foreach (var r in f.ParseStreamAsync(stream))
        {
            list.Add(r);
        }
        Assert.Single(list);
    }

}