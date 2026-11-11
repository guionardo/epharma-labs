using System.Globalization;

namespace Epharma.Labs.FWFile;

/// <summary>Describes a single field in a configuration-driven layout.</summary>
public class FWFieldDefinition
{
    public string Name { get; set; } = "";
    public int Start { get; set; }
    public int Length { get; set; }
    public FWFieldType Type { get; set; } = FWFieldType.String;
    public string Format { get; set; } = "";
    public string Culture { get; set; } = "";
    public bool Trim { get; set; } = true;
    public char PadChar { get; set; } = ' ';
    public bool RightAlign { get; set; }

    public CultureInfo GetCulture() =>
        string.IsNullOrWhiteSpace(Culture) ? CultureInfo.InvariantCulture : new CultureInfo(Culture);
}