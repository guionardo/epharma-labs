using System.Text.Json.Serialization;

namespace Epharma.Labs.ParentedCollection;

/// <summary>
/// Holds default metadata and a list of items.
/// Items without own metadata use <see cref="Metadata"/>.
/// </summary>
/// <typeparam name="TValue">The type of the item value.</typeparam>
/// <typeparam name="TMeta">The type of the metadata. Use an immutable type, because items share this instance.</typeparam>
/// <remarks>
/// For System.Text.Json source generation, add one <c>[JsonSerializable]</c> attribute
/// for each closed generic type, for example <c>ParentCollection&lt;string, Meta&gt;</c>.
/// </remarks>
public sealed class ParentCollection<TValue, TMeta> where TMeta : class
{
    /// <summary>Creates an empty collection without metadata.</summary>
    public ParentCollection() => Items = new ItemCollection<TValue, TMeta>(this);

    /// <summary>
    /// Gets or sets the default metadata for the items.
    /// A change is visible immediately in all items without own metadata.
    /// </summary>
    public TMeta? Metadata { get; set; }

    /// <summary>Gets the items of this collection.</summary>
    // Populate faz o desserializador preencher esta instância via Add, em vez de
    // criar outra lista; assim cada item passa por InsertItem e recebe o parent.
    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public ItemCollection<TValue, TMeta> Items { get; }
}
