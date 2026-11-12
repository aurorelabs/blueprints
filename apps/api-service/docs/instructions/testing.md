# Testing

The tests of the foundation packages live with them, in the foundation solution (see its
[testing guide](https://github.com/aurorelabs/foundation/blob/main/docs/instructions/testing.md)). This page is about the tests of this service.

## What we test

Our own logic, never the framework or a package: a test that would still pass if our code were deleted is not worth keeping.
Each test has explicit `// Arrange`, `// Act` and `// Assert` sections and a `DisplayName` that reads as a rule. Larger scenario sets use
`TheoryData` with `MemberData`, not long `InlineData` lists. Test folders mirror the source folders, and
`--filter-trait "Feature=Idempotency"` runs one feature.

| Project | What it holds |
|---|---|
| `tests/Tests` | The real `appsettings.json` files: they bind strictly and are valid. |
| `tests/Tests.Features` | The features of the blog on a real PostgreSQL, wired as the application wires them. |
| `tests/Tests.Integration` | The application through `WebApplicationFactory`: pipeline, OpenAPI documents, API keys, **real Keycloak tokens**, and the blog from the HTTP side (real Postgres and tokens together). |
| `tests/Tests.Support` | Shared helpers, no tests: `TestDatabase` creates a database for the test class on the shared PostgreSQL. |

## What the tests need

The tests that touch the database or authentication run against the real thing, because a fake would only prove that the fake works (how the queries
translate to SQL, the claims Keycloak really writes, how roles and scopes reach the application). **They assume the infrastructure of the sandbox is up**: the
environment shared by every project of the machine, with one PostgreSQL, one Keycloak and nginx (`.sandbox/docker-compose.yaml` in the folder that holds the
repositories). Nothing for the tests is started from this repository.

What the tests rely on:

- **PostgreSQL** on `127.0.0.1:5432`, user and password `postgres`. Each test class creates a database of its own on it (`blueprints_<guid>`, with the schema made from
  the model) and drops it afterwards.
- **Keycloak** at `http://auth.localhost:9999`, realm `aurorelabs`: the client `api-tests` (password grant), the audience `api`, the scopes `api.read` and `api.write`,
  and the users `alice`/`alice` (reader), `bob`/`bob` (admin and editor), `carol`/`carol` and `dave`/`dave` (readers; carol never gets an author profile).

Without them these tests fail with a message that says what to start (they do not skip: a silent skip hides regressions).

**Docker inside WSL on Windows:** WSL stops idle distributions after a few seconds, taking dockerd and the containers with it.
Keep a WSL session open while the tests run.

**`localhost` and IPv6:** the ports docker publishes from WSL are not forwarded on IPv6, and `localhost` names resolve to `::1` first, so a connection
can hang for about twenty seconds before falling back. The Keycloak tests use an IPv4-only handler (`Ipv4Handler`) and the database tests the address 127.0.0.1 for that reason; do the same if you write a client
test against these services.

## Running

```
dotnet test --solution Blueprints.ApiService.slnx
dotnet test --project tests/Tests/Tests.csproj
docker compose --profile tests run --build --rm tests                    # everything in a Linux container, with coverage
make test                                                                # the same; make test-local, make report (opens the coverage report), make clean
```

Results go to `artifacts/test-results`. The container run is the `test` stage of the `Dockerfile`: it joins the `aur-sandbox` network, so PostgreSQL is `sbx-postgres`
(`TEST_POSTGRES_HOST`) and Keycloak is reached by the same `auth.localhost:9999` as from the host. The coverage report of our own code (the foundation is left out)
is `artifacts/coverage-report/index.html`.
