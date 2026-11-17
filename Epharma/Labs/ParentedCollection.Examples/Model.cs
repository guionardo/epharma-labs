using System.Text.Json.Serialization;

namespace Epharma.Labs.ParentedCollection.Examples;

// Domínio fictício: tabela de preços enviada por um laboratório parceiro.
// O PriceSource do parent vale para todos os produtos, exceto os que trazem o próprio.

/// <summary>Price of one product.</summary>
public sealed record ProductPrice(string Sku, string Description, decimal Price);

/// <summary>Source of a price: who sent it, currency and start date.</summary>
public sealed record PriceSource(string Laboratory, string Currency, DateOnly ValidFrom);

// Um contexto por convenção de nomes. Em produção, normalmente há só um.

/// <summary>Default names (PascalCase, as in the C# code).</summary>
[JsonSerializable(typeof(ParentCollection<ProductPrice, PriceSource>))]
internal partial class DefaultJsonContext : JsonSerializerContext;

/// <summary>snake_case names, common in external APIs.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(ParentCollection<ProductPrice, PriceSource>))]
internal partial class SnakeCaseJsonContext : JsonSerializerContext;

/// <summary>camelCase names, used with custom names for the collection keys.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ParentCollection<ProductPrice, PriceSource>))]
internal partial class CamelCaseJsonContext : JsonSerializerContext;

/// <summary>Strict reading for JSON from outside: no duplicate or unknown keys.</summary>
[JsonSourceGenerationOptions(
    AllowDuplicateProperties = false,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    RespectNullableAnnotations = true)]
[JsonSerializable(typeof(ParentCollection<ProductPrice, PriceSource>))]
internal partial class StrictJsonContext : JsonSerializerContext;
