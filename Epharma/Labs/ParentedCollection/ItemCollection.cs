using System.Collections.ObjectModel;

namespace Epharma.Labs.ParentedCollection;

/// <summary>
/// Keeps the items of a <see cref="ParentCollection{TValue, TMeta}"/>
/// and sets <see cref="Item{TValue, TMeta}.Parent"/> for each item.
/// </summary>
/// <remarks>
/// An item can be in one collection only, one time.
/// Remove the item from its collection before you add it to a different collection.
/// </remarks>
/// <exception cref="InvalidOperationException">The item is already in a collection.</exception>
/// <exception cref="ArgumentNullException">The item is <see langword="null"/>.</exception>
public sealed class ItemCollection<TValue, TMeta> : Collection<Item<TValue, TMeta>>
    where TMeta : class
{
    private readonly ParentCollection<TValue, TMeta> _owner;

    internal ItemCollection(ParentCollection<TValue, TMeta> owner) => _owner = owner;

    // Todas as formas de entrada (Add, Insert, indexador e desserialização)
    // passam por estes overrides, então o vínculo com o parent nunca fica para trás.
    /// <inheritdoc />
    protected override void InsertItem(int index, Item<TValue, TMeta> item)
    {
        Attach(item);
        base.InsertItem(index, item);
    }

    /// <inheritdoc />
    protected override void SetItem(int index, Item<TValue, TMeta> item)
    {
        var previous = this[index];

        // Reatribuir o mesmo item é no-op; sem isso, Attach rejeitaria o item por já ter parent.
        if (ReferenceEquals(previous, item)) return;

        Attach(item);
        base.SetItem(index, item);
        previous.Parent = null;
    }

    /// <inheritdoc />
    protected override void RemoveItem(int index)
    {
        this[index].Parent = null;
        base.RemoveItem(index);
    }

    /// <inheritdoc />
    protected override void ClearItems()
    {
        foreach (var item in this) item.Parent = null;
        base.ClearItems();
    }

    private void Attach(Item<TValue, TMeta> item)
    {
        ArgumentNullException.ThrowIfNull(item);

        // Rejeita também item já presente nesta coleção: com duplicata, remover uma
        // ocorrência zeraria o Parent da outra e a herança quebraria em silêncio.
        if (item.Parent is not null)
            throw new InvalidOperationException("The item is already in a collection. Remove it first.");

        item.Parent = _owner;
    }
}
