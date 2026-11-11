# fwfile

Biblioteca .NET para leitura e escrita de arquivos de largura fixa (fixed-width).

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

## Performance

Benchmark (100k linhas, release):

| Método | Tempo | Alocação |
|---|---|---|
| ParseStream | ~85 ms | ~40 MB |
| ParseStreamAsync | ~93 ms | ~47 MB |

Rode com `dotnet run -c Release --project Benchmarks`.
