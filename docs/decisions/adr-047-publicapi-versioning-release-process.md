# ADR-047: PublicAPI.txt Versioning and Release Process

## Status
Proposed — August 2026

## Context
During the consistency audit (August 2026), PublicAPI.Unshipped.txt was found to contain entries for MergeQuery<T> and Sql.Merge<T>() despite these types having no corresponding implementation files in src/EricksonLopez.SqlBuilder/. The Roslyn Analyzer ESQL026 reports an Error for MergeQuery<T> usage, labeling it as "removed in v2.0", while PublicAPI.Unshipped.txt treats it as a pending addition.

This mismatch reveals the absence of a formal process for:
1. Transitioning API surface from Unshipped → Shipped during a release.
2. Removing API surface from Unshipped.txt when an API is rejected or removed before shipping.
3. Verifying that entries in PublicAPI.Unshipped.txt have a corresponding .cs implementation.

## Decision (Proposed)
Define and enforce the following process:

### 1. PublicAPI.Unshipped.txt governance
- Any entry in PublicAPI.Unshipped.txt must correspond to an existing, compilable type or method in src/.
- Entries for removed or rejected APIs must be deleted from Unshipped.txt immediately — they must NOT remain as "pending" documentation for removed features.
- A new CI check (script or Roslyn analyzer) must verify that every entry in Unshipped.txt resolves to a known public symbol in the compiled assembly.

### 2. Release promotion process
At each release:
1. Move all entries from PublicAPI.Unshipped.txt to PublicAPI.Shipped.txt.
2. Reset PublicAPI.Unshipped.txt to empty.
3. The PublicAPI.Analyzer (RS0016/RS0017) enforces that new public members are either declared or suppressed.

### 3. Cleanup action for current state
Remove MergeQuery<T> and Sql.Merge<T>() from PublicAPI.Unshipped.txt since:
- No implementation file exists.
- ESQL026 enforces removal.
- The API was never shipped (not in PublicAPI.Shipped.txt).

## Consequences
- CI will fail if PublicAPI.Unshipped.txt contains dangling references to non-existent types.
- Release automation becomes trivially scriptable: move Unshipped → Shipped + empty Unshipped.
- Prevents future discrepancies between documented-but-unimplemented APIs and analyzer errors.
