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
            lines.Append($"{1000,8}");
            lines.Append("19770205");
            lines.Append("S    ");
            lines.Append("20010101  ");
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

[MemoryDiagnoser]
public class MultiFileBenchmarks
{
    private byte[] _data = Array.Empty<byte>();
    private const int Rows = 100_000;

    [GlobalSetup]
    public void Setup()
    {
        var lines = new StringBuilder();
        lines.Append("0    2\n"); // header Count=2 (placeholder)
        for (int i = 0; i < Rows; i++)
        {
            lines.Append("1");
            lines.Append($"{i % 9999 + 1,4}");
            lines.Append("Paciente Teste      ");
            lines.Append('\n');
        }
        lines.Append("9    2\n");
        _data = Encoding.UTF8.GetBytes(lines.ToString());
    }

    [Benchmark]
    public int ParseMulti()
    {
        using var ms = new MemoryStream(_data);
        var multi = new FWMultiFile()
            .Record<MultiHeaderBench>("0")
            .Record<MultiDetailBench>("1")
            .Record<MultiTrailerBench>("9");
        int n = 0;
        foreach (var _ in multi.ParseStream(ms)) n++;
        return n;
    }
}

public class MultiHeaderBench { [FWField(1)] public string Type = ""; [FWField(5)] public int Count; }
public class MultiDetailBench { [FWField(1)] public string Type = ""; [FWField(4)] public int Id; [FWField(20)] public string Name = default!; }
public class MultiTrailerBench { [FWField(1)] public string Type = ""; [FWField(5)] public int Total; }

public class BenchModel
{
    [FWField(4)]
    public int Id;

    [FWField(20)]
    public string Name = default!;

    [FWField(8, Decimals = 2)]
    public decimal Price;

    [FWField(8, Start = 32, DateTimeFormat = "yyyyMMdd")]
    public DateTime Creation;

    [FWField(5, Start = 40)]
    public bool Enabled;

    [FWField(10, Start = 45, DateTimeFormat = "yyyyMMdd")]
    public DateOnly BirthDay;
}