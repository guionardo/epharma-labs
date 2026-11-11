using System.Globalization;

namespace Epharma.Labs.FWFile;

/// <summary>Describes a fixed-width field.</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public class FWFieldAttribute : Attribute
{
    public FWFieldAttribute(int length, string header = "", string? boolTrueValues = null, string? boolFalseValues = null, string dateTimeFormat = "", string dateFormat = "", string timeFormat = "", bool trim = true, char padChar = ' ', bool rightAlign = false, int order = 0, string culture = "", Type? converterType = null, bool truncate = true)
    {
        Length = length;
        Header = header;
        Trim = trim;
        PadChar = padChar;
        RightAlign = rightAlign;
        Order = order;
        Culture = culture;
        ConverterType = converterType;
        Truncate = truncate;
        boolTrueValues ??= "true,1,y,s";
        boolFalseValues ??= "false,0,n,no,f";
        var trueTokens = boolTrueValues.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var falseTokens = boolFalseValues.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        BoolTrueValues = new(trueTokens, StringComparer.InvariantCultureIgnoreCase);
        BoolFalseValues = new(falseTokens, StringComparer.InvariantCultureIgnoreCase);
        BoolTrueValueTokens = trueTokens;
        BoolFalseValueTokens = falseTokens;
        DateTimeFormat = dateTimeFormat;
        DateFormat = dateFormat;
        TimeFormat = timeFormat;
    }

    public int Length { get; private set; }

    public string Header { get; internal set; }

    public readonly HashSet<string> BoolTrueValues;
    public readonly HashSet<string> BoolFalseValues;
    public readonly string[] BoolTrueValueTokens;
    public readonly string[] BoolFalseValueTokens;
    public bool Truncate { get; private set; } = true;

    private string _dateTimeFormat = "";
    private string _dateFormat = "";
    private string _timeFormat = "";

    public string DateTimeFormat
    {
        get => _dateTimeFormat;
        set { _dateTimeFormat = value; DateTimeFormats = DefaultDateTimeFormat(value); }
    }
    public string DateFormat
    {
        get => _dateFormat;
        set { _dateFormat = value; DateFormats = DefaultDateFormat(value); }
    }
    public string TimeFormat
    {
        get => _timeFormat;
        set { _timeFormat = value; TimeFormats = DefaultTimeFormat(value); }
    }

    public bool Required { get; set; }
    public string Regex { get; set; } = "";
    public string AllowedValues { get; set; } = "";
    public string Min { get; set; } = "";
    public string Max { get; set; } = "";

    public string[] DateTimeFormats { get; private set; } = Array.Empty<string>();

    public string[] DateFormats { get; private set; } = Array.Empty<string>();

    public string[] TimeFormats { get; private set; } = Array.Empty<string>();

    public bool Trim { get; private set; } = true;

    public char PadChar { get; private set; } = ' ';

    public bool RightAlign { get; private set; }

    public int Order { get; private set; }

    public string Culture { get; private set; } = "";

    public Type? ConverterType { get; private set; }

    /// <summary>Explicit start position (0-based). -1 means sequential (default).</summary>
    public int Start { get; set; } = -1;

    /// <summary>Number of decimal places for implicit decimal fields (e.g. 2 -> 12345 = 123.45).</summary>
    public int Decimals { get; set; }

    private bool _hasDefaultValue;
    private object? _defaultValue;
    /// <summary>Valor usado quando a conversão do campo falhar.</summary>
    public object? DefaultValue
    {
        get => _defaultValue;
        set { _defaultValue = value; _hasDefaultValue = true; }
    }
    public bool HasDefaultValue => _hasDefaultValue;

    private static string[] DefaultDateTimeFormat(string dateTimeFormat)
    {
        return DefaultFormat(dateTimeFormat, [ "yyyy-MM-dd", "dd/MM/yyyy", "yyyyMMdd", "ddMMyyyy"]);
    }

    private static string[] DefaultDateFormat(string dateFormat)
    {
        return DefaultFormat(dateFormat, ["yyyy-MM-dd", "dd/MM/yyyy", "yyyyMMdd", "ddMMyyyy"]);
    }

    private static string[] DefaultTimeFormat(string timeFormat)
    {
        return DefaultFormat(timeFormat, ["HH:mm:ss", "HHmmss"]);
    }

    private static string[] DefaultFormat(string format, string[] defaultFormats) =>
        string.IsNullOrWhiteSpace(format) ? defaultFormats : [format];
}