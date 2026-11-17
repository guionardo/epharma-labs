# ParentedCollection

Coleção genérica em que cada item pode ter metadata própria; sem ela, o item usa a metadata da coleção (parent).
Namespace: `Epharma.Labs.ParentedCollection`. Parent e itens serializam em JSON com System.Text.Json (source generation, compatível com AOT).

## Uso

```csharp
var parent = new ParentCollection<string, Meta> { Metadata = new Meta("default", "1") };
parent.Items.Add(new() { Value = "a" });                                // herda
parent.Items.Add(new() { Value = "b", Metadata = new Meta("custom", "2") }); // próprio

parent.Items[0].Metadata;          // null: o item não tem metadata própria
parent.Items[0].EffectiveMetadata; // Meta("default", "1"): valor que o negócio usa
```

```csharp
[JsonSerializable(typeof(ParentCollection<string, Meta>))]
internal partial class AppJsonContext : JsonSerializerContext;

var parent = JsonSerializer.Deserialize(json, AppJsonContext.Default.ParentCollectionStringMeta);
```

## Customizar as chaves do JSON

| Preciso de... | Use |
|---|---|
| Mudar chaves dos meus tipos (`TValue`, `TMeta`) | `[JsonPropertyName]` no próprio tipo |
| Uma convenção para todas as chaves (camelCase, snake_case...) | `PropertyNamingPolicy` em `[JsonSourceGenerationOptions]` |
| Nomes livres nas chaves da coleção (`Metadata`, `Items`, `Value`) | `CreateOptions` com `ParentedCollectionJsonNames` |

```csharp
var options = AppJsonContext.Default.CreateOptions(new()
{
    Metadata = "origem", // vale para o parent e para os itens
    Items = "itens",
    Value = "produto",
});
var parent = JsonSerializer.Deserialize<ParentCollection<Product, Source>>(json, options);
```

- Nome `null` mantém o padrão, já com a naming policy do contexto.
- `CreateOptions` copia as opções do contexto. Não monte `new JsonSerializerOptions { TypeInfoResolver = contexto }` à mão: a naming policy do contexto se perde.
- Para resolvers sem contexto (ex.: `DefaultJsonTypeInfoResolver`), use `resolver.WithPropertyNames(names)`.

## Exemplos

O projeto [ParentedCollection.Examples](../ParentedCollection.Examples/Program.cs) desserializa JSON em instâncias com chaves padrão, snake_case, chaves customizadas, alteração após a leitura e leitura estrita.

```bash
dotnet run --project Epharma/Labs/ParentedCollection.Examples
```

## Decisões

- **Metadata efetiva é calculada, não copiada** (`Metadata ?? Parent?.Metadata`): alterar a metadata do parent reflete na hora nos itens que herdam.
- **Mesmo nome C# (`Metadata`) no parent e no item, sem `[JsonPropertyName]`**: qualquer naming policy gera a mesma chave nos dois níveis.
- **Só a metadata própria vai para o JSON** (omitida quando nula): o valor herdado não se repete por item e o round-trip preserva a diferença entre "definido" e "herdado".
- **`Parent` fica fora do JSON**: evita ciclo; na desserialização `Items` usa `JsonObjectCreationHandling.Populate`, então cada item passa por `InsertItem` e recebe o parent.
- **Um item pertence a uma coleção, uma vez**: adicionar item que já tem parent lança `InvalidOperationException`. Com duplicatas, remover uma ocorrência desvincularia a outra.
- **`TMeta : class`**: `null` significa "sem valor próprio". Prefira tipos imutáveis (`record`), pois os itens compartilham a instância do parent.
- **JSON de origem externa**: use `AllowDuplicateProperties = false` e `UnmappedMemberHandling.Disallow` (ver exemplo 5 e `ParentedCollectionStrictJsonContext` em `Tests`).

## Testes

```bash
dotnet test Epharma/Labs/Tests --filter "FullyQualifiedName~ParentedCollection"
```
