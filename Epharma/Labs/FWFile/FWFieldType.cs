namespace Epharma.Labs.FWFile;

/// <summary>Field type supported by a dynamic (configuration-driven) layout.</summary>
public enum FWFieldType
{
    String,
    Int,
    Long,
    Short,
    Byte,
    Float,
    Double,
    Decimal,
    Bool,
    Char,
    Guid,
    DateTime,
    DateTimeOffset,
    DateOnly,
    TimeOnly,
    TimeSpan,
}