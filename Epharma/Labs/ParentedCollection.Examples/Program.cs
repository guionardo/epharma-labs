// Exemplos de JSON → instâncias com ParentedCollection.
// Execute: dotnet run --project Epharma/Labs/ParentedCollection.Examples
// Todos os dados são fictícios.

using System.Text.Json;
using Epharma.Labs.ParentedCollection;
using Epharma.Labs.ParentedCollection.Examples;
using PriceTable = Epharma.Labs.ParentedCollection.ParentCollection<
    Epharma.Labs.ParentedCollection.Examples.ProductPrice,
    Epharma.Labs.ParentedCollection.Examples.PriceSource>;

DefaultKeys();
SnakeCaseKeys();
CustomKeys();
ChangeAndSerializeBack();
StrictRejectsInvalidPayload();

// 1. Chaves padrão: os nomes do C# viram as chaves do JSON.
static void DefaultKeys()
{
    const string json = """
        {
          "Metadata": { "Laboratory": "Laboratório Alfa", "Currency": "BRL", "ValidFrom": "2026-11-01" },
          "Items": [
            { "Value": { "Sku": "ALF-001", "Description": "Analgésico 500 mg", "Price": 12.90 } },
            { "Value": { "Sku": "ALF-002", "Description": "Antitérmico 200 mg", "Price": 8.50 } },
            {
              "Value": { "Sku": "BET-010", "Description": "Antialérgico 10 mg", "Price": 21.00 },
              "Metadata": { "Laboratory": "Laboratório Beta", "Currency": "BRL", "ValidFrom": "2026-10-15" }
            }
          ]
        }
        """;

    var table = JsonSerializer.Deserialize(json, DefaultJsonContext.Default.ParentCollectionProductPricePriceSource)!;

    Print("1. Chaves padrão", table);
}

// 2. API externa em snake_case: a naming policy do contexto vale para o parent e para os itens.
static void SnakeCaseKeys()
{
    const string json = """
        {
          "metadata": { "laboratory": "Laboratório Alfa", "currency": "BRL", "valid_from": "2026-11-01" },
          "items": [
            { "value": { "sku": "ALF-001", "description": "Analgésico 500 mg", "price": 12.90 } },
            {
              "value": { "sku": "GAM-300", "description": "Vitamina C 1 g", "price": 15.75 },
              "metadata": { "laboratory": "Laboratório Gama", "currency": "BRL", "valid_from": "2026-11-10" }
            }
          ]
        }
        """;

    var table = JsonSerializer.Deserialize(json, SnakeCaseJsonContext.Default.ParentCollectionProductPricePriceSource)!;

    Print("2. snake_case (naming policy)", table);
}

// 3. Chaves livres para a coleção ("origem", "itens", "produto") e camelCase no restante.
//    Os nomes da coleção vêm do helper; os dos seus tipos vêm da policy ou de [JsonPropertyName].
static void CustomKeys()
{
    const string json = """
        {
          "origem": { "laboratory": "Laboratório Alfa", "currency": "BRL", "validFrom": "2026-11-01" },
          "itens": [
            { "produto": { "sku": "ALF-001", "description": "Analgésico 500 mg", "price": 12.90 } },
            {
              "produto": { "sku": "DEL-050", "description": "Protetor gástrico 20 mg", "price": 33.40 },
              "origem": { "laboratory": "Laboratório Delta", "currency": "BRL", "validFrom": "2026-11-05" }
            }
          ]
        }
        """;

    var options = CamelCaseJsonContext.Default.CreateOptions(new()
    {
        Metadata = "origem",
        Items = "itens",
        Value = "produto",
    });

    var table = JsonSerializer.Deserialize<PriceTable>(json, options)!;

    Print("3. Chaves customizadas (CreateOptions)", table);

    // A volta usa as mesmas chaves, e o item que herda sai sem "origem".
    // Acentos saem como \uXXXX: é o encoder padrão (seguro para HTML), não perda de dado.
    Console.WriteLine("   JSON de volta:");
    Console.WriteLine("   " + JsonSerializer.Serialize(table, options));
    Console.WriteLine();
}

// 4. Instâncias criadas a partir do JSON continuam ligadas ao parent:
//    trocar a origem padrão afeta só os itens que herdam.
static void ChangeAndSerializeBack()
{
    const string json = """
        {
          "Metadata": { "Laboratory": "Laboratório Alfa", "Currency": "BRL", "ValidFrom": "2026-11-01" },
          "Items": [
            { "Value": { "Sku": "ALF-001", "Description": "Analgésico 500 mg", "Price": 12.90 } },
            {
              "Value": { "Sku": "BET-010", "Description": "Antialérgico 10 mg", "Price": 21.00 },
              "Metadata": { "Laboratory": "Laboratório Beta", "Currency": "BRL", "ValidFrom": "2026-10-15" }
            }
          ]
        }
        """;

    var table = JsonSerializer.Deserialize(json, DefaultJsonContext.Default.ParentCollectionProductPricePriceSource)!;

    table.Metadata = table.Metadata! with { ValidFrom = new DateOnly(2026, 12, 1) };
    table.Items.Add(new() { Value = new ProductPrice("ALF-003", "Xarope 120 ml", 18.20m) });

    Print("4. Alteração após desserializar", table);
}

// 5. JSON externo com chave duplicada: as opções estritas rejeitam, em vez de usar o último valor.
static void StrictRejectsInvalidPayload()
{
    const string json = """
        {
          "Items": [
            {
              "Value": { "Sku": "ALF-001", "Description": "Analgésico 500 mg", "Price": 12.90 },
              "Metadata": { "Laboratory": "Laboratório Alfa", "Currency": "BRL", "ValidFrom": "2026-11-01" },
              "Metadata": { "Laboratory": "Laboratório Falso", "Currency": "USD", "ValidFrom": "2026-11-01" }
            }
          ]
        }
        """;

    Console.WriteLine("== 5. Leitura estrita");
    try
    {
        JsonSerializer.Deserialize(json, StrictJsonContext.Default.ParentCollectionProductPricePriceSource);
        Console.WriteLine("   Aceito (não esperado).");
    }
    catch (JsonException ex)
    {
        Console.WriteLine($"   Rejeitado: {ex.Message}");
    }
    Console.WriteLine();
}

static void Print(string title, PriceTable table)
{
    Console.WriteLine($"== {title}");
    Console.WriteLine($"   Padrão: {Describe(table.Metadata)}");
    foreach (var item in table.Items)
    {
        // Metadata == null indica herança; EffectiveMetadata é o valor que o negócio usa.
        var origin = item.Metadata is null ? "herdado" : "próprio";
        Console.WriteLine($"   {item.Value!.Sku,-8} {item.Value.Price,8:N2}  {Describe(item.EffectiveMetadata)} ({origin})");
    }
    Console.WriteLine();
}

static string Describe(PriceSource? source) =>
    source is null ? "(sem origem)" : $"{source.Laboratory}, {source.Currency}, desde {source.ValidFrom:dd/MM/yyyy}";
