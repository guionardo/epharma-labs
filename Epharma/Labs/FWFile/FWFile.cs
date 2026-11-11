using System.Reflection;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Linq.Expressions;

namespace Epharma.Labs.FWFile;

public class FWFile<T> where T : new()
{
    record Column
    {
        internal FWFieldAttribute Attribute = null!;
        internal MemberInfo Member = null!;

        internal bool IsNullable;

        internal CultureInfo Culture = CultureInfo.InvariantCulture;

        internal IFWConverter? Converter;

        internal delegate void ReadDataDelegate(ReadOnlySpan<char> data, T record);
        internal ReadDataDelegate ReadData = null!;

        internal Type FieldType => Member is FieldInfo fi ? fi.FieldType : ((PropertyInfo)Member).PropertyType;

        internal Type NonNullableType => Nullable.GetUnderlyingType(FieldType) ?? FieldType;

        internal object? ResolveDefaultValue()
        {
            if (!Attribute.HasDefaultValue) throw new InvalidOperationException("No default value defined for this field.");
            if (Attribute.DefaultValue is null) return null;
            var underlying = NonNullableType;
            var v = Attribute.DefaultValue;
            if (v.GetType() == underlying || underlying.IsAssignableFrom(v.GetType())) return v;
            try
            {
                return Convert.ChangeType(v, underlying, Culture);
            }
            catch (Exception ex)
            {
                throw new FWParseException($"Failed to convert default value '{v}' to {underlying.Name} for field '{Member.Name}'", ex);
            }
        }

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
            Culture = string.IsNullOrWhiteSpace(Attribute.Culture) ? CultureInfo.InvariantCulture : new CultureInfo(Attribute.Culture);
            if (Attribute.ConverterType is not null)
            {
                if (!typeof(IFWConverter).IsAssignableFrom(Attribute.ConverterType)) throw new FWParseException($"Converter type must implement {nameof(IFWConverter)}");
                Converter = (IFWConverter)Activator.CreateInstance(Attribute.ConverterType)!;
                ReadData = ReadWithConverter;
                return this;
            }
            if (ft == typeof(string))
            {
                ReadData = ReadString;
            }
            else if (ft == typeof(int))
            {
                ReadData = ReadInt;
            }
            else if (ft == typeof(long))
            {
                ReadData = ReadLong;
            }
            else if (ft == typeof(short))
            {
                ReadData = ReadShort;
            }
            else if (ft == typeof(byte))
            {
                ReadData = ReadByte;
            }
            else if (ft == typeof(float))
            {
                ReadData = ReadFloat;
            }
            else if (ft == typeof(double))
            {
                ReadData = ReadDouble;
            }
            else if (ft == typeof(decimal))
            {
                ReadData = ReadDecimal;
            }
            else if (ft == typeof(bool))
            {
                ReadData = ReadBool;
            }
            else if (ft == typeof(char))
            {
                ReadData = ReadChar;
            }
            else if (ft == typeof(Guid))
            {
                ReadData = ReadGuid;
            }
            else if (ft == typeof(DateTime))
            {
                ReadData = ReadDateTime;
            }
            else if (ft == typeof(DateTimeOffset))
            {
                ReadData = ReadDateTimeOffset;
            }
            else if (ft == typeof(DateOnly))
            {
                ReadData = ReadDate;
            }
            else if (ft == typeof(TimeOnly))
            {
                ReadData = ReadTime;
            }
            else if (ft == typeof(TimeSpan))
            {
                ReadData = ReadTimeSpan;
            }
            else if (ft.IsEnum)
            {
                ReadData = ReadEnum;
            }
            else
            {
                throw new FWParseException($"Unsupported field type '{ft.FullName}' for member '{Member.Name}'");
            }
            return this;
        }

        internal void ReadString(ReadOnlySpan<char> data, T record)
        {
            if (IsNullable && data.Trim().IsEmpty)
            {
                SetMemberValue(record, null);
                return;
            }
            SetMemberValue(record, data.ToString());
        }
        internal void ReadInt(ReadOnlySpan<char> data, T record)
        {
            if (IsNullable && data.Trim().IsEmpty) { SetMemberValue(record, null); return; }
            if (!int.TryParse(data, NumberStyles.Integer, Culture, out var i))
            {
                throw new FWParseException($"Failed to parse value '{data}' to int field '{Member.Name}");
            }
            SetMemberValue(record, i);
        }
        internal void ReadFloat(ReadOnlySpan<char> data, T record)
        {
            if (IsNullable && data.Trim().IsEmpty) { SetMemberValue(record, null); return; }
            if (!float.TryParse(data, NumberStyles.Float, Culture, out var f))
            {
                throw new FWParseException($"Failed to parse value '{data}' to float field '{Member.Name}");
            }
            SetMemberValue(record, f);
        }

