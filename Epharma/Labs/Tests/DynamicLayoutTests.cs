namespace Epharma.Labs.Tests;

using System.Text;
using Epharma.Labs.FWFile;
using Xunit;

public class DynamicLayoutTests
{
    [Fact]
    public void FluentLayoutRoundTrip()
    {
        var layout = new FWLayout()
            .Field("Id", 4, FWFieldType.Int)
            .Field("Name", 10, FWFieldType.String);
        var file = new FWDynamicFile(layout);
        var row = "   1Alice     ";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(row));
        var rec = file.ParseStream(stream).First();
        Assert.Equal(1, rec["Id"]);
        Assert.Equal("Alice", ((string?)rec["Name"])?.Trim());

        using var ms = new MemoryStream();
        file.Write(ms, new[] { rec });
        ms.Position = 0;
        var text = new StreamReader(ms).ReadToEnd();
        Assert.Contains("Alice", text);
    }

    [Fact]
    public void JsonLayout()
    {
        var json = "[{\"Name\":\"Id\",\"Length\":4,\"Type\":\"Int\"},{\"Name\":\"Name\",\"Length\":5,\"Type\":\"String\"}]";
        var layout = FWLayout.FromJson(json);
        var file = new FWDynamicFile(layout);
        var row = "   7Hi   ";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(row));
        var rec = file.ParseStream(stream).First();
        Assert.Equal(7, rec["Id"]);
    }
}