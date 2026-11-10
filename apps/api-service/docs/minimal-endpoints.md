# Minimal endpoints

- `ResourceGroup`: one URL group (`/v1/examples`): name, tags, versions, authorization, extra problems.
- `MinimalEndpoint<TGroup>`: one endpoint: version, summary, description, `Map`.

```csharp
builder.AddAuroreMinimalEndpoints<Program>(out var endpoints);   // discover + validate
app.MapAuroreMinimalEndpoints(endpoints, api => api.ProducesProblem(503));
```

The catalog is the single source of truth for mapping, API versioning and the Scalar documents.

## Resource group

```csharp
internal sealed class ExamplesResource : ResourceGroup
{
    public override string Name => "examples";
    public override string[] Tags => ["Examples"];
    public override MinimalEndpointVersion[] Versions => [1, new(2, IsDeprecated: true, Sunset: new DateOnly(2027, 1, 1))];
    public override int[] Problems => [404, 409];
    public override void Configure(RouteGroupBuilder group) { }
}
```

Defaults: name is the class name without `Resource`, requires authorization, version 1, route `v{version}/{name}`
(`PrefixWithApi` makes it `api/v{version}/{name}`). A deprecated version is flagged in headers and docs; `Sunset` needs
`IsDeprecated`; `Description` is shown in the docs.

## Endpoint

```csharp
internal sealed class GetOrderEndpoint : MinimalEndpoint<OrdersResource>
{
    public override int Version => 1;
    public override string Summary => "Gets an order";

    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder) => builder.MapGet("{id:guid}", Handle);
}
```

No registration needed. The same route can exist in several versions.

## Conventions stack

| Level | Where |
| --- | --- |
| application | `MapAuroreMinimalEndpoints(catalog, api => ...)` |
| group | `ResourceGroup.Problems` / `Configure` |
| endpoint | inside `Map` |

Authorization: groups require an authenticated caller by default. Narrow it in `Configure`
(`group.RequireScopes("api.write")`) or per endpoint; all levels must pass. Opt out with
`RequireAuthorization => false` or `.AllowAnonymous()`.

Problems: 401, 403, 429, 500 (`StandardProblems`) and a 400 validation problem are documented on every endpoint. Pass
`globalProblems` to replace the list.

## Startup validation

The app fails to start when: group names or route names repeat; versions are empty, zero or duplicated; an endpoint targets an
undeclared version; a declared version has no endpoint; groups sharing a version disagree on deprecation, sunset or
description; a sunset is set on a non-deprecated version.

## Add a version

Add it to the group's `Versions` (mark the old one deprecated), create endpoints with `Version => 2`. The Scalar document
appears automatically.