        internal void ReadBool(ReadOnlySpan<char> data, T record)
        {
            if (IsNullable && data.Trim().IsEmpty) { SetMemberValue(record, null); return; }
            var s = data.ToString();
            if (Attribute.BoolTrueValues.Contains(s))
            {
                SetMemberValue(record, true);
                return;
            }
            if (bool.TryParse(data, out var b))
            {
                SetMemberValue(record, b);
                return;
            }
            if (s.Equals("false", StringComparison.InvariantCultureIgnoreCase)
                || s == "0"
                || s.Equals("n", StringComparison.InvariantCultureIgnoreCase)
                || s.Equals("no", StringComparison.InvariantCultureIgnoreCase)
                || s.Equals("f", StringComparison.InvariantCultureIgnoreCase))
            {
                SetMemberValue(record, false);
                return;
            }
            throw new FWParseException($"Failed to parse value '{s}' to bool field '{Member.Name}'");
        }
        internal void ReadDecimal(ReadOnlySpan<char> data, T record)
        {
            if (IsNullable && data.Trim().IsEmpty) { SetMemberValue(record, null); return; }
            if (!decimal.TryParse(data, NumberStyles.Number, Culture, out var d))
            {
                throw new FWParseException($"Failed to parse value '{data}' to Decimal field '{Member.Name}'");
            }
            SetMemberValue(record, d);
        }

        internal void ReadDateTime(ReadOnlySpan<char> data, T record)
        {
            if (IsNullable && data.Trim().IsEmpty) { SetMemberValue(record, null); return; }
            foreach (var template in Attribute.DateTimeFormats)
            {
                if (DateTime.TryParseExact(data, template, Culture, DateTimeStyles.None, out var d))
                {
                    SetMemberValue(record, d);
                    return;
                }
            }
            throw new FWParseException($"Failed to parse value '{data}' to DateTime field '{Member.Name}");
        }
        internal void ReadDate(ReadOnlySpan<char> data, T record)
        {
            if (IsNullable && data.Trim().IsEmpty) { SetMemberValue(record, null); return; }
            foreach (var template in Attribute.DateFormats)
            {
                if (DateOnly.TryParseExact(data, template, Culture, DateTimeStyles.None, out var d))
                {
                    SetMemberValue(record, d);
                    return;
                }
            }
            throw new FWParseException($"Failed to parse value '{data}' to DateOnly field '{Member.Name}");
        }
        internal void ReadTime(ReadOnlySpan<char> data, T record)
        {
            if (IsNullable && data.Trim().IsEmpty) { SetMemberValue(record, null); return; }
            foreach (var template in Attribute.TimeFormats)
            {
                if (TimeOnly.TryParseExact(data, template, Culture, DateTimeStyles.None, out var t))
                {
                    SetMemberValue(record, t);
                    return;
                }
            }
            throw new FWParseException($"Failed to parse value '{data}' to TimeOnly field '{Member.Name}");
        }

        internal void ReadWithConverter(ReadOnlySpan<char> data, T record)
        {
            if (IsNullable && data.Trim().IsEmpty) { SetMemberValue(record, null); return; }
            var value = Converter!.Read(data, NonNullableType);
            SetMemberValue(record, value);
        }

        internal void ReadLong(ReadOnlySpan<char> data, T record)
        {
            if (IsNullable && data.Trim().IsEmpty) { SetMemberValue(record, null); return; }
            if (!long.TryParse(data, NumberStyles.Integer, Culture, out var v)) throw new FWParseException($"Failed to parse value '{data}' to long field '{Member.Name}'");
            SetMemberValue(record, v);
        }
        internal void ReadShort(ReadOnlySpan<char> data, T record)
        {
            if (IsNullable && data.Trim().IsEmpty) { SetMemberValue(record, null); return; }
            if (!short.TryParse(data, NumberStyles.Integer, Culture, out var v)) throw new FWParseException($"Failed to parse value '{data}' to short field '{Member.Name}'");
            SetMemberValue(record, v);
        }
        internal void ReadByte(ReadOnlySpan<char> data, T record)
        {
            if (IsNullable && data.Trim().IsEmpty) { SetMemberValue(record, null); return; }
            if (!byte.TryParse(data, NumberStyles.Integer, Culture, out var v)) throw new FWParseException($"Failed to parse value '{data}' to byte field '{Member.Name}'");
            SetMemberValue(record, v);
        }
        internal void ReadDouble(ReadOnlySpan<char> data, T record)
        {
            if (IsNullable && data.Trim().IsEmpty) { SetMemberValue(record, null); return; }
            if (!double.TryParse(data, NumberStyles.Float, Culture, out var v)) throw new FWParseException($"Failed to parse value '{data}' to double field '{Member.Name}'");
            SetMemberValue(record, v);
        }
        internal void ReadChar(ReadOnlySpan<char> data, T record)
        {
            if (IsNullable && data.Trim().IsEmpty) { SetMemberValue(record, null); return; }
            var t = data.Trim();
            if (t.IsEmpty) { if (IsNullable) { SetMemberValue(record, null); return; } throw new FWParseException($"Failed to parse value '{data}' to char field '{Member.Name}'"); }
            SetMemberValue(record, t[0]);
        }
        internal void ReadGuid(ReadOnlySpan<char> data, T record)
        {
            if (IsNullable && data.Trim().IsEmpty) { SetMemberValue(record, null); return; }
            if (!Guid.TryParse(data.ToString(), out var v)) throw new FWParseException($"Failed to parse value '{data}' to Guid field '{Member.Name}'");
            SetMemberValue(record, v);
        }
        internal void ReadDateTimeOffset(ReadOnlySpan<char> data, T record)
        {
            if (IsNullable && data.Trim().IsEmpty) { SetMemberValue(record, null); return; }
            if (!DateTimeOffset.TryParse(data.ToString(), Culture, DateTimeStyles.None, out var v)) throw new FWParseException($"Failed to parse value '{data}' to DateTimeOffset field '{Member.Name}'");
            SetMemberValue(record, v);
        }
        internal void ReadTimeSpan(ReadOnlySpan<char> data, T record)
        {
            if (IsNullable && data.Trim().IsEmpty) { SetMemberValue(record, null); return; }
            if (!TimeSpan.TryParse(data.ToString(), Culture, out var v)) throw new FWParseException($"Failed to parse value '{data}' to TimeSpan field '{Member.Name}'");
            SetMemberValue(record, v);
        }
        internal void ReadEnum(ReadOnlySpan<char> data, T record)
        {
            if (IsNullable && data.Trim().IsEmpty) { SetMemberValue(record, null); return; }
            try
            {
                var v = Enum.Parse(NonNullableType, data.ToString(), ignoreCase: true);
                SetMemberValue(record, v);
            }
            catch (Exception ex)
            {
                throw new FWParseException($"Failed to parse value '{data}' to enum field '{Member.Name}'", ex);
            }
        }
    }
    private List<Column> columns = [];
    private int rowLength = 0;
    private readonly List<(MemberInfo Member, FWFieldAttribute Attribute)> _definitions = new();
    private bool _hasDefinitions;
    private bool _validated;

    public FWFile()
    {
    }

    public FWFile(Action<FWFile<T>> configure)
    {
        configure(this);
    }

    private void EnsureValidated()
    {
        if (_validated) return;
        if (!_hasDefinitions)
        {
            ValidateFields();
        }
        else
        {
            foreach (var (member, attribute) in _definitions)
            {
                if (string.IsNullOrWhiteSpace(attribute.Header))
                {
                    attribute.Header = member.Name;
                }
                rowLength += attribute.Length;
                columns.Add(new Column { Attribute = attribute, Member = member }.Validate());
            }
            columns = columns.OrderBy(c => c.Attribute.Order).ToList();
            if (columns.Count == 0) throw new FWParseException($"There is no field defined for the type {nameof(T)}");
        }
        _validated = true;
    }

    public FWFile<T> Field<TValue>(Expression<Func<T, TValue>> selector, int length, Action<FWFieldAttribute>? configure = null)
    {
        var member = GetMember(selector);
        var attribute = new FWFieldAttribute(length);
        configure?.Invoke(attribute);
        _definitions.Add((member, attribute));
        _hasDefinitions = true;
        return this;
    }

    private static MemberInfo GetMember<TValue>(Expression<Func<T, TValue>> selector)
    {
        if (selector.Body is MemberExpression me) return me.Member;
        if (selector.Body is UnaryExpression ue && ue.Operand is MemberExpression me2) return me2.Member;
        throw new ArgumentException("Selector must be a member access", nameof(selector));
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
        columns = columns.OrderBy(c => c.Attribute.Order).ToList();
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
            var data = row.AsSpan(index, c.Attribute.Length);
            index += c.Attribute.Length;
            if (c.Attribute.Trim) data = data.Trim();
            try
            {
                c.ReadData(data, record);
            }
            catch (Exception) when (c.Attribute.HasDefaultValue)
            {
                c.SetMemberValue(record, c.ResolveDefaultValue());
            }
        }
        return record;
    }

    private bool IsHeaderLine(string row)
    {
        if (row.Length != rowLength) return false;
        var index = 0;
        foreach (var c in columns)
        {
            var field = row.AsSpan(index, c.Attribute.Length);
            index += c.Attribute.Length;
            var header = c.Attribute.Header ?? string.Empty;
            if (!field.Trim().Equals(header.AsSpan(), StringComparison.OrdinalIgnoreCase)) return false;
        }
        return true;
    }

    private string NormalizeRow(string row)
    {
        if (row.Length == rowLength) return row;
        if (row.Length < rowLength) return row.PadRight(rowLength);
        return row.Substring(0, rowLength);
    }

    public string Format(T record)
    {
        EnsureValidated();
        var sb = new StringBuilder(rowLength);
        foreach (var c in columns)
        {
            var value = c.Member is FieldInfo fi ? fi.GetValue(record) : ((PropertyInfo)c.Member).GetValue(record);
            string text;
            if (c.Converter is not null)
            {
                text = c.Converter.Write(value, c.NonNullableType);
            }
            else
            {
                text = value switch
                {
                    null => string.Empty,
                    string s => s,
                    bool b => b ? "true" : "false",
                    DateTime dt => dt.ToString(c.Attribute.DateTimeFormats[0], c.Culture),
                    DateOnly d => d.ToString(c.Attribute.DateFormats[0], c.Culture),
                    TimeOnly t => t.ToString(c.Attribute.TimeFormats[0], c.Culture),
                    IFormattable f => f.ToString(null, c.Culture),
                    _ => value.ToString() ?? string.Empty,
                };
            }
            if (text.Length > c.Attribute.Length) text = text.Substring(0, c.Attribute.Length);
            sb.Append(c.Attribute.RightAlign
                ? text.PadLeft(c.Attribute.Length, c.Attribute.PadChar)
                : text.PadRight(c.Attribute.Length, c.Attribute.PadChar));
        }
        return sb.ToString();
    }

    public void Write(Stream stream, IEnumerable<T> records, Encoding? encoding = null, bool writeHeader = false)
    {
        EnsureValidated();
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

    public async Task WriteAsync(Stream stream, IEnumerable<T> records, Encoding? encoding = null, bool writeHeader = false, CancellationToken cancellationToken = default)
    {
        EnsureValidated();
        await using var writer = new StreamWriter(stream, encoding ?? Encoding.UTF8, leaveOpen: true);
        if (writeHeader)
        {
            var header = new StringBuilder(rowLength);
            foreach (var c in columns)
            {
                var h = c.Attribute.Header ?? string.Empty;
                if (h.Length > c.Attribute.Length) h = h.Substring(0, c.Attribute.Length);
                header.Append(h.PadRight(c.Attribute.Length));
            }
            await writer.WriteLineAsync(header.ToString()).ConfigureAwait(false);
        }
        foreach (var r in records)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await writer.WriteLineAsync(Format(r)).ConfigureAwait(false);
        }
        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public IEnumerable<T> ParseStream(Stream stream, Encoding? encoding = null, bool skipHeader = false, int maxRows = 0, bool lenient = false, bool autoSkipHeader = false, CancellationToken cancellationToken = default)
    {
        EnsureValidated();
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
        var firstLine = !skipHeader;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (maxRows > 0 && yielded >= maxRows) break;
            index++;
            data = reader.ReadLine();
            if (data is null) break; // End of stream
            if (data.Length == 0) continue; // skip empty lines
            if (firstLine && autoSkipHeader && IsHeaderLine(data))
            {
                firstLine = false;
                continue;
            }
            firstLine = false;
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

    public async IAsyncEnumerable<T> ParseStreamAsync(Stream stream, Encoding? encoding = null, bool skipHeader = false, int maxRows = 0, bool lenient = false, bool autoSkipHeader = false, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        EnsureValidated();
        using var reader = new StreamReader(stream, encoding ?? Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var index = 0;
        if (skipHeader)
        {
            await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            index = 1;
        }
        string? data;
        var yielded = 0;
        var firstLine = !skipHeader;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (maxRows > 0 && yielded >= maxRows) break;
            index++;
            data = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (data is null) break;
            if (data.Length == 0) continue;
            if (firstLine && autoSkipHeader && IsHeaderLine(data))
            {
                firstLine = false;
                continue;
            }
            firstLine = false;
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
