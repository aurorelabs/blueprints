# Adding a feature

The path a new use case takes through this service, using the blog as the example (see [the blog](./blog.md)). Each step names where the code goes.

1. **The model.** A new entity goes in `src/Core/Domain/Entities`, deriving from `Entity` (timestamps) or `SoftDeletableEntity` (also soft delete). Add its `DbSet` to
   `AppDbContext` and a configuration class in `src/Infra/Contexts/Configurations` (`EntityConfigurationBase<T>`: lengths, indexes, relationships). Name the unique
   indexes with constants.
2. **The migration.** `dotnet ef migrations add <Name> --project tools/Tools.Migrations --output-dir History -- "<connection string>"`, then read it: names in snake case,
   the indexes you expect. See [database](./database.md).
3. **The errors.** Expected failures are `ApplicationError` values in `AppErrors` (`src/Core/Errors`), one line each, with a stable code. A unique index that clients can
   collide with gets its error registered in `AddAppDatabase`.
4. **The feature.** One class per use case in `src/Features/<Area>`: a request, a response and the logic in `ExecuteAsync`, returning a `Result`. Business rules that read
   the database are `FeatureValidator`s of the request, so the feature only sees valid requests. Lists go through `ToPageAsync` or `ToCursorPageAsync` with a `SortMap`,
   and project with object initializers (see the [pagination guide](https://github.com/aurorelabs/foundation/blob/main/docs/guides/pagination.md)). Features are found and
   registered by their base class, with the decorators chosen in `AddAppFeatures`.
5. **The endpoint.** A class in `src/App.Api/Resources/<Area>`: it validates the shape of the body with a `RequestValidator`, takes who is calling from the token
   (`ToCaller()`), calls the feature and turns the `Result` into the response (`ToHttpResult()`). It states who may call it (`RequireRoles`, `RequireScopes`) and what it
   documents (`ProducesPage`, `ProducesCursorPage`). **No business rule goes in an endpoint.**
6. **The tests.** The feature on a real PostgreSQL in `tests/Tests.Features` (a case per rule, plus the failures), and the HTTP side in `tests/Tests.Integration` (who may call it,
   the status codes, the shape of the answers) with real tokens. See [testing](../instructions/testing.md).
7. **The docs.** What it does for a client, in the guide of its area.

The `Demo` folder of `App.Api` is for examples of the platform only; nothing of the business lives there.
