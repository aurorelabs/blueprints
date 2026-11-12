# Blueprints @ API Service

The reference .NET API service: the guideline every new service starts from.

```
src/          App.Api (host), Core (settings, entities), Features, Infra
tools/        Tools.Migrations (the EF tools only)
tests/        Tests (real appsettings), Tests.Features, Tests.Integration (WebApplicationFactory), Tests.Support
Dockerfile, docker-compose.yaml    the image and its container (see docs/guides/deployment.md)
docs/         guides (how this service works), instructions (testing) and scripts
```

It is built on the [Aurore Foundation](https://github.com/aurorelabs/foundation/blob/main/README.md) packages, which live in a
solution of their own.

## Guides of this service

- [Application settings](./docs/guides/app-settings.md): strict binding, validation, conventions, environments, overrides, how to add a setting.
- [The blog](./docs/guides/blog.md): the domain (authors, posts, comments), who can do what, the endpoints, where the rules live.
- [Adding a feature](./docs/guides/adding-a-feature.md): the path of a new use case, from the entity to the tests.
- [Database](./docs/guides/database.md): settings, where things go, migrations.
- [Running it](./docs/guides/deployment.md): the image, the settings of a container, what sits in front, health and stopping, the sandbox.
- [Testing](./docs/instructions/testing.md): what we test, the services the tests need, how to run them.

## Foundation guides

They live with the packages, in the [foundation docs](https://github.com/aurorelabs/foundation/tree/main/docs/guides): errors and results,
feature decorators, pagination, Entity Framework Core on Postgres, authentication, the request pipeline, minimal endpoints, the API
documentation (Scalar), telemetry and JSON serialization; its testing instructions are in the same docs folder.

## Running

```
dotnet test --solution Blueprints.ApiService.slnx
```

The tests assume the infrastructure of the sandbox is up (PostgreSQL, Keycloak, nginx), see [testing](./docs/instructions/testing.md). To run the service itself as a container, see
[running it](./docs/guides/deployment.md).

Filter by feature with `--filter-trait "Feature=Idempotency"`.


## Project documentation

- [Changelog](../../CHANGELOG.md)
- [Third-party notices](../../NOTICE.md)
- [License](../../LICENSE) ([MIT](https://mit-license.org))
