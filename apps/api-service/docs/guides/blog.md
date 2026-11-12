# The blog: authors, posts and comments

The domain of this service. It is deliberately small, and it uses most of what the foundation offers.

## The model

```
Author 1 ─── * Post 1 ─── * Comment
   └────────────────────────────* (an author writes both)
```

| Entity | Base | Notes |
|---|---|---|
| `Author` | `Entity` (timestamps) | An account, linked to the identity by `Subject` (the `sub` claim, unique). `Handle` is unique and never changes. Having one does not oblige its owner to write anything. Not soft deletable on purpose: a filtered parent with unfiltered children makes joins return nothing. |
| `Post` | `SoftDeletableEntity` | Unique `Slug` (the unique index ignores deleted rows, so a deleted post gives its slug back), `Title`, `Body`, `Status` (`Draft` or `Published`, stored as text), `PublishedAt`. |
| `Comment` | `SoftDeletableEntity` | Belongs to a post, written by an author. |

Entities are in `src/Core/Domain`, their configurations in `src/Infra/Contexts/Configurations`, and the migration in
`tools/Tools.Migrations/History`.

## Who can do what

| | Reader | Editor | Admin |
|---|---|---|---|
| Read posts and comments | yes | yes | yes, drafts of everyone |
| Create the own author profile | yes | yes | yes |
| Comment (needs a profile) | yes | yes | yes |
| Write, change and publish posts | no | yes (own) | yes (any) |
| Delete comments | no | no | yes |
| Delete posts | no | no | yes |

In the sandbox Keycloak (realm `aurorelabs`): `alice` is a reader, `bob` is an editor and an admin. The API key client of `appsettings.json` can read only.
Roles come from the token, the scopes `api.read` and `api.write` from the client.

## Endpoints (`/v1`)

| | | |
|---|---|---|
| `PUT /authors/me` | any, `api.write` | Creates the profile of the caller or changes its display name. Idempotent by nature. |
| `GET /posts` | `api.read` | A numbered page. Filters `title` (any case), `status` and `author` (a handle); ranges `createdFrom/To`, `updatedFrom/To`; `orderBy` title, author, publishedAt, createdAt (newest first by default). Drafts of others never appear. |
| `GET /feed` | `api.read` | The published posts as an infinite scroll (`cursor`, `pageSize`), newest first, never a draft. Filters `title` and `author`; `orderBy` title, author or createdAt (not publishedAt: a cursor cannot page by a value that can be missing). |
| `GET /posts/{slug}` | `api.read` | A draft is a 404 for anyone but its author and admins. |
| `POST /posts` | editor or admin | A draft. 409 `post.slug_taken` when the slug is in use. Accepts `Idempotency-Key`. |
| `PATCH /posts/{id}` | editor or admin | Changes the fields that are sent (title, body, or both) and nothing else: the slug never changes. The author or an admin. |
| `POST /posts/{id}/publish` | editor or admin | The author or an admin, once (409 `post.already_published`). |
| `DELETE /posts/{id}` | admin | Soft delete. |
| `GET /posts/{id}/comments` | `api.read` | An infinite scroll (`cursor`, `pageSize`), oldest first. |
| `POST /posts/{id}/comments` | `api.write` | Only on published posts (409 `post.not_published`). Accepts `Idempotency-Key`. |
| `DELETE /posts/{id}/comments/{commentId}` | admin | Soft delete; a comment of another post is a 404. |

Without a profile, writing a post or a comment is a 409 `author.profile_required`: the profile is created explicitly, never as a side effect.

## Where the rules live

- **Endpoints** (`src/App.Api/Resources`) validate the shape of the body (`RequestValidator`) and build the response. The caller comes from the
  token (`ToCaller()`), never from the body or the query.
- **Features** (`src/Features`) hold the rules: the author profile is a `FeatureValidator`, the visibility of drafts, who may publish, the status
  checks. Unique conflicts (handle, slug) are answered by the unique violation decorator, with the errors registered for the indexes in
  `AddAppDatabase`.

## Trying it

With the sandbox up (PostgreSQL, Keycloak and nginx, plus this service as a container behind `http://api.localhost`, see [running it](./deployment.md)), run the requests of `src/App.Api/Resources/Blog.http`
in order: it gets the tokens from Keycloak itself. Point its `@host` at `http://api.localhost` for the container, or at `https://localhost:5001` for the service run from the IDE (then apply the
migration to a local database first, see [database](./database.md)). You can also sign in through the API documentation page. The order of the story: `PUT /authors/me`, `POST /posts`,
`POST /posts/{id}/publish`, `POST /posts/{id}/comments`, `GET /posts/{id}/comments`.
