namespace Epharma.Labs.Tests;

using Epharma.Labs.ParentedCollection;

public class ParentedCollectionInheritanceTests
{
    private static readonly TestMeta Default = new("default", "1");
    private static readonly TestMeta Custom = new("custom", "2");

    private static ParentCollection<string, TestMeta> NewParent() => new() { Metadata = Default };

    [Fact]
    public void Item_without_own_metadata_uses_parent_metadata()
    {
        var parent = NewParent();
        parent.Items.Add(new() { Value = "a" });

        Assert.Same(Default, parent.Items[0].EffectiveMetadata);
    }

    [Fact]
    public void Own_metadata_overrides_parent_metadata()
    {
        var parent = NewParent();
        parent.Items.Add(new() { Value = "a", Metadata = Custom });

        Assert.Same(Custom, parent.Items[0].EffectiveMetadata);
    }

    [Fact]
    public void Parent_metadata_change_reaches_inheriting_items()
    {
        var parent = NewParent();
        parent.Items.Add(new() { Value = "a" });
        var changed = new TestMeta("changed", "3");

        parent.Metadata = changed;

        Assert.Same(changed, parent.Items[0].EffectiveMetadata);
    }

    [Fact]
    public void Clearing_own_metadata_falls_back_to_parent()
    {
        var parent = NewParent();
        var item = new Item<string, TestMeta> { Value = "a", Metadata = Custom };
        parent.Items.Add(item);

        item.Metadata = null;

        Assert.Same(Default, item.EffectiveMetadata);
    }

    [Fact]
    public void Item_outside_a_collection_has_only_own_metadata()
    {
        var item = new Item<string, TestMeta> { Value = "a" };

        Assert.Null(item.Parent);
        Assert.Null(item.EffectiveMetadata);
    }

    [Fact]
    public void Removed_item_stops_inheriting()
    {
        var parent = NewParent();
        var item = new Item<string, TestMeta> { Value = "a" };
        parent.Items.Add(item);

        parent.Items.Remove(item);

        Assert.Null(item.Parent);
        Assert.Null(item.EffectiveMetadata);
    }

    [Fact]
    public void Clear_detaches_all_items()
    {
        var parent = NewParent();
        var a = new Item<string, TestMeta> { Value = "a" };
        var b = new Item<string, TestMeta> { Value = "b" };
        parent.Items.Add(a);
        parent.Items.Add(b);

        parent.Items.Clear();

        Assert.Null(a.Parent);
        Assert.Null(b.Parent);
    }

    [Fact]
    public void Indexer_replacement_attaches_new_item_and_detaches_old_item()
    {
        var parent = NewParent();
        var oldItem = new Item<string, TestMeta> { Value = "old" };
        var newItem = new Item<string, TestMeta> { Value = "new" };
        parent.Items.Add(oldItem);

        parent.Items[0] = newItem;

        Assert.Null(oldItem.Parent);
        Assert.Same(parent, newItem.Parent);
    }

    [Fact]
    public void Indexer_with_same_item_keeps_the_link()
    {
        var parent = NewParent();
        var item = new Item<string, TestMeta> { Value = "a" };
        parent.Items.Add(item);

        parent.Items[0] = item;

        Assert.Same(parent, item.Parent);
    }

    [Fact]
    public void Adding_item_from_another_collection_throws()
    {
        var first = NewParent();
        var second = NewParent();
        var item = new Item<string, TestMeta> { Value = "a" };
        first.Items.Add(item);

        Assert.Throws<InvalidOperationException>(() => second.Items.Add(item));
        Assert.Same(first, item.Parent);
    }

    [Fact]
    public void Adding_same_item_twice_throws()
    {
        var parent = NewParent();
        var item = new Item<string, TestMeta> { Value = "a" };
        parent.Items.Add(item);

        Assert.Throws<InvalidOperationException>(() => parent.Items.Add(item));
        Assert.Single(parent.Items);
    }

    [Fact]
    public void Item_can_move_after_removal()
    {
        var first = NewParent();
        var second = new ParentCollection<string, TestMeta> { Metadata = Custom };
        var item = new Item<string, TestMeta> { Value = "a" };
        first.Items.Add(item);

        first.Items.Remove(item);
        second.Items.Add(item);

        Assert.Same(Custom, item.EffectiveMetadata);
    }

    [Fact]
    public void Adding_null_throws()
    {
        var parent = NewParent();

        Assert.Throws<ArgumentNullException>(() => parent.Items.Add(null!));
    }
}
