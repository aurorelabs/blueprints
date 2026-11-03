[back](../../CHANGELOG.md)

# Changelog Instructions

## General Recommendations

- Keep `Unreleased` at the top and move its contents into a version section after creating a new release.
- `DO NOT` copy commit messages into changelog. Write entries that explain the impact of what was done in a reader friendly manner.
- Only include notable changes. Refactors, renamings and other small changes are not relevant or customer-wise and should be avoided in the `CHANGELOG.md` file.

## Example of Entries

```markdown
## [Unreleased]

### Added

- Support for OpenTelemetry tracing.

### Changed

- Improved cache invalidation strategy to reduce stale data.

---

## [1.0.1] - 2026-07-01

### Added

- HybridCache support with Redis as distributed solution.
- Configurable rate limiting middleware.

### Fixed

- Updated dependencies to address known vulnerabilities.

```

[back](../../CHANGELOG.md)