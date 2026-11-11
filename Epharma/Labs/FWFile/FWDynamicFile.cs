using System.Globalization;
using System.Text;

namespace Epharma.Labs.FWFile;

/// <summary>Reads/writes fixed-width files using a configuration-driven <see cref="FWLayout"/>.</summary>
public class FWDynamicFile
{
    public FWLayout Layout { get; }

    public FWDynamicFile(FWLayout layout)
    {
        Layout = layout ?? throw new ArgumentNullException(nameof(layout));
    }

    private static readonly string[] DefaultDateFormats = { "yyyy-MM-dd", "dd/MM/yyyy" };
    private static readonly string[] DefaultTimeFormats = { "HH:mm:ss", "HHmmss" };
    private static readonly string[] DefaultDateTimeFormats = { "yyyy-MM-dd", "MM/dd/yyyy HH:mm", "dd-MMM-yyyy" };

    private static object? ParseField(FWFieldDefinition field, ReadOnlySpan<char> data)
    {
        var culture = field.GetCulture();
        var span = field.Trim ? data.Trim() : data;
        if (span.IsEmpty)
        {
            return field.Type == FWFieldType.String ? string.Empty : null;
        }
        switch (field.Type)
        {
            case FWFieldType.String: return span.ToString();
            case FWFieldType.Int: int.TryParse(span, NumberStyles.Integer, culture, out var i); return i;
            case FWFieldType.Long: long.TryParse(span, NumberStyles.Integer, culture, out var l); return l;
            case FWFieldType.Short: short.TryParse(span, NumberStyles.Integer, culture, out var sh); return sh;
            case FWFieldType.Byte: byte.TryParse(span, NumberStyles.Integer, culture, out var by); return by;
            case FWFieldType.Float: float.TryParse(span, NumberStyles.Float, culture, out var f); return f;
            case FWFieldType.Double: double.TryParse(span, NumberStyles.Float, culture, out var d); return d;
            case FWFieldType.Decimal: decimal.TryParse(span, NumberStyles.Number & ~NumberStyles.AllowThousands, culture, out var dec); return dec;
            case FWFieldType.Bool:
                var s = span.ToString();
                if (s.Equals("true", StringComparison.InvariantCultureIgnoreCase) || s == "1" || s.Equals("y", StringComparison.InvariantCultureIgnoreCase) || s.Equals("s", StringComparison.InvariantCultureIgnoreCase)) return true;
                if (s.Equals("false", StringComparison.InvariantCultureIgnoreCase) || s == "0" || s.Equals("n", StringComparison.InvariantCultureIgnoreCase) || s.Equals("f", StringComparison.InvariantCultureIgnoreCase)) return false;
                bool.TryParse(span, out var b); return b;
            case FWFieldType.Char: return span[0];
            case FWFieldType.Guid: Guid.TryParse(span.ToString(), out var g); return g;
            case FWFieldType.DateTime:
                foreach (var fmt in (string.IsNullOrWhiteSpace(field.Format) ? DefaultDateTimeFormats : new[] { field.Format }))
                    if (DateTime.TryParseExact(span, fmt, culture, DateTimeStyles.None, out var dt)) return dt;
                DateTime.TryParse(span.ToString(), culture, DateTimeStyles.None, out var dt2); return dt2;
            case FWFieldType.DateTimeOffset:
                DateTimeOffset.TryParse(span.ToString(), culture, DateTimeStyles.None, out var dto); return dto;
            case FWFieldType.DateOnly:
                foreach (var fmt in (string.IsNullOrWhiteSpace(field.Format) ? DefaultDateFormats : new[] { field.Format }))
                    if (DateOnly.TryParseExact(span, fmt, culture, DateTimeStyles.None, out var dobj)) return dobj;
                DateOnly.TryParse(span.ToString(), culture, DateTimeStyles.None, out var dobj2); return dobj2;
            case FWFieldType.TimeOnly:
                foreach (var fmt in (string.IsNullOrWhiteSpace(field.Format) ? DefaultTimeFormats : new[] { field.Format }))
                    if (TimeOnly.TryParseExact(span, fmt, culture, DateTimeStyles.None, out var tobj)) return tobj;
                TimeOnly.TryParse(span.ToString(), culture, DateTimeStyles.None, out var tobj2); return tobj2;
            case FWFieldType.TimeSpan:
                TimeSpan.TryParse(span.ToString(), culture, out var ts); return ts;
            default: return span.ToString();
        }
    }

    private static string FormatField(FWFieldDefinition field, object? value)
    {
        var culture = field.GetCulture();
        if (value is null) return new string(' ', field.Length);
        string text;
        switch (field.Type)
        {
            case FWFieldType.Bool: text = (bool)value ? "true" : "false"; break;
            case FWFieldType.DateTime: text = ((DateTime)value).ToString(string.IsNullOrWhiteSpace(field.Format) ? "yyyy-MM-dd" : field.Format, culture); break;
            case FWFieldType.DateOnly: text = ((DateOnly)value).ToString(string.IsNullOrWhiteSpace(field.Format) ? "yyyy-MM-dd" : field.Format, culture); break;
            case FWFieldType.TimeOnly: text = ((TimeOnly)value).ToString(string.IsNullOrWhiteSpace(field.Format) ? "HH:mm:ss" : field.Format, culture); break;
            default: text = value.ToString() ?? string.Empty; break;
        }
        if (text.Length > field.Length) text = text.Substring(0, field.Length);
        return text;
    }

    public IEnumerable<Dictionary<string, object?>> ParseStream(Stream stream, Encoding? encoding = null, bool lenient = false, int maxRows = 0, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream, encoding ?? Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        string? data;
        var yielded = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (maxRows > 0 && yielded >= maxRows) break;
            data = reader.ReadLine();
            if (data is null) break;
            if (data.Length == 0) continue;
            var row = data;
            if (lenient && row.Length != Layout.RowLength)
                row = row.Length < Layout.RowLength ? row.PadRight(Layout.RowLength) : row.Substring(0, Layout.RowLength);
            var record = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in Layout.Fields)
            {
                var start = field.Start;
                var end = start + field.Length;
                if (end > row.Length)
                {
                    if (lenient) row = row.PadRight(end);
                    else throw new FWParseException($"Row is too short for field '{field.Name}'.");
                }
                var slice = row.AsSpan(start, field.Length);
                record[field.Name] = ParseField(field, slice);
            }
            yielded++;
            yield return record;
        }
    }

    public void Write(Stream stream, IEnumerable<Dictionary<string, object?>> records, Encoding? encoding = null)
    {
        using var writer = new StreamWriter(stream, encoding ?? Encoding.UTF8, leaveOpen: true);
        foreach (var record in records)
        {
            var chars = new char[Layout.RowLength];
            for (var i = 0; i < chars.Length; i++) chars[i] = ' ';
            foreach (var field in Layout.Fields)
            {
                var text = FormatField(field, record.TryGetValue(field.Name, out var v) ? v : null);
                var arr = text.ToCharArray();
                var len = Math.Min(arr.Length, field.Length);
                int offset;
                if (field.RightAlign)
                    offset = field.Start + field.Length - len;
                else
                    offset = field.Start;
                for (var i = 0; i < len; i++)
                    chars[offset + i] = arr[i];
            }
            writer.WriteLine(new string(chars));
        }
        writer.Flush();
    }
}