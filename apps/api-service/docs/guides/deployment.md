# Running it: the image, the settings and what sits in front

## The image

`Dockerfile` (next to this repository's solution) builds the service with the SDK image and runs it on the ASP.NET image, as the non-root user `app`, on port 8080
(the port of the .NET images). The **build context is the folder that holds both repositories side by side**, because the projects reference the foundation by path
until its packages are published; inside the image the layout is the same as on disk (`/src/foundation`, `/src/blueprints/apps/api-service`), so the references
resolve. `Dockerfile.dockerignore` keeps everything that is not part of the build out of the context.

```
docker build -f blueprints/apps/api-service/Dockerfile -t blueprints-api-service .      # from the folder that holds the repositories
```

When the foundation is consumed as packages, the `COPY foundation/` line and the layout requirement go away.

## Settings

The image has no configuration of its own: `appsettings.json` is the development one, and everything that differs is an environment variable (`__` for nesting). In a container,
the ones that matter:

| Variable | Why |
|---|---|
| `Postgres__Host`, `Postgres__Database`, `Postgres__Username`, `Postgres__Password` | Where the database is. The password only from the environment or a secret store. |
| `IdentityProvider__AuthorityUrl` | The identity provider as the tokens name it (the issuer). A container must resolve that name: in the sandbox `auth.localhost` is an alias of nginx on the shared network. |
| `RequestPipeline__ForwardedHeaders__KnownProxies__0` or `__KnownNetworks__0` | The proxy (or its network) whose `X-Forwarded-*` headers are trusted. Without it the client address is the proxy's. |
| `RequestPipeline__Privacy__Keys__Current` | The key that encrypts personal data in logs and traces (see the foundation's privacy guide). Required in Production. |
| `Telemetry__Otlp__Endpoint` | The collector. |
| `ASPNETCORE_ENVIRONMENT` | `Production` makes the service refuse to start while a development value is still in effect (local URLs, short keys, no privacy key). |

The database schema is not applied when the service starts; the migrations tool does it ([database](./database.md)).

## What sits in front

The service speaks plain HTTP and expects a reverse proxy (nginx) to terminate TLS, set the forwarded headers and own CORS and compression limits. The proxy must pass the
headers below, and the service must be told to trust it (the setting above):

```
proxy_set_header Host $host;
proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
proxy_set_header X-Forwarded-Proto $scheme;
proxy_set_header X-Forwarded-Host $host;
```

## Health and stopping

- `/health/live` for a liveness probe, `/health/ready` for readiness (PostgreSQL is a critical dependency), `/health/status` for the detail (needs a caller). The ASP.NET image has no
  `curl`; a container health check can use bash: `bash -c 'exec 3<>/dev/tcp/localhost/8080 && ...'`.
- The service waits `Hosting:ShutdownTimeoutSeconds` (10) for requests in flight when it stops, so the orchestrator must give it at least that (`stop_grace_period: 15s`).

## The sandbox

On a development machine it runs against the sandbox (`.sandbox` in the folder that holds the repositories), the infrastructure shared by every project: PostgreSQL, Keycloak and nginx on the
network `aur-sandbox`. This repository brings its own `docker-compose.yaml` (service `sv-blueprints-api-service`, behind `http://api.localhost`, tokens from `http://auth.localhost:9999`): `docker compose up -d --build`.
