# Telemetry (OpenTelemetry)

The service emits **logs, metrics and traces** over OTLP to a single endpoint: an OpenTelemetry **Collector**. The service
never talks to Loki, Tempo, Prometheus and friends directly. Where the data goes is configured in the Collector, later,
without redeploying the service.

```csharp
builder.AddAuroreTelemetry(appSettings.AppInformation, appSettings.Telemetry);
```

Code: `foundation/Telemetry/Configuration/TelemetryConfiguration.cs`.

## What is exported

- **Resource attributes** on every signal: service name/namespace/version/instance, environment, domain, boundary,
  maintainer, plus anything in `Telemetry:ResourceAttributes`.
- **Logs**: `ILogger` output through OTLP, with formatted message, scopes and parsed state values (configurable). A console
  logger stays on as well so logs remain visible if the collector is down: single-line in Development, JSON elsewhere.
- **Metrics**: ASP.NET Core, HttpClient, runtime and process instrumentation, plus the `Npgsql` meter.
- **Traces**: ASP.NET Core and HttpClient instrumentation (exceptions recorded, error status on exception), the app's
  activity source, the infrastructure source and `Npgsql`.

At startup `TelemetryStartupLogger` logs the protocol, endpoint and sampling ratio, so a wrong collector address is visible
in the first log lines.

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

`BatchExport` = `MaxQueueSize` (2048), `MaxExportBatchSize` (512, must be <= queue), `ScheduledDelaySeconds` (5),
`ExportTimeoutSeconds` (30). `TelemetryOptionsValidator` checks everything at startup.

### Protocol and endpoint (gRPC vs HTTP)

`Protocol` is `Grpc` or `HttpProtobuf`.

- **gRPC** (port 4317): the endpoint is used as-is (`http://collector:4317`).
- **HTTP/protobuf** (port 4318): each signal needs its own path. When the endpoint is set in code the SDK does **not**
  append them, so the service does (`BuildOtlpEndpoint`): `/v1/traces`, `/v1/metrics`, `/v1/logs`. Configure only the base
  (`http://collector:4318`). Verified against a real listener: without the suffix the data went to the wrong URL.

Production rejects a localhost collector.

### Sampling

`Tracing:SamplingRatio` (0..1) is **head sampling**: the decision is taken when a trace starts, by trace-id ratio, and
respects the parent's decision (`ParentBased`), so a trace is kept or dropped whole across services.

- Keep `1.0` while volume is low. Lower it when cost or volume demands.
- Head sampling cannot know whether a trace will turn out slow or failed. To "keep all errors and slow traces, sample the
  rest", leave the service at `1.0` (or a high ratio) and do **tail sampling in the Collector**.

### Excluded paths

Health probes (`/health/...`) are never traced, whatever is configured. `ExcludedIncomingPaths` and `ExcludedOutgoingPaths`
add more prefixes (case-insensitive `StartsWith`). Empty lists are fine.

## How-to: add a custom metric

```csharp
// one Meter per component, named after the app (static, created once)
public static class AppMetrics
{
    public static readonly Meter Meter = new("Aurore.Blueprints.ApiService");
    public static readonly Counter<long> OrdersCreated = Meter.CreateCounter<long>("orders.created");
}

// at the call site
AppMetrics.OrdersCreated.Add(1, new KeyValuePair<string, object?>("channel", "web"));
```

Then register the meter name in `ConfigureMetrics`: `.AddMeter("Aurore.Blueprints.ApiService")`. A meter that is not
registered is silently ignored.

## How-to: add a trace source

```csharp
private static readonly ActivitySource Source = new("Aurore.Orders.Checkout");

using var activity = Source.StartActivity("ProcessOrder");
activity?.SetTag("order.id", id);
```

Register it in `ConfigureTracing`: `.AddSource("Aurore.Orders.Checkout")`. Use one source per library or business flow
(for example `Aurore.Foundation.Messaging`). `StartActivity` returns `null` when nothing listens, so always use `?.`.

## Local debugging

Set `Metrics:ExportToConsole` / `Tracing:ExportToConsole` to `true` to print signals to the console without a collector.

## Units

Settings are in seconds. The SDK takes milliseconds, so `ToMilliseconds` converts in one place; nothing else uses milliseconds.
