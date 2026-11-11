namespace Epharma.Labs.FWFile;

/// <summary>Exception thrown when parsing a fixed-width file fails.</summary>
public class FWParseException : Exception
{
    /// <summary>Line number (1-based) where the failure occurred, when available.</summary>
    public int? LineNumber { get; set; }

    /// <summary>Field name where the failure occurred, when available.</summary>
    public string? FieldName { get; set; }

    /// <summary>Starting position (0-based) of the field in the row, when available.</summary>
    public int? Position { get; set; }

    /// <summary>Initializes a new instance.</summary>
    public FWParseException(string message) : base(message) { }

    /// <summary>Initializes a new instance with inner exception.</summary>
    public FWParseException(string message, Exception innerException) : base(message, innerException) { }
}