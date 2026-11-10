# Blueprints @ API Service

The reference .NET API service: the guideline every new service starts from.

```
foundation/   reusable packages: Core, AspNetCore, Telemetry, EntityFrameworkCore (+ foundation/tests)
src/          App.Api (host), Core (settings), Features, Infra
tests/        Tests (unit, real appsettings), Tests.Integration (WebApplicationFactory)
docs/         guides and how-tos
```

## Guides

- [JSON serialization](./docs/serialization.md): the single set of JSON rules and how to use it outside HTTP.
- [Application settings](./docs/app-settings.md): strict binding, validation, conventions, environments, overrides, how to add a setting.
- [Telemetry](./docs/telemetry.md): logs/metrics/traces over OTLP to a collector, protocols, sampling, custom metrics and sources.
- [Minimal endpoints](./docs/minimal-endpoints.md): resource groups, versions, convention layers, authorization, startup validation.
- [API documentation (Scalar)](./docs/scalar.md): a document per version, security schemes, OAuth2 + PKCE login.

## Running the tests

```
dotnet test --project foundation/tests/Tests.Core/Tests.Core.csproj
dotnet test --project foundation/tests/Tests.AspNetCore/Tests.AspNetCore.csproj
dotnet test --project foundation/tests/Tests.Telemetry/Tests.Telemetry.csproj
dotnet test --project tests/Tests/Tests.csproj
dotnet test --project tests/Tests.Integration/Tests.Integration.csproj
```

Filter by feature with `--filter-trait "Feature=Idempotency"`.

## Project documentation

- [Changelog](./CHANGELOG.md)
- [Contributing](./CONTRIBUTING.md)
- [Third-party notices](./NOTICE.md)
- [License](./LICENSE) ([MIT](https://mit-license.org))
