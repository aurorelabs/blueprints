# Telemetry

Logs, metrics and traces are sent over OTLP to one OpenTelemetry Collector. The Collector decides where the data goes.

```csharp
builder.AddAuroreTelemetry(appSettings.AppInformation, appSettings.Telemetry);
```

Code: `foundation/Telemetry/Configuration/TelemetryConfiguration.cs`. At startup the protocol, endpoint and sampling ratio are logged.

## Settings

```json
"Telemetry": {
  "Otlp": { "Endpoint": "http://localhost:4317", "Protocol": "Grpc", "RequestTimeoutSeconds": 10 },
  "ResourceAttributes": {},
  "Logging": { "IncludeScopes": true, "ParseStateValues": true, "BatchExport": { } },
  "Metrics": { "ExportToConsole": false, "ExportIntervalSeconds": 60, "ExportTimeoutSeconds": 30 },
  "Tracing": { "SamplingRatio": 1.0, "ExcludedIncomingPaths": [], "ExcludedOutgoingPaths": [], "ExportToConsole": false, "BatchExport": { } }
}
```

`BatchExport`: `MaxQueueSize` 2048, `MaxExportBatchSize` 512 (not above the queue), `ScheduledDelaySeconds` 5, `ExportTimeoutSeconds` 30.

- **Protocol**: `Grpc` (port 4317) uses the endpoint as-is. `HttpProtobuf` (port 4318) needs `/v1/traces|metrics|logs`; the
  service appends them, so configure only the base URL.
- **Sampling**: `SamplingRatio` is head sampling (decided when the trace starts, parent-based). For "keep errors and slow
  traces", leave it at 1.0 and sample in the Collector.
- **Excluded paths**: `/health/` is never traced. Add more prefixes in the two `Excluded*` lists.
- **Local debugging**: `ExportToConsole` prints signals without a collector.
- Production rejects a localhost endpoint.

## Add a custom metric

```csharp
public static class AppMetrics
{
    public static readonly Meter Meter = new("Aurore.Blueprints.ApiService");
    public static readonly Counter<long> OrdersCreated = Meter.CreateCounter<long>("orders.created");
}

AppMetrics.OrdersCreated.Add(1, new KeyValuePair<string, object?>("channel", "web"));
```

Register the name in `ConfigureMetrics`: `.AddMeter("Aurore.Blueprints.ApiService")`. Unregistered meters are ignored.

## Add a trace source

```csharp
private static readonly ActivitySource Source = new("Aurore.Orders.Checkout");

using var activity = Source.StartActivity("ProcessOrder");   // null when nobody listens
activity?.SetTag("order.id", id);
```

Register it in `ConfigureTracing`: `.AddSource("Aurore.Orders.Checkout")`.
