using System.Globalization;

namespace fwfile;

/// <summary>Describes a fixed-width field.</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public class FWFieldAttribute : Attribute
{
    public FWFieldAttribute(int length, string header = "", string? boolTrueValues = null, string dateTimeFormat = "", string dateFormat = "", string timeFormat = "", bool trim = true, char padChar = ' ', bool rightAlign = false, int order = 0)
    {
        Length = length;
        Header = header;
        Trim = trim;
        PadChar = padChar;
        RightAlign = rightAlign;
        Order = order;
        boolTrueValues ??= "true,1,y,s";
        BoolTrueValues = new(boolTrueValues.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries), StringComparer.InvariantCultureIgnoreCase);
        DateTimeFormats = DefaultDateTimeFormat(dateTimeFormat);
        DateFormats = DefaultDateFormat(dateFormat);
        TimeFormats = DefaultTimeFormat(timeFormat);
    }

    public int Length { get; private set; }

    public string Header { get; internal set; }

    public readonly HashSet<string> BoolTrueValues;

    public readonly string[] DateTimeFormats;

    public readonly string[] DateFormats;

    public readonly string[] TimeFormats;

    public bool Trim { get; private set; } = true;

    public char PadChar { get; private set; } = ' ';

    public bool RightAlign { get; private set; }

    public int Order { get; private set; }

    private static string[] DefaultDateTimeFormat(string dateTimeFormat)
    {
        return DefaultFormat(dateTimeFormat, [ "yyyy-MM-dd", "MM/dd/yyyy HH:mm", "dd-MMM-yyyy"]);
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