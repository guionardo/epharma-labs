namespace Epharma.Labs.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using Epharma.Labs.ParentedCollection;

public class ParentedCollectionSerializationTests
{
    private static ParentCollection<string, TestMeta> Sample()
    {
        var parent = new ParentCollection<string, TestMeta> { Metadata = new TestMeta("default", "1") };
        parent.Items.Add(new() { Value = "a" });
        parent.Items.Add(new() { Value = "b", Metadata = new TestMeta("custom", "2") });
        return parent;
    }

    [Fact]
    public void Serialization_writes_only_own_metadata()
    {
        var json = JsonSerializer.Serialize(Sample(), ParentedCollectionJsonContext.Default.ParentCollectionStringTestMeta);
        var items = JsonNode.Parse(json)!["Items"]!.AsArray();

        // Item herdado sai sem "Metadata": o valor efetivo não é duplicado no JSON.
        Assert.Null(items[0]!["Metadata"]);
        Assert.Equal("custom", (string?)items[1]!["Metadata"]!["Source"]);
    }

    [Fact]
    public void Serialization_does_not_write_parent_reference()
    {
        var json = JsonSerializer.Serialize(Sample(), ParentedCollectionJsonContext.Default.ParentCollectionStringTestMeta);

        Assert.DoesNotContain("Parent", json);
    }

    [Fact]
    public void Round_trip_restores_parent_link_and_inheritance()
    {
        var json = JsonSerializer.Serialize(Sample(), ParentedCollectionJsonContext.Default.ParentCollectionStringTestMeta);

        var back = JsonSerializer.Deserialize(json, ParentedCollectionJsonContext.Default.ParentCollectionStringTestMeta)!;

        Assert.All(back.Items, item => Assert.Same(back, item.Parent));
        Assert.Null(back.Items[0].Metadata);
        Assert.Equal(new TestMeta("default", "1"), back.Items[0].EffectiveMetadata);
        Assert.Equal(new TestMeta("custom", "2"), back.Items[1].EffectiveMetadata);
    }

    [Fact]
    public void Deserialization_handles_missing_items()
    {
        const string json = """{"Metadata":{"Source":"default","Version":"1"}}""";

        var back = JsonSerializer.Deserialize(json, ParentedCollectionJsonContext.Default.ParentCollectionStringTestMeta)!;

        Assert.Empty(back.Items);
    }

    [Fact]
    public void Strict_options_reject_duplicate_metadata_property()
    {
        const string json = """
            {"Items":[{"Value":"a",
              "Metadata":{"Source":"x","Version":"1"},
              "Metadata":{"Source":"y","Version":"2"}}]}
            """;

        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize(json, ParentedCollectionStrictJsonContext.Default.ParentCollectionStringTestMeta));
    }

    [Fact]
    public void Strict_options_reject_unknown_property()
    {
        const string json = """{"Items":[{"Value":"a","Unknown":1}]}""";

        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize(json, ParentedCollectionStrictJsonContext.Default.ParentCollectionStringTestMeta));
    }
}
