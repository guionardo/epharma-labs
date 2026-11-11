namespace Epharma.Labs.Tests;

using System.Text;
using Epharma.Labs.FWFile;
using Xunit;

public class ValidationModel
{
    [FWField(4, Required = true)]
    public int Id;

    [FWField(10, AllowedValues = "A,B,C")]
    public string Code = "";

    [FWField(10, Min = "0", Max = "100")]
    public int Score;
}

public class ValidationTests
{
    [Fact]
    public void CollectsErrors()
    {
        var f = new FWFile<ValidationModel>();
        var rows = "   0XX        150       \n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rows));
        var results = f.ParseStreamWithValidation(stream).ToList();
        Assert.Single(results);
        Assert.False(results[0].IsValid);
        Assert.True(results[0].Issues.Count >= 2);
    }

    [Fact]
    public void ValidRowHasNoErrors()
    {
        var f = new FWFile<ValidationModel>();
        var rows = "   1A         050       \n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rows));
        var results = f.ParseStreamWithValidation(stream).ToList();
        Assert.Single(results);
        Assert.True(results[0].IsValid);
    }
}