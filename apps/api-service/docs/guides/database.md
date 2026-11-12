# Database

The database is PostgreSQL through `Aurore.Foundation.EntityFrameworkCore.Postgres`; read its
[guide](https://github.com/aurorelabs/foundation/blob/main/docs/guides/entity-framework-postgres.md) for the conventions (snake case, the `app` schema, soft delete,
timestamps, unique violations). This page is about this service; its model is described in the [blog guide](./blog.md).

## Settings

The `Postgres` section of `appsettings.json` is a map of Npgsql keywords. The local values point at a database called
`blueprints-api-service` on `127.0.0.1`. Real environments set them with environment variables, the password above all:

```
Postgres__Host=db.internal.example.com
Postgres__Password=<secret>
```

Production refuses to start with a local host.

## Where things go

| What | Where |
|---|---|
| The `DbContext` | `src/Infra/Contexts/AppDbContext.cs` |
| Entity configurations (`EntityConfigurationBase<T>`) | `src/Infra/Contexts/Configurations`, found by `ApplyConfigurationsFromAssembly` |
| Registration | `AddAppDatabase` in `src/Infra/Configuration/InfraConfiguration.cs`: the context, and the errors the unique indexes answer with |
| Entities | `src/Core/Domain/Entities`, deriving from `Entity` or `SoftDeletableEntity` |
| Migrations | `tools/Tools.Migrations/History` (generated; style rules do not apply to it) |

## Migrations

The tool project is only for the EF tools. The connection string is the first argument after `--`; the database of the sandbox is on `127.0.0.1:5432` (the `Database` name is the one the application uses, `blueprints-api-service`, which the first update creates):

```
dotnet ef migrations add InitialDb --project tools/Tools.Migrations --output-dir History -- "Host=127.0.0.1;Database=blueprints-api-service;Username=postgres;Password=postgres"
# or, with the Makefile (CONNECTION overrides the connection string): make migration-add AddComments, make db-update, make migration-remove, make migration-script, make migration-check, make db-drop
dotnet ef database update --project tools/Tools.Migrations -- "<same connection string>"
dotnet ef migrations remove --project tools/Tools.Migrations -- "<same connection string>"
```

The history table is `history.migrations`. For now the service does not apply migrations when it starts: the tool does.

Entities and configurations change with migrations: after changing one, add a migration, and `dotnet ef migrations has-pending-model-changes --project tools/Tools.Migrations -- "<connection string>"`
tells whether one is missing (it exits with an error when the model and the migrations disagree; a build step can run it). The tests of the features and of the API create their
database from the model, not from the migrations.
