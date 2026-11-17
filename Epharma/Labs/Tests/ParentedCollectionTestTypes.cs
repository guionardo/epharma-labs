namespace Epharma.Labs.Tests;

using System.Text.Json.Serialization;
using Epharma.Labs.ParentedCollection;

public sealed record TestMeta(string Source, string Version);

// Source generation: cada tipo genérico fechado usado em JSON precisa do próprio atributo.
[JsonSerializable(typeof(ParentCollection<string, TestMeta>))]
internal partial class ParentedCollectionJsonContext : JsonSerializerContext;

// Mesmo contexto com as opções estritas do .NET 10, para JSON vindo de fora.
[JsonSourceGenerationOptions(
    AllowDuplicateProperties = false,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    RespectNullableAnnotations = true)]
[JsonSerializable(typeof(ParentCollection<string, TestMeta>))]
internal partial class ParentedCollectionStrictJsonContext : JsonSerializerContext;

// Policies que alteram caixa: provam que a chave do metadata segue a policy nos dois níveis.
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseUpper)]
[JsonSerializable(typeof(ParentCollection<string, TestMeta>))]
internal partial class ParentedCollectionSnakeUpperJsonContext : JsonSerializerContext;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ParentCollection<string, TestMeta>))]
internal partial class ParentedCollectionCamelJsonContext : JsonSerializerContext;
