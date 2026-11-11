namespace fwfile;

/// <summary>Exception thrown when parsing a fixed-width file fails.</summary>
public class FWParseException : Exception
{
    /// <summary>Initializes a new instance.</summary>
    public FWParseException(string message) : base(message) { }
    /// <summary>Initializes a new instance with inner exception.</summary>
    public FWParseException(string message, Exception innerException) : base(message, innerException) { }
}