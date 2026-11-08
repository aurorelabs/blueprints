# JSON serialization

One set of JSON rules is used everywhere: HTTP request/response bodies, `JsonSerializerConfiguration.Serialize/Deserialize`
and anything that calls `JsonSerializerConfiguration.Default()`. The rules live in
`foundation/Core/Configuration/JsonSerializerConfiguration.cs` (`Configure`), so there is exactly one place to change them.

## Rules

| Setting | Value | Why |
| --- | --- | --- |
| Property names | camelCase | Matches what JavaScript/TypeScript clients expect. |
| Name matching on input | case-insensitive | Clients that send `OrderId` or `orderId` both work. |
| Enums | strings (`JsonStringEnumConverter`) | Readable payloads; reordering an enum does not break clients. |
| Trailing commas | allowed | Friendlier for hand-written requests and files. |
| Reference cycles | ignored (`IgnoreCycles`) | A cyclic graph serializes instead of throwing. |
| Indentation | off | Smaller payloads. `SerializeToFile` turns it on for files. |
| Encoder | `UnsafeRelaxedJsonEscaping` | Non-ASCII text and characters like `+` are not escaped to `\uXXXX`. Fine for JSON APIs; never embed the output in HTML. |

`Converters.Clear()` runs before the enum converter is added, so the converter list is deterministic.

## Wiring

```csharp
builder.ConfigureAuroreSerializers();
```

This configures ASP.NET Core's `JsonOptions` (minimal API request/response bodies) with the same `Configure` method.
It is the first call in the builder chain in `Program.cs`.

## Using it outside HTTP

```csharp
var json = order.Serialize();
var back = json.Deserialize<Order>();

order.SerializeToFile("order.json");             // indented by default
var loaded = "order.json".DeserializeFromFile<Order>(throwOnError: false);

await stream.SerializeAsync(order);
var value = await stream.DeserializeAsync<Order>();
```

Each call builds a fresh options instance from `Default()`. Pass an `Action<JsonSerializerOptions>` to tweak a single call;
the tweak never leaks into other calls.
