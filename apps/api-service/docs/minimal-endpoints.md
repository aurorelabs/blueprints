# Minimal endpoints

Endpoints are classes discovered by scanning the assembly. Two base types:

- `ResourceGroup`: one URL group (`/v1/examples`). Declares name, tags, **versions**, authorization and extra problems.
- `MinimalEndpoint<TGroup>`: one endpoint. Declares its version, summary, description and the actual `Map`.

```csharp
builder.AddAuroreMinimalEndpoints<Program>(out var endpoints);   // discover + validate -> catalog
// ...
app.MapAuroreMinimalEndpoints(endpoints, api => api.ProducesProblem(503));
```

The **catalog** is the single source of truth: mapping, API versioning policies and the Scalar documents are all driven by
it, so a version is declared in exactly one place (the resource group).

## A resource group

```csharp
internal sealed class ExamplesResource : ResourceGroup
{
    public override string Name => "examples";                       // default: class name minus "Resource", slug-cased
    public override string[] Tags => ["Examples"];
    public override bool RequireAuthorization => true;               // default; false = anonymous group
    public override MinimalEndpointVersion[] Versions => [1, new(2, true)];   // v2 is deprecated
    public override int[] Problems => [404, 409];                    // extra documented problems for this group

    public override void Configure(RouteGroupBuilder group) { /* conventions for this group */ }
}
```

`Versions` accepts plain ints (implicit conversion) or the full form:

```csharp
new(2, IsDeprecated: true, Sunset: new DateOnly(2027, 1, 1), Description: "Replaced by v3")
```

Deprecated versions are announced in the `api-deprecated-versions` header and flagged in the docs; `Sunset` is sent in the
`Sunset` header and requires the version to be deprecated.

`PrefixWithApi` makes the route `api/v{version}/name` instead of `v{version}/name`.

## An endpoint

```csharp
internal sealed class GetOrderEndpoint : MinimalEndpoint<OrdersResource>
{
    public override int Version => 1;                       // default 1; must be declared by the group
    public override string Summary => "Gets an order";      // default: class name, title-cased
    public override string? Description => "...";

    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
        => builder.MapGet("{id:guid}", Handle);
}
```

The route name defaults to the summary in PascalCase (unique across the app, validated). The same route can exist in
several versions: `GetLayersEndpoint` (v1) and `GetLayersV2Endpoint` (v2) both map `layers`.

## Convention layers

Conventions stack from the outside in; each level only adds.

| Level | Where | Applies to |
| --- | --- | --- |
| application | `MapAuroreMinimalEndpoints(catalog, api => ...)` | every endpoint |
| resource | `ResourceGroup.Problems` / `Configure` | every endpoint in the group |
| endpoint | inside `Map(...)` | that endpoint only |

`Examples.http` and `GetLayersEndpoint` demonstrate this with `X-Level-*` headers.

### Authorization

Groups require an authenticated caller by default, so a new endpoint without auth **fails closed**. Narrow it in
`Configure` (`group.RequireAuthorization(p => p.RequireAuroreScope("api.write"))`) or per endpoint. Requirements from every
level must pass. Opt out with `RequireAuthorization => false` (group) or `.AllowAnonymous()` (endpoint).

### Problem responses

`StandardProblems` (401, 403, 429, 500) plus a validation problem for 400 are documented on every endpoint. Replace the
global list by passing `globalProblems` to `MapAuroreMinimalEndpoints` (400-599, validated). Groups add theirs via
`Problems`; endpoints via `.ProducesProblem(...)`.

## What the catalog validates (startup failure if violated)

- Group names are unique.
- Versions are non-empty, greater than zero and distinct.
- Every endpoint targets a version its group declares; every declared version has at least one endpoint.
- Several groups may share a version number, but its deprecation, sunset and description must agree.
- A sunset requires the version to be deprecated.
- Route names are unique.

Groups and endpoints are created once at startup with parameterless constructors; handler parameters are bound per request
by the framework, so scanning has no per-request cost.

## How-to: add an endpoint

1. Create the class in `Resources/<Group>/Endpoints/`, inherit `MinimalEndpoint<YourResource>`.
2. Implement `Map`; set `Version`, `Summary`, `Description` if the defaults do not fit.
3. Done: no registration. The catalog picks it up and the docs show it.

## How-to: add a new API version

1. Add it to the group's `Versions` (mark the old one `new(1, true)` to deprecate it).
2. Create the endpoints with `Version => 2`. A version without endpoints fails startup.
3. The versioning policy and a new Scalar document appear automatically.
