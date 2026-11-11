namespace Epharma.Labs.Tests;

using System.Text;
using Epharma.Labs.FWFile;
using Xunit;

public class DefaultValueModel
{
    [FWField(4, DefaultValue = 99)]
    public int Id;

    [FWField(10, DefaultValue = "N/A")]
    public string Name = default!;

    [FWField(10, dateTimeFormat: "yyyy-MM-dd", DefaultValue = "2000-01-01")]
    public DateTime Creation;

    [FWField(5, DefaultValue = false)]
    public bool Enabled;
}

public class DefaultValueTests
{
    [Fact]
    public void UsesDefaultOnParseFailure()
    {
        var f = new FWFile<DefaultValueModel>();
        // Id field "ABCD" fails int parse -> default 99
        // Name field already string no parse failure; put empty? string no failure
        // Creation field "not-a-date" fails -> default
        // Enabled field "maybe" fails -> default false
        var row = "ABCD" + "          " + "not-a-date" + "maybe";
        // lengths: 4 + 10 + 10 + 5 = 29
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(row));
        var r = f.ParseStream(stream).First();
        Assert.Equal(99, r.Id);
        Assert.Equal(new DateTime(2000, 1, 1), r.Creation);
        Assert.False(r.Enabled);
    }
}