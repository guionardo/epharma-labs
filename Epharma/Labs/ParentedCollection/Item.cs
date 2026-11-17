using System.Text.Json.Serialization;

namespace Epharma.Labs.ParentedCollection;

/// <summary>
/// Holds a value and optional metadata.
/// When the item has no metadata, it uses the metadata of its parent collection.
/// </summary>
/// <typeparam name="TValue">The type of the item value.</typeparam>
/// <typeparam name="TMeta">The type of the metadata. Use an immutable type, because items share the parent instance.</typeparam>
/// <example>
/// <code>
/// var parent = new ParentCollection&lt;string, Meta&gt; { Metadata = new Meta("default") };
/// parent.Items.Add(new() { Value = "a" });
/// var meta = parent.Items[0].EffectiveMetadata; // Meta("default")
/// </code>
/// </example>
public sealed class Item<TValue, TMeta> where TMeta : class
{
    /// <summary>
    /// Gets the collection that contains this item.
    /// The value is <see langword="null"/> when the item is not in a collection.
    /// </summary>
    // Fora do JSON para evitar referência circular; a coleção reconstrói o vínculo.
    [JsonIgnore]
    public ParentCollection<TValue, TMeta>? Parent { get; internal set; }

    /// <summary>Gets or sets the item value.</summary>
    public TValue? Value { get; set; }

    /// <summary>
    /// Gets or sets the metadata of this item only.
    /// Set <see langword="null"/> to use the parent metadata.
    /// </summary>
    // Mesmo nome C# do parent e sem [JsonPropertyName]: qualquer naming policy gera
    // a mesma chave nos dois níveis. Só o valor próprio é serializado, então o herdado
    // não se repete por item e o round-trip preserva "definido" x "herdado".
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TMeta? Metadata { get; set; }

    /// <summary>
    /// Gets the effective metadata: <see cref="Metadata"/> if it has a value,
    /// else the metadata of <see cref="Parent"/>.
    /// The value is <see langword="null"/> when neither has metadata.
    /// </summary>
    [JsonIgnore]
    public TMeta? EffectiveMetadata => Metadata ?? Parent?.Metadata;
}
