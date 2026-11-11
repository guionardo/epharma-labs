using System.Reflection;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Runtime.CompilerServices;

namespace fwfile;

public class FWFile<T> where T : new()
{
    record Column
    {
        internal FWFieldAttribute Attribute = null!;
        internal MemberInfo Member = null!;

        internal bool IsNullable;

        internal Action<string, T> ReadData = null!;

        internal void SetMemberValue(T record, object? value)
        {
            if (Member is FieldInfo fi) fi.SetValue(record, value);
            else ((PropertyInfo)Member).SetValue(record, value);
        }
        internal Column Validate()
        {
            // Accepted types: int, decimal, float, bool, string, datetime, date, time
            var rawType = Member is FieldInfo fi ? fi.FieldType : ((PropertyInfo)Member).PropertyType;
            var ft = Nullable.GetUnderlyingType(rawType) ?? rawType;
            IsNullable = rawType != ft;
            if (ft == typeof(string))
            {
                ReadData = ReadString;
            }
            else if (ft == typeof(int))
            {
                ReadData = ReadInt;
            }
            else if (ft == typeof(float))
            {
                ReadData = ReadFloat;
            }
            else if (ft == typeof(bool))
            {
                ReadData = ReadBool;
            }
            else if (ft == typeof(decimal))
            {
                ReadData = ReadDecimal;
            }
            else if (ft == typeof(DateTime))
            {
                ReadData = ReadDateTime;
            }
            else if (ft == typeof(DateOnly))
            {
                ReadData = ReadDate;
            }
            else if (ft == typeof(TimeOnly))
            {
                ReadData = ReadTime;
            }
            else
            {
                throw new FWParseException($"");
            }
            return this;
        }

