# FWFile

Biblioteca .NET para leitura e escrita de arquivos de largura fixa (fixed-width), com suporte a múltiplos tipos de registro, validação, layout dinâmico e convenções legado (pt-BR).

## Uso básico

```csharp
public class SampleFW
{
    [FWField(4)]  public int Id;
    [FWField(20)] public string Name = default!;
    [FWField(10)] public decimal Price;
}

var f = new FWFile<SampleFW>();
using var stream = File.OpenRead("data.txt");
foreach (var row in f.ParseStream(stream))
    Console.WriteLine(row);
```

## Funcionalidades

- Tipos suportados: `string`, `int`, `long`, `short`, `byte`, `float`, `double`, `decimal`, `bool`, `char`, `Guid`, `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, `TimeSpan`, enums e seus nullable.
- Formatos customizáveis por campo (data/hora), valores booleanos, trim, alinhamento, padding.
- Leitura síncrona (`ParseStream`) e assíncrona (`ParseStreamAsync`).
- Escrita (`Write`/`WriteAsync`/`Format`).
- Opções: `skipHeader`, `maxRows`, `lenient`, `encoding`, `CancellationToken`, `Order`.
- `DefaultValue`: valor usado quando a conversão do campo falhar (com aviso em modo de validação).
- Múltiplos tipos de registro no mesmo arquivo via `FWMultiFile` (discriminador posicional).
- Modo de validação (`ParseStreamWithValidation`/`Async`) com `FWParseResult<T>` e regras `Required`, `AllowedValues`, `Regex`, `Min`, `Max` e validador customizado.
- Layout dinâmico via `FWLayout` (fluent ou JSON) e `FWDynamicFile`, sem classe compilada.
- Convenções legado: decimal implícito (`Decimals`), `Start` posicional, datas zeradas tratadas como nulas, formatos sem separador (`yyyyMMdd`, `ddMMyyyy`), encoding legado (Latin-1/Windows-1252 via `CodePagesEncodingProvider`), numéricos alinhados à direita com zero à esquerda.
- Segurança: mensagens de erro não expõem a linha/valor bruto; dados disponíveis via propriedades da `FWParseException`.

## Exemplos

### Múltiplos tipos de registro

```csharp
var multi = new FWMultiFile()
    .Record<Header>("0")
    .Record<Detail>("1")
    .Record<Trailer>("9")
    .TrailerValidator(records => { /* valida totais */ });

foreach (var rec in multi.ParseStream(stream))
    Console.WriteLine(rec.GetType().Name);
```

### Validação acumulada

```csharp
var f = new FWFile<Row>();
foreach (var r in f.ParseStreamWithValidation(stream))
{
    if (!r.IsValid)
        foreach (var issue in r.Issues)
            Console.WriteLine(issue);
}
```

### Layout dinâmico (JSON)

```csharp
var json = "[{\"Name\":\"Id\",\"Length\":4,\"Type\":\"Int\"},{\"Name\":\"Name\",\"Length\":10,\"Type\":\"String\"}]";
var layout = FWLayout.FromJson(json);
var file = new FWDynamicFile(layout);
foreach (var rec in file.ParseStream(stream))
    Console.WriteLine(rec["Id"]);
```

### Convenções legado

```csharp
public class Legacy
{
    [FWField(6, Decimals = 2)] public decimal Amount;
    [FWField(8, Start = 6, DateTimeFormat = "yyyyMMdd")] public DateTime? Date;
}
```

## Performance

Benchmark (100k linhas, release):

| Método | Tempo | Alocação |
|---|---|---|
| ParseStream | ~85 ms | ~40 MB |
| ParseStreamAsync | ~93 ms | ~47 MB |

Rode com `dotnet run -c Release --project Epharma/Labs/Benchmarks`.