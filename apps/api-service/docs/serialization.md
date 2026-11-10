# JSON serialization

One set of JSON rules for HTTP bodies and for `JsonSerializerConfiguration`. Defined in
`foundation/Core/Configuration/JsonSerializerConfiguration.cs`.

| Setting | Value |
| --- | --- |
| Property names | camelCase |
| Input name matching | case-insensitive |
| Enums | strings |
| Trailing commas | allowed |
| Reference cycles | ignored |
| Indentation | off (on for `SerializeToFile`) |
| Encoder | `UnsafeRelaxedJsonEscaping`: do not embed the output in HTML |

## Use

```csharp
builder.ConfigureAuroreSerializers();      // HTTP request/response bodies

var json = order.Serialize();              // outside HTTP
var order = json.Deserialize<Order>();
order.SerializeToFile("order.json");
```

Each call starts from `Default()`; the optional `Action<JsonSerializerOptions>` only affects that call.
