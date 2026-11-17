namespace Epharma.Labs.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using Epharma.Labs.ParentedCollection;

public class ParentedCollectionJsonNamesTests
{
    private static ParentCollection<string, TestMeta> Sample()
    {
        var parent = new ParentCollection<string, TestMeta> { Metadata = new TestMeta("default", "1") };
        parent.Items.Add(new() { Value = "a" });
        parent.Items.Add(new() { Value = "b", Metadata = new TestMeta("custom", "2") });
        return parent;
    }

    private static JsonSerializerOptions Options(ParentedCollectionJsonNames names) =>
        ParentedCollectionJsonContext.Default.CreateOptions(names);

    [Fact]
    public void Naming_policy_produces_same_metadata_key_in_parent_and_items()
    {
        var json = JsonSerializer.Serialize(Sample(), ParentedCollectionSnakeUpperJsonContext.Default.ParentCollectionStringTestMeta);
        var root = JsonNode.Parse(json)!;

        Assert.NotNull(root["METADATA"]);
        Assert.NotNull(root["ITEMS"]![1]!["METADATA"]);
    }

    [Fact]
    public void Custom_names_replace_default_keys()
    {
        var options = Options(new() { Metadata = "meta", Items = "itens", Value = "valor" });

        var root = JsonNode.Parse(JsonSerializer.Serialize(Sample(), options))!;

        Assert.NotNull(root["meta"]);
        Assert.Equal("a", (string?)root["itens"]![0]!["valor"]);
        Assert.NotNull(root["itens"]![1]!["meta"]);
        Assert.Null(root["Metadata"]);
    }

    [Fact]
    public void Custom_names_do_not_change_metadata_type_keys()
    {
        var options = Options(new() { Metadata = "meta" });

        var root = JsonNode.Parse(JsonSerializer.Serialize(Sample(), options))!;

        // As chaves de TMeta são do tipo do consumidor e não devem ser tocadas.
        Assert.Equal("default", (string?)root["meta"]!["Source"]);
    }

    [Fact]
    public void Custom_names_deserialize_into_linked_instances()
    {
        const string json = """
            {"meta":{"Source":"default","Version":"1"},
             "itens":[{"valor":"a"},{"valor":"b","meta":{"Source":"custom","Version":"2"}}]}
            """;

        var parent = JsonSerializer.Deserialize<ParentCollection<string, TestMeta>>(json, Options(new() { Metadata = "meta", Items = "itens", Value = "valor" }))!;

        Assert.Equal(2, parent.Items.Count);
        Assert.All(parent.Items, item => Assert.Same(parent, item.Parent));
        Assert.Equal(new TestMeta("default", "1"), parent.Items[0].EffectiveMetadata);
        Assert.Equal(new TestMeta("custom", "2"), parent.Items[1].EffectiveMetadata);
    }

    [Fact]
    public void Create_options_keeps_context_naming_policy_for_null_names()
    {
        var options = ParentedCollectionCamelJsonContext.Default.CreateOptions(new() { Items = "itens" });

        var root = JsonNode.Parse(JsonSerializer.Serialize(Sample(), options))!;

        Assert.NotNull(root["metadata"]);
        Assert.Equal("a", (string?)root["itens"]![0]!["value"]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Blank_name_throws(string name)
    {
        Assert.Throws<ArgumentException>(() =>
            ParentedCollectionJsonContext.Default.WithPropertyNames(new() { Metadata = name }));
    }

    [Fact]
    public void Resolver_overload_renames_with_explicit_options()
    {
        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = ParentedCollectionJsonContext.Default.WithPropertyNames(new() { Value = "valor" }),
        };

        var root = JsonNode.Parse(JsonSerializer.Serialize(Sample(), options))!;

        Assert.Equal("a", (string?)root["Items"]![0]!["valor"]);
    }

    [Fact]
    public void Null_arguments_throw()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ParentedCollectionJsonContext.Default.WithPropertyNames(null!));
        Assert.Throws<ArgumentNullException>(() =>
            ParentedCollectionJsonContext.Default.CreateOptions(null!));
        Assert.Throws<ArgumentNullException>(() =>
            ParentedCollectionJson.CreateOptions(null!, new()));
    }
}
