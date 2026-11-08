# API documentation (OpenAPI + Scalar)

Interactive docs are served by [Scalar](https://scalar.com) on top of ASP.NET Core's built-in OpenAPI generator.
**Not available in Production**: `AddAppScalar`/`UseAppScalar` return early there, so documents and UI are not exposed.

Code: `foundation/AspNetCore/OpenApi/Extensions/ScalarExtensions.cs`, wiring in `src/App.Api/Configuration/ScalarConfiguration.cs`.

## One document per version

For every `MinimalEndpointVersion` in the catalog there is an OpenAPI document named from the same format used by API
versioning (`v1`, `v2`; `ScalarExtensions.DocumentName`), so a document can never drift from the versions that are
actually mapped. Scalar shows them in a version dropdown.

Each document gets:

- title, version, description (from `AppInformation`) and contact (maintainer name, email and optional URL);
- for a deprecated version, a deprecation notice (and the sunset date, if any);
- the version's own description, when set on the version.

## Security schemes

Declared per document: JWT bearer, API key, OpenID Connect and OAuth2 (authorization code). Which operations show a lock is
decided **per operation** from endpoint metadata: `IAuthorizeData` means secured, `IAllowAnonymous` means open. So an
anonymous endpoint inside an authorized group correctly shows no lock, and a group that requires auth marks all of its
operations.

## Logging in from the docs UI

`WithAuroreAuthentication` preconfigures Scalar for the OAuth2 **authorization code + PKCE (SHA-256)** flow:

- client id comes from `ApiDocs:OAuthClientId` (a public client in the identity provider; no client secret);
- the available scopes are the ones in `IdentityProvider:Scopes`, all selected by default;
- OAuth2 is the preferred scheme, so "Authorize" opens the login flow first.

In the identity provider, register that client as public, allow PKCE, and add the docs page URL as a valid redirect URI.
The redirect URI is not set in code; Scalar uses its own page address.

## Settings involved

| Key | Used for |
| --- | --- |
| `AppInformation` | document title, description, contact |
| `ApiDocs:OAuthClientId` | OAuth2 client for the docs UI |
| `IdentityProvider` | authority, scopes |
| `ApiKeys:HeaderName` | header name for the API key scheme |

## How-to: describe an endpoint better

Use `Summary` and `Description` on the endpoint class and `.ProducesProblem(...)`/`.Produces<T>()` in `Map`. Everything else
(tags, versions, security, global problems) comes from the group and the catalog.

## How-to: change the look

Theme, default HTTP client and title are in `UseAppScalar` (`WithTheme`, `WithDefaultHttpClient`, `WithTitle`). Everything
auth-related lives in `ScalarExtensions` so applications do not repeat it.
