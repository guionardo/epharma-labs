using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Epharma.Labs.ParentedCollection;

/// <summary>
/// Defines custom JSON property names for <see cref="ParentCollection{TValue, TMeta}"/>
/// and <see cref="Item{TValue, TMeta}"/>.
/// Keep a property <see langword="null"/> to use the default name (with the naming policy, if set).
/// </summary>
/// <remarks>
/// <see cref="Metadata"/> applies to the parent and to the items.
/// Thus the metadata key is always the same at the two levels.
/// </remarks>
public sealed record ParentedCollectionJsonNames
{
    /// <summary>Gets the JSON name of the metadata property, in the parent and in the items.</summary>
    public string? Metadata { get; init; }

    /// <summary>Gets the JSON name of the items list in the parent.</summary>
    public string? Items { get; init; }

    /// <summary>Gets the JSON name of the value property in the items.</summary>
    public string? Value { get; init; }
}

/// <summary>
/// Extensions to configure the JSON contract of the parented collection types.
/// </summary>
public static class ParentedCollectionJson
{
    /// <summary>
    /// Creates serializer options from <paramref name="context"/> with custom JSON property names
    /// for the parented collection types.
    /// The options keep the settings of the context, for example the naming policy.
    /// </summary>
    /// <param name="context">The source generation context.</param>
    /// <param name="names">The custom names.</param>
    /// <returns>New options. Use them in <c>JsonSerializer.Serialize</c> and <c>JsonSerializer.Deserialize</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="names"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A name in <paramref name="names"/> is empty or white space.</exception>
    /// <example>
    /// <code>
    /// var options = AppJsonContext.Default.CreateOptions(new() { Metadata = "meta", Items = "itens" });
    /// var parent = JsonSerializer.Deserialize&lt;ParentCollection&lt;Product, Meta&gt;&gt;(json, options);
    /// </code>
    /// </example>
    public static JsonSerializerOptions CreateOptions(
        this JsonSerializerContext context, ParentedCollectionJsonNames names)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Copia as opções do contexto: um JsonSerializerOptions novo, só com o contexto
        // como resolver, perderia a naming policy e as demais [JsonSourceGenerationOptions].
        return new JsonSerializerOptions(context.Options)
        {
            TypeInfoResolver = context.WithPropertyNames(names),
        };
    }

    /// <summary>
    /// Returns a resolver that uses <paramref name="names"/> as the JSON property names
    /// of the parented collection types. Other types do not change.
    /// </summary>
    /// <remarks>
    /// For a <c>JsonSerializerContext</c>, use <see cref="CreateOptions"/>.
    /// If you use this method, the options that you create do not get the settings of the context.
    /// </remarks>
    /// <param name="resolver">The source resolver, for example a <c>DefaultJsonTypeInfoResolver</c>.</param>
    /// <param name="names">The custom names.</param>
    /// <returns>A new resolver. Set it in <c>JsonSerializerOptions.TypeInfoResolver</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="resolver"/> or <paramref name="names"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A name in <paramref name="names"/> is empty or white space.</exception>
    /// <example>
    /// <code>
    /// var options = new JsonSerializerOptions(JsonSerializerOptions.Web)
    /// {
    ///     TypeInfoResolver = new DefaultJsonTypeInfoResolver().WithPropertyNames(new() { Metadata = "meta" }),
    /// };
    /// </code>
    /// </example>
    public static IJsonTypeInfoResolver WithPropertyNames(
        this IJsonTypeInfoResolver resolver, ParentedCollectionJsonNames names)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(names);
        ThrowIfBlank(names.Metadata, nameof(names.Metadata));
        ThrowIfBlank(names.Items, nameof(names.Items));
        ThrowIfBlank(names.Value, nameof(names.Value));

        return resolver.WithAddedModifier(typeInfo => Rename(typeInfo, names));
    }

    private static void Rename(JsonTypeInfo typeInfo, ParentedCollectionJsonNames names)
    {
        if (!typeInfo.Type.IsGenericType) return;

        var definition = typeInfo.Type.GetGenericTypeDefinition();
        var isParent = definition == typeof(ParentCollection<,>);
        var isItem = definition == typeof(Item<,>);
        if (!isParent && !isItem) return;

        // O nome que chega aqui já passou pela naming policy; converte o nome C#
        // da mesma forma para achar a propriedade sem depender de reflection (AOT).
        var policy = typeInfo.Options.PropertyNamingPolicy;
        string JsonName(string clrName) => policy?.ConvertName(clrName) ?? clrName;

        var renames = new Dictionary<string, string?>
        {
            [JsonName(nameof(ParentCollection<object, object>.Metadata))] = names.Metadata,
        };
        if (isParent) renames[JsonName(nameof(ParentCollection<object, object>.Items))] = names.Items;
        if (isItem) renames[JsonName(nameof(Item<object, object>.Value))] = names.Value;

        foreach (var property in typeInfo.Properties)
        {
            if (renames.TryGetValue(property.Name, out var newName) && newName is not null)
                property.Name = newName;
        }
    }

    private static void ThrowIfBlank(string? name, string paramName)
    {
        // null é válido (mantém o nome padrão); vazio quase sempre é erro de configuração.
        if (name is not null && string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("The JSON property name must not be empty.", paramName);
    }
}
