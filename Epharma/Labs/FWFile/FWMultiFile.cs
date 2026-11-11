using System.Text;
using System.Runtime.CompilerServices;

namespace Epharma.Labs.FWFile;

/// <summary>
/// Reads/writes fixed-width files that contain more than one record type
/// (header, detail, trailer), each identified by a discriminator field.
/// </summary>
public class FWMultiFile
{
    private int _discriminatorStart = 0;
    private int _discriminatorLength = 1;
    private readonly Dictionary<string, Type> _recordTypes = new(StringComparer.Ordinal);
    private readonly Dictionary<Type, IRecordParser> _parsersByType = new();
    private Action<IReadOnlyList<object>>? _trailerValidator;

    private interface IRecordParser
    {
        Type RecordType { get; }
        object ParseLine(string row);
        string Format(object record);
        int RowLength { get; }
    }

    private sealed class RecordParser<T> : IRecordParser where T : new()
    {
        public FWFile<T> File { get; }
        public RecordParser(FWFile<T> file) => File = file;
        public Type RecordType => typeof(T);
        public object ParseLine(string row) => File.ParseLine(row);
        public string Format(object record) => File.Format((T)record);
        public int RowLength => File.RowLength;
    }

    public FWMultiFile Discriminator(int start, int length)
    {
        if (start < 0) throw new ArgumentOutOfRangeException(nameof(start));
        if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length));
        _discriminatorStart = start;
        _discriminatorLength = length;
        return this;
    }

    public FWMultiFile Record<T>(string discriminator, Action<FWFile<T>>? configure = null) where T : new()
    {
        if (string.IsNullOrEmpty(discriminator)) throw new ArgumentException("Discriminator value is required.", nameof(discriminator));
        if (discriminator.Length != _discriminatorLength)
            throw new ArgumentException($"Discriminator '{discriminator}' length must be {_discriminatorLength}.", nameof(discriminator));
        if (_recordTypes.ContainsKey(discriminator))
            throw new FWParseException($"Discriminator '{discriminator}' is already mapped.");
        var file = new FWFile<T>();
        configure?.Invoke(file);
        var parser = new RecordParser<T>(file);
        _recordTypes[discriminator] = typeof(T);
        _parsersByType[typeof(T)] = parser;
        return this;
    }

    public FWMultiFile TrailerValidator(Action<IReadOnlyList<object>> validator)
    {
        _trailerValidator = validator;
        return this;
    }

    private IRecordParser ResolveParser(string row)
    {
        if (row.Length < _discriminatorStart + _discriminatorLength)
            throw new FWParseException($"Row is too short to contain the discriminator (expected at position {_discriminatorStart}, length {_discriminatorLength}).");
        var key = row.Substring(_discriminatorStart, _discriminatorLength);
        if (!_recordTypes.TryGetValue(key, out var type))
            throw new FWParseException($"Unknown record type discriminator '{key}'.");
        return _parsersByType[type];
    }

    public IEnumerable<object> ParseStream(Stream stream, Encoding? encoding = null, int maxRows = 0, bool lenient = false, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream, encoding ?? Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var records = new List<object>();
        string? data;
        var yielded = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (maxRows > 0 && yielded >= maxRows) break;
            data = reader.ReadLine();
            if (data is null) break;
            if (data.Length == 0) continue;
            var parser = ResolveParser(data);
            var row = lenient && data.Length != parser.RowLength
                ? (data.Length < parser.RowLength ? data.PadRight(parser.RowLength) : data.Substring(0, parser.RowLength))
                : data;
            var record = parser.ParseLine(row);
            records.Add(record);
            yielded++;
            yield return record;
        }
        _trailerValidator?.Invoke(records);
    }

    public async IAsyncEnumerable<object> ParseStreamAsync(Stream stream, Encoding? encoding = null, int maxRows = 0, bool lenient = false, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream, encoding ?? Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var records = new List<object>();
        string? data;
        var yielded = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (maxRows > 0 && yielded >= maxRows) break;
            data = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (data is null) break;
            if (data.Length == 0) continue;
            var parser = ResolveParser(data);
            var row = lenient && data.Length != parser.RowLength
                ? (data.Length < parser.RowLength ? data.PadRight(parser.RowLength) : data.Substring(0, parser.RowLength))
                : data;
            var record = parser.ParseLine(row);
            records.Add(record);
            yielded++;
            yield return record;
        }
        _trailerValidator?.Invoke(records);
    }

    public void Write(Stream stream, IEnumerable<object> records, Encoding? encoding = null)
    {
        using var writer = new StreamWriter(stream, encoding ?? Encoding.UTF8, leaveOpen: true);
        foreach (var record in records)
        {
            var type = record.GetType();
            if (!_parsersByType.TryGetValue(type, out var parser))
                throw new FWParseException($"No record type registered for '{type.Name}'.");
            writer.WriteLine(parser.Format(record));
        }
        writer.Flush();
    }

    public async Task WriteAsync(Stream stream, IEnumerable<object> records, Encoding? encoding = null, CancellationToken cancellationToken = default)
    {
        await using var writer = new StreamWriter(stream, encoding ?? Encoding.UTF8, leaveOpen: true);
        foreach (var record in records)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var type = record.GetType();
            if (!_parsersByType.TryGetValue(type, out var parser))
                throw new FWParseException($"No record type registered for '{type.Name}'.");
            await writer.WriteLineAsync(parser.Format(record)).ConfigureAwait(false);
        }
        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}