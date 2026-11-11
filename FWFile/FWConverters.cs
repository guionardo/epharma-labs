namespace fwfile;

/// <summary>Custom conversion between a fixed-width field string and a CLR value.</summary>
public interface IFWConverter
{
    object? Read(ReadOnlySpan<char> data, Type targetType);

    string Write(object? value, Type targetType);
}