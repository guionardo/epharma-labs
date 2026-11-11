using System.Reflection;
using System.Globalization;
using Microsoft.VisualBasic;

namespace fwfile;
// 1. Specify where the attribute can be used
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class FWFieldAttribute : Attribute
{

    /// <summary>
    /// 
    /// </summary>
    /// <param name="length"></param>
    /// <param name="header"></param>
    /// <param name="boolTrueValues">default "true", "1", "Y", "S"</param>
    /// <param name="dateTimeFormat"></param>
    public FWFieldAttribute(int length, string header = "", string? boolTrueValues = null, string dateTimeFormat = "", string dateFormat = "", string timeFormat = "")
    {
        Length = length;
        Header = header;
        boolTrueValues ??= "true,1,y,s";
        BoolTrueValues = new(boolTrueValues.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries), StringComparer.InvariantCultureIgnoreCase);
        DateTimeFormats = DefaultDateTimeFormat(dateTimeFormat);
        DateFormats = DefaultDateFormat(dateFormat);
        TimeFormats = DefaultTimeFormat(timeFormat);
    }
    /// <summary>
    /// length of the field
    /// </summary>
    public int Length { get; private set; }
    /// <summary>
    /// Header in the first line of the file
    /// </summary>
    public string Header { get; internal set; }
    // public string BoolTrueValues {get;internal set;} = boolTrueValues;
    public readonly HashSet<string> BoolTrueValues;
    public readonly string[] DateTimeFormats;
    public readonly string[] DateFormats;
    public readonly string[] TimeFormats;

    private static string[] DefaultDateTimeFormat(string dateTimeFormat)
    {
        return DefaultFormat(dateTimeFormat, [ "yyyy-MM-dd",
            "MM/dd/yyyy HH:mm",
            "dd-MMM-yyyy"]);
    }
    private static string[] DefaultDateFormat(string dateFormat)
    {
        return DefaultFormat(dateFormat, ["yyyy-MM-dd", "dd/MM/yyyy"]);
    }
    private static string[] DefaultTimeFormat(string timeFormat)
    {
        return DefaultFormat(timeFormat, ["HH:mm:ss", "HHmmss"]);
    }

    private static string[] DefaultFormat(string format, string[] defaultFormats) =>
        string.IsNullOrWhiteSpace(format) ? defaultFormats : [format];
}



public class FWParseException : Exception
{
    public FWParseException(string message) : base(message) { }
    public FWParseException(string message, Exception innerException) : base(message, innerException) { }

}

public class FWFile<T> where T : new()
{
    record Column<T>
    {
        internal FWFieldAttribute Attribute;
        internal FieldInfo FieldInfo;

        internal Action<string, T> ReadData;
        internal Column<T> Validate()
        {
            // Accepted types: int, decimal, float, bool, string, datetime, date, time
            var ft = FieldInfo.FieldType;
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
            FieldInfo.SetValue(record, data);
        }
        internal void ReadInt(string data, T record)
        {
            if (!int.TryParse(data, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
            {
                throw new FWParseException($"Failed to parse value '{data}' to int field '{FieldInfo.Name}");
            }
            FieldInfo.SetValue(record, i);
        }
        internal void ReadFloat(string data, T record)
        {
            if (!float.TryParse(data, NumberStyles.Float, CultureInfo.InvariantCulture, out var f))
            {
                throw new FWParseException($"Failed to parse value '{data}' to float field '{FieldInfo.Name}");
            }
            FieldInfo.SetValue(record, f);
        }

        internal void ReadBool(string data, T record)
        {
            if (!bool.TryParse(data, out var b))
            {
                b = Attribute.BoolTrueValues.Contains(data.ToLowerInvariant());
            }
            FieldInfo.SetValue(record, b);
        }
        internal void ReadDecimal(string data, T record)
        {
            if (!decimal.TryParse(data, NumberStyles.Number, CultureInfo.InvariantCulture, out var d))
            {
                throw new FWParseException($"Failed to parse value '{data}' to Decimal field '{FieldInfo.Name}'");
            }
            FieldInfo.SetValue(record, d);
        }

        internal void ReadDateTime(string data, T record)
        {
            foreach (var template in Attribute.DateTimeFormats)
            {
                if (DateTime.TryParseExact(data, template, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                {
                    FieldInfo.SetValue(record, d);
                    return;
                }
            }
            throw new FWParseException($"Failed to parse value '{data}' to DateTime field '{FieldInfo.Name}");
        }
        internal void ReadDate(string data, T record)
        {
            foreach (var template in Attribute.DateFormats)
            {
                if (DateOnly.TryParseExact(data, template, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                {
                    FieldInfo.SetValue(record, d);
                    return;
                }
            }
            throw new FWParseException($"Failed to parse value '{data}' to DateOnly field '{FieldInfo.Name}");
        }
        internal void ReadTime(string data, T record)
        {
            foreach (var template in Attribute.TimeFormats)
            {
                if (TimeOnly.TryParseExact(data, template, CultureInfo.InvariantCulture, DateTimeStyles.None, out var t))
                {
                    FieldInfo.SetValue(record, t);
                    return;
                }
            }
            throw new FWParseException($"Failed to parse value '{data}' to TimeOnly field '{FieldInfo.Name}");
        }
    }
    private List<Column<T>> columns = [];
    private int rowLength = 0;
    public FWFile()
    {
        ValidateFields();
    }

    private void ValidateFields()
    {
        Type typeInfo = typeof(T);


        // Retrieve the fields of the class
        foreach (var field in typeInfo.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            var attribute = field.GetCustomAttribute<FWFieldAttribute>();
            if (attribute is null) continue;

            if (string.IsNullOrWhiteSpace(attribute.Header))
            {
                attribute.Header = field.Name;
            }
            rowLength += attribute.Length;

            columns.Add(new Column<T> { Attribute = attribute, FieldInfo = field }.Validate());
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
        columns.ForEach(c =>
        {
            var data = row.Substring(index, c.Attribute.Length);
            index += c.Attribute.Length;
            c.ReadData(data.Trim(), record);
        });
        return record;
    }

    public IEnumerable<T> ParseStream(Stream stream)
    {
        using var reader = new StreamReader(stream);
        var index = 0;
        string data;
        T e;
        while (true)
        {
            index++;
            data = reader.ReadLine();
            if (data is null) break; // End of stream
            try
            {
                e = ParseRow(data);
            }
            catch (Exception ex)
            {
                throw new FWParseException($"failed parsing line #{index} : '{data}' : {ex.Message}", ex);
            }
            yield return e;
        }
    }

}
