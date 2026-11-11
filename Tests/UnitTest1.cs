namespace Tests;

using System.Text;
using fwfile;
using Xunit.Abstractions;

public class SampleFW
{
    [FWField(4)]
    public int Id;

    [FWField(20)]
    public string Name = default!;

    [FWField(10)]
    public decimal Price;

    [FWField(10, dateTimeFormat: "yyyy-MM-dd")]
    public DateTime Creation;

    [FWField(1)]
    public bool Enabled;

    [FWField(10)]
    public DateOnly BirthDay;

    [FWField(8)]
    public TimeOnly StartTime;

    public override string ToString()
    {
        return $"#{Id}:{Name}:{Price}:{Creation}:{Enabled}:{BirthDay}:{StartTime}";
    }
}
public class UnitTest1(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void Test1()
    {
        var f = new FWFile<SampleFW>();

        Assert.NotNull(f);

        var rows = @"   1Guionardo Furlan         10.001977-02-05S2001-01-0101:02:03
   2Benjamin Furlan          20.002014-02-14N2002-02-0223:22:21";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rows));

        foreach (var s in f.ParseStream(stream))
        {
            _output.WriteLine(s.ToString());
        }


    }
}
