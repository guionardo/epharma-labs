namespace fwfile;

public class FWParseException : Exception
{
    public FWParseException(string message) : base(message) { }
    public FWParseException(string message, Exception innerException) : base(message, innerException) { }
}