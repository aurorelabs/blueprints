# Application settings

`appsettings.json` is the contract between the service and whoever deploys it. It is bound once at startup into a single
`AppSettings` object, validated, and passed to the `Add*`/`Use*` methods that need a piece of it.

## How loading works

```csharp
builder
    .ConfigureAuroreSerializers()
    .LoadAppSettings(out var appSettings)      // bind + validate, or the app does not start
    .AddAuroreTelemetry(appSettings.AppInformation, appSettings.Telemetry);
```

- **Strict binding.** `GetStrict<T>()` (`foundation/AspNetCore/Configuration/ConfigurationBindingExtensions.cs`) fails on
  keys that do not map to a property. A typo (`Telemtry:Otlp:Endpoint`) is a startup error instead of a silent default.
  `Logging` and framework keys such as `AllowedHosts` are read by the framework, not by `AppSettings`.
- **Validation.** `AppSettingsValidator` (`src/Core`) composes FluentValidation validators from the foundation packages
  (`IdentityProviderOptionsValidator`, `TelemetryOptionsValidator`, `HttpDependencyOptionsValidator`) and adds app rules.
  Errors carry the configuration key path (`Telemetry:Otlp:Endpoint`), so the message tells you what to edit.
- **No DI registration.** `AppSettings` is deliberately *not* in the container. Configuration flows through method
  parameters, which keeps it away from code that has no business reading it.
- **Failure = exit code 78.** Any exception while building the app returns `ExitCodes.ConfigurationError` (sysexits
  `EX_CONFIG`). Runtime failures return 70. Orchestrators can tell "bad config, do not restart-loop" from "crashed".

## Conventions

- **Time is always in seconds.** The name says so: `CacheSeconds`, `TimeoutSeconds`, `RetryAfterSeconds`. Milliseconds exist
  only at the OpenTelemetry SDK boundary (`ToMilliseconds` in `TelemetryConfiguration`).
- **Booleans**: `Enabled` for switches, `Critical` for "failure makes the app unhealthy".
- **Sizes carry the unit**: `MaxRequestBodySizeBytes`.
- **One level of grouping where it is cohesive.** `RequestPipeline` groups the HTTP-pipeline concerns, `Telemetry:Otlp`
  groups the exporter. Nest when the children share a lifecycle or are read together; keep flat when they are
  independent. Nesting is a judgment call, neither forbidden nor a goal. Remember each level adds a `__` to the
  environment-variable name.

## Layout

| Section | Purpose |
| --- | --- |
| `AllowedHosts` | Framework host filtering. `*` in development; set it in production if the app is reachable directly. |
| `AppInformation` | Domain/Boundary/Component, description, maintainer (name, email, optional URL), locale. Feeds telemetry resource attributes and the API documents. |
| `ApiDocs` | `OAuthClientId`: the public client the docs UI uses to log in. |
| `ApiKeys` | Header name and the clients allowed to use API-key auth, with roles and scopes. |
| `HealthChecks` | `CacheSeconds`: how long a health result is reused. |
| `HttpDependencies` | One entry per upstream service: base URL, headers, upstream auth (API key or client credentials), health check. |
| `IdentityProvider` | OIDC authority, audience, scopes, `RequireHttpsMetadata`. |
| `Logging` | Standard `Logging:LogLevel`, read by the framework. |
| `RequestPipeline` | CORS, forwarded headers, idempotency, Kestrel limits, maintenance mode, rate limiting, compression, security headers, timeouts. |
| `Telemetry` | OTLP exporter, resource attributes, logging/metrics/tracing tunables. See [telemetry](./telemetry.md). |

## Environments

`appsettings.json` holds **development-friendly defaults** (localhost URLs, dev secrets). `appsettings.Production.json`
overrides only what must differ and documents, in comments, what production *must* provide.

In Production the validators refuse to start the app while a development default is in effect: local URLs for the
identity provider, telemetry collector, CORS origins and upstream dependencies; plain HTTP; API keys shorter than 32
characters. Upstream secrets (`ClientSecret`, `ApiKey:Value`) are only checked for being non-empty, because a placeholder
secret cannot be told apart from a real one.

### Overriding with environment variables

Nested keys use a double underscore:

```
Telemetry__Otlp__Endpoint=http://otel-collector:4317
RequestPipeline__MaintenanceMode__Enabled=true
RequestPipeline__CorsPolicies__Default__AllowedOrigins__0=https://app.example.com
```

Arrays are overridden by index and merged with lower layers: an empty array in an override does **not** clear the values
from `appsettings.json`. That is why production origins must be provided rather than "emptied".

### Secrets

Do not put real secrets in `appsettings*.json`. Supply them through environment variables or an encrypted file
(SOPS + age) decrypted at deploy time.

## How-to: add a setting

1. Add the property to the options record (`foundation/Core/...`, or `src/Core` for app-only settings) with a sensible default.
2. Add the key to `appsettings.json` with a development-safe value.
3. Add a rule to the options validator (`> 0` for durations, absolute URL for URLs, and so on).
4. Use it where needed, passing it as a parameter. Do not inject `AppSettings`.
5. Run the tests: `AppSettingsFileTests` loads the real `appsettings.json` strictly and checks that Production refuses dev defaults.

## How-to: add a setting group

Create the options record, add it as a property of `AppSettings`, compose its validator in `AppSettingsValidator` using
`Custom` so errors keep the full key path, and add the section to `appsettings.json`.