        internal void ReadString(string data, T record)
        {
            SetMemberValue(record, IsNullable && string.IsNullOrWhiteSpace(data) ? null : data);
        }
        internal void ReadInt(string data, T record)
        {
            if (IsNullable && string.IsNullOrWhiteSpace(data)) { SetMemberValue(record, null); return; }
            if (!int.TryParse(data, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
            {
                throw new FWParseException($"Failed to parse value '{data}' to int field '{Member.Name}");
            }
            SetMemberValue(record, i);
        }
        internal void ReadFloat(string data, T record)
        {
            if (IsNullable && string.IsNullOrWhiteSpace(data)) { SetMemberValue(record, null); return; }
            if (!float.TryParse(data, NumberStyles.Float, CultureInfo.InvariantCulture, out var f))
            {
                throw new FWParseException($"Failed to parse value '{data}' to float field '{Member.Name}");
            }
            SetMemberValue(record, f);
        }

        internal void ReadBool(string data, T record)
        {
            if (IsNullable && string.IsNullOrWhiteSpace(data)) { SetMemberValue(record, null); return; }
            if (Attribute.BoolTrueValues.Contains(data))
            {
                SetMemberValue(record, true);
                return;
            }
            if (bool.TryParse(data, out var b))
            {
                SetMemberValue(record, b);
                return;
            }
            if (data.Equals("false", StringComparison.InvariantCultureIgnoreCase)
                || data.Equals("0", StringComparison.InvariantCultureIgnoreCase)
                || data.Equals("n", StringComparison.InvariantCultureIgnoreCase)
                || data.Equals("no", StringComparison.InvariantCultureIgnoreCase)
                || data.Equals("f", StringComparison.InvariantCultureIgnoreCase))
            {
                SetMemberValue(record, false);
                return;
            }
            throw new FWParseException($"Failed to parse value '{data}' to bool field '{Member.Name}'");
        }
        internal void ReadDecimal(string data, T record)
        {
            if (IsNullable && string.IsNullOrWhiteSpace(data)) { SetMemberValue(record, null); return; }
            if (!decimal.TryParse(data, NumberStyles.Number, CultureInfo.InvariantCulture, out var d))
            {
                throw new FWParseException($"Failed to parse value '{data}' to Decimal field '{Member.Name}'");
            }
            SetMemberValue(record, d);
        }

        internal void ReadDateTime(string data, T record)
        {
            if (IsNullable && string.IsNullOrWhiteSpace(data)) { SetMemberValue(record, null); return; }
            foreach (var template in Attribute.DateTimeFormats)
            {
                if (DateTime.TryParseExact(data, template, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                {
                    SetMemberValue(record, d);
                    return;
                }
            }
            throw new FWParseException($"Failed to parse value '{data}' to DateTime field '{Member.Name}");
        }
        internal void ReadDate(string data, T record)
        {
            if (IsNullable && string.IsNullOrWhiteSpace(data)) { SetMemberValue(record, null); return; }
            foreach (var template in Attribute.DateFormats)
            {
                if (DateOnly.TryParseExact(data, template, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                {
                    SetMemberValue(record, d);
                    return;
                }
            }
            throw new FWParseException($"Failed to parse value '{data}' to DateOnly field '{Member.Name}");
        }
        internal void ReadTime(string data, T record)
        {
            if (IsNullable && string.IsNullOrWhiteSpace(data)) { SetMemberValue(record, null); return; }
            foreach (var template in Attribute.TimeFormats)
            {
                if (TimeOnly.TryParseExact(data, template, CultureInfo.InvariantCulture, DateTimeStyles.None, out var t))
                {
                    SetMemberValue(record, t);
                    return;
                }
            }
            throw new FWParseException($"Failed to parse value '{data}' to TimeOnly field '{Member.Name}");
        }
    }
    private List<Column> columns = [];
    private int rowLength = 0;
    public FWFile()
    {
        ValidateFields();
    }

    private void ValidateFields()
    {
        Type typeInfo = typeof(T);


        // Retrieve the fields and public read/write properties of the class
        var members = typeInfo.GetFields(BindingFlags.Public | BindingFlags.Instance).Cast<MemberInfo>()
            .Concat(typeInfo.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead && p.CanWrite));
        foreach (var member in members)
        {
            var attribute = member.GetCustomAttribute<FWFieldAttribute>();
            if (attribute is null) continue;

            if (string.IsNullOrWhiteSpace(attribute.Header))
            {
                attribute.Header = member.Name;
            }
            rowLength += attribute.Length;

            columns.Add(new Column { Attribute = attribute, Member = member }.Validate());
        }
        if (columns.Count == 0)
        {
            throw new FWParseException($"There is no field defined with the {nameof(FWFieldAttribute)} attribute in the type {nameof(T)}");
        }
    }

    private T ParseRow(string row)
    {
        if (row.Length != rowLength) throw new FWParseException($"expected row with length {rowLength} but found {row.Length}");
        T record = new();
        var index = 0;
        foreach (var c in columns)
        {
            var data = row.Substring(index, c.Attribute.Length);
            index += c.Attribute.Length;
            c.ReadData(c.Attribute.Trim ? data.Trim() : data, record);
        }
        return record;
    }

    private string NormalizeRow(string row)
    {
        if (row.Length == rowLength) return row;
        if (row.Length < rowLength) return row.PadRight(rowLength);
        return row.Substring(0, rowLength);
    }

    public string Format(T record)
    {
        var sb = new StringBuilder(rowLength);
        foreach (var c in columns)
        {
            var value = c.Member is FieldInfo fi ? fi.GetValue(record) : ((PropertyInfo)c.Member).GetValue(record);
            string text = value switch
            {
                null => string.Empty,
                string s => s,
                bool b => b ? "true" : "false",
                DateTime dt => dt.ToString(c.Attribute.DateTimeFormats[0], CultureInfo.InvariantCulture),
                DateOnly d => d.ToString(c.Attribute.DateFormats[0], CultureInfo.InvariantCulture),
                TimeOnly t => t.ToString(c.Attribute.TimeFormats[0], CultureInfo.InvariantCulture),
                IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString() ?? string.Empty,
            };
            if (text.Length > c.Attribute.Length) text = text.Substring(0, c.Attribute.Length);
            sb.Append(c.Attribute.RightAlign
                ? text.PadLeft(c.Attribute.Length, c.Attribute.PadChar)
                : text.PadRight(c.Attribute.Length, c.Attribute.PadChar));
        }
        return sb.ToString();
    }

    public void Write(Stream stream, IEnumerable<T> records, Encoding? encoding = null, bool writeHeader = false)
    {
        using var writer = new StreamWriter(stream, encoding ?? Encoding.UTF8, leaveOpen: true);
        if (writeHeader)
        {
            var header = new StringBuilder(rowLength);
            foreach (var c in columns)
            {
                var h = c.Attribute.Header ?? string.Empty;
                if (h.Length > c.Attribute.Length) h = h.Substring(0, c.Attribute.Length);
                header.Append(h.PadRight(c.Attribute.Length));
            }
            writer.WriteLine(header.ToString());
        }
        foreach (var r in records)
        {
            writer.WriteLine(Format(r));
        }
        writer.Flush();
    }

    public IEnumerable<T> ParseStream(Stream stream, Encoding? encoding = null, bool skipHeader = false, int maxRows = 0, bool lenient = false, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream, encoding ?? Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var index = 0;
        if (skipHeader)
        {
            reader.ReadLine();
            index = 1;
        }
        string? data;
        T e;
        var yielded = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (maxRows > 0 && yielded >= maxRows) break;
            index++;
            data = reader.ReadLine();
            if (data is null) break; // End of stream
            if (data.Length == 0) continue; // skip empty lines
            try
            {
                e = ParseRow(lenient ? NormalizeRow(data) : data);
            }
            catch (Exception ex)
            {
                throw new FWParseException($"failed parsing line #{index} : '{data}' : {ex.Message}", ex);
            }
            yielded++;
            yield return e;
        }
    }

    public async IAsyncEnumerable<T> ParseStreamAsync(Stream stream, Encoding? encoding = null, bool skipHeader = false, int maxRows = 0, bool lenient = false, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream, encoding ?? Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var index = 0;
        if (skipHeader)
        {
            await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            index = 1;
        }
        string? data;
        var yielded = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (maxRows > 0 && yielded >= maxRows) break;
            index++;
            data = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (data is null) break;
            if (data.Length == 0) continue;
            T e;
            try
            {
                e = ParseRow(lenient ? NormalizeRow(data) : data);
            }
            catch (Exception ex)
            {
                throw new FWParseException($"failed parsing line #{index} : '{data}' : {ex.Message}", ex);
            }
            yielded++;
            yield return e;
        }
    }

}
