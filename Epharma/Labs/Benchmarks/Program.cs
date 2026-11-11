using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Epharma.Labs.FWFile;
using System.Text;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

[MemoryDiagnoser]
public class FixedWidthBenchmarks
{
    private byte[] _data = Array.Empty<byte>();
    private const int Rows = 100_000;

    [GlobalSetup]
    public void Setup()
    {
        var lines = new StringBuilder();
        for (int i = 0; i < Rows; i++)
        {
            lines.Append($"{i % 9999 + 1,4}");
            lines.Append("Paciente Teste      ");
            lines.Append("     10.00");
            lines.Append("1977-02-05");
            lines.Append("S    ");
            lines.Append("2001-01-01");
            lines.Append('\n');
        }
        _data = Encoding.UTF8.GetBytes(lines.ToString());
    }

    [Benchmark]
    public int ParseStream()
    {
        using var ms = new MemoryStream(_data);
        var f = new FWFile<BenchModel>();
        int n = 0;
        foreach (var _ in f.ParseStream(ms)) n++;
        return n;
    }

    [Benchmark]
    public async Task<int> ParseStreamAsync()
    {
        using var ms = new MemoryStream(_data);
        var f = new FWFile<BenchModel>();
        int n = 0;
        await foreach (var _ in f.ParseStreamAsync(ms)) n++;
        return n;
    }
}

public class BenchModel
{
    [FWField(4)]
    public int Id;

    [FWField(20)]
    public string Name = default!;

    [FWField(10)]
    public decimal Price;

    [FWField(10, dateTimeFormat: "yyyy-MM-dd")]
    public DateTime Creation;

    [FWField(5)]
    public bool Enabled;

    [FWField(10)]
    public DateOnly BirthDay;
}