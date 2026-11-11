namespace Epharma.Labs.FWFile;

public enum FWValidationSeverity
{
    Warning,
    Error,
}

/// <summary>Issue found while validating/parsing a fixed-width record.</summary>
public record FWValidationIssue(
    int Line,
    string? Field,
    int Position,
    FWValidationSeverity Severity,
    string Message);

/// <summary>Result of parsing/validating a single fixed-width row.</summary>
public record FWParseResult<T>(int Line, T? Record, IReadOnlyList<FWValidationIssue> Issues)
{
    public bool IsValid => Issues.All(i => i.Severity != FWValidationSeverity.Error);
}