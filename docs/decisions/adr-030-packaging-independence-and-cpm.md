# ADR-030: Package Independence, Zero Sibling Project References, and CPM Governance

## Status

Accepted

## Date

2026-08-15

## Context

In multi-package .NET solutions, projects often reference sister packages. During early development, local relative project references (e.g. `<ProjectReference Include="..\..\..\dotnet-pagination\..." />`) were introduced.

Such relative references introduce several critical liabilities:
1. **Broken Isolated Builds:** The repository cannot be cloned, built, or tested in clean CI environments or by external open-source contributors without checking out adjacent sibling repositories at identical directory paths.
2. **Broken NuGet Packaging:** `dotnet pack` fails to generate proper package dependencies when referencing projects outside the repository boundary.
3. **Version Coupling:** Independent versioning (SemVer) of packages is compromised.

## Problem

How should `EricksonLopez.SqlBuilder` maintain strict build isolation, reproducible CI/CD pipelines, independent NuGet releases, and centralized dependency governance?

## Options Considered

### Option A: Retain relative sibling project references (Rejected)
- Causes build and CI failures in isolated build runners.
- Prevents public open-source contributions.

### Option B: Monorepo merge of all ecosystem packages into one solution (Rejected)
- Forces simultaneous releases and defeats fine-grained package modularity.
- Bloats repository checkout size and complicates CI matrix.

### Option C: Strict Repository Independence with Central Package Management (CPM) (Chosen)
- Every repository must build 100% self-contained from a clean clone with no external filesystem dependencies.
- Shared ecosystem abstractions (e.g., `EricksonLopez.Pagination`, `EricksonLopez.SharedKernel`) are consumed via official `<PackageReference>` managed in `Directory.Packages.props`.
- In cases where a light contract is needed (e.g., pagination parameter records), either reference the published NuGet package or define a minimal internal abstraction.

## Decision

**Option C.** All relative `<ProjectReference>` elements pointing outside the repository root are strictly prohibited. All external package dependencies must be declared in `Directory.Packages.props` via Central Package Management (CPM). The solution must build cleanly on any machine via `dotnet build` with zero external preconditions.

## Decision Drivers

- **Reproducibility:** Clean checkout → `dotnet build` succeeds immediately.
- **CI/CD Reliability:** GitHub Actions runners do not require multi-repo orchestrations.
- **Packaging Integrity:** `dotnet pack` generates standard, resolvable NuGet dependency graphs.

## Consequences

### Positive

- Standardized, reliable NuGet package publishing.
- Clean open-source developer experience.
- Strict architectural boundaries between distinct domain libraries.

### Negative

- Changes to shared abstractions must follow the standard release-and-bump workflow through NuGet or private feeds.

## Reconsideration Criteria

This policy is permanent.
