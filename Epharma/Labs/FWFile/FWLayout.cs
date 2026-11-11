using System.Text.Json;

namespace Epharma.Labs.FWFile;

/// <summary>Configuration-driven fixed-width layout (no compiled class required).</summary>
public class FWLayout
{
    public List<FWFieldDefinition> Fields { get; } = new();

    public int RowLength => Fields.Count == 0 ? 0 : Fields.Max(f => f.Start + f.Length);

    public FWLayout Field(string name, int length, FWFieldType type = FWFieldType.String, int start = -1,
        string format = "", string culture = "", bool trim = true, char padChar = ' ', bool rightAlign = false)
    {
        if (start < 0) start = RowLength;
        Fields.Add(new FWFieldDefinition
        {
            Name = name,
            Length = length,
            Type = type,
            Start = start,
            Format = format,
            Culture = culture,
            Trim = trim,
            PadChar = padChar,
            RightAlign = rightAlign,
        });
        return this;
    }

    public static FWLayout FromJson(string json)
    {
        var layout = new FWLayout();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var dtos = JsonSerializer.Deserialize<List<FieldDto>>(json, options) ?? new();
        foreach (var dto in dtos)
        {
            var type = FWFieldType.String;
            if (!string.IsNullOrWhiteSpace(dto.Type))
                type = Enum.Parse<FWFieldType>(dto.Type, ignoreCase: true);
            layout.Field(dto.Name ?? "", dto.Length, type, dto.Start, dto.Format ?? "", dto.Culture ?? "", dto.Trim, dto.PadChar, dto.RightAlign);
        }
        return layout;
    }

    private sealed class FieldDto
    {
        public string? Name { get; set; }
        public int Length { get; set; }
        public string? Type { get; set; }
        public int Start { get; set; } = -1;
        public string? Format { get; set; }
        public string? Culture { get; set; }
        public bool Trim { get; set; } = true;
        public char PadChar { get; set; } = ' ';
        public bool RightAlign { get; set; }
    }
}