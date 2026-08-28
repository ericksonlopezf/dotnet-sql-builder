# ADR-048: API Deprecation vs Removal Policy

## Status
Proposed — August 2026

## Context
During the consistency audit (August 2026), the state of Sql.Merge<T>() was found to be contradicted across four different sources simultaneously:

| Source | What it says |
|--------|-------------|
| README.md | "Sql.Merge<T>() is [Obsolete]" (implies warning) |
| ADR-025 | "will be removed in v2.0" (future) |
| ESQL026 Analyzer | "DiagnosticSeverity.Error" + "has been removed in v2.0" (present) |
| PublicAPI.Unshipped.txt | Lists Sql.Merge<T>() as active public API |

This four-way contradiction was caused by the absence of a formal, documented policy that distinguishes between:
1. **Deprecated** (still works, warning issued)
2. **Obsolete** ([Obsolete] attribute, build warning)
3. **Removed** (API does not exist)
4. **Blocked by analyzer** (API exists but its use is a build error)

## Decision (Proposed)
Establish the following formal deprecation → removal lifecycle:

### Phase 1: Soft Deprecation
- Add [Obsolete("Use X instead. This API will be removed in vX.Y.", error: false)] attribute.
- Analyzer emits **Warning** (not Error) if usage detected.
- API remains in PublicAPI.Unshipped.txt (if not yet shipped) or PublicAPI.Shipped.txt (if shipped).
- Documentation says: "Deprecated — use [alternative]."
- ADR status: the decision ADR says "Deprecated since vX".

### Phase 2: Hard Deprecation (one major version later)
- Change [Obsolete] attribute to rror: true.
- Analyzer emits **Error**.
- API remains callable via suppression (#pragma warning disable or SuppressMessage).
- Documentation says: "Removed in vX — [alternative]."
- ADR states the removal version explicitly.

### Phase 3: Full Removal
- Remove the type/method entirely from source.
- Remove from PublicAPI.Unshipped.txt.
- If previously in PublicAPI.Shipped.txt, add to a PublicAPI.Removed.txt for tracking.
- Analyzer remains (references to removed API fail with compile error at the call site level, not analyzer level).

### Enforcement rules
- An API cannot skip from Phase 1 to Phase 3 within the same major version cycle.
- The analyzer severity for a deprecated API must match its phase (Warning → Error → Compiler error).
- The documentation status must match the code state at all times.

## Consequences
- Eliminates four-way contradictions like the Sql.Merge<T>() case.
- Makes the transition from "deprecated" to "removed" predictable for consumers.
- The CHANGELOG must record each phase transition.
- ADR documents must be updated at each phase transition.

## Immediate Actions for MergeQuery<T>

Since MergeQuery<T> was never shipped (not in PublicAPI.Shipped.txt), it should be treated as:
- **Rejected before shipping** — the simplest case.
- Remove from PublicAPI.Unshipped.txt.
- The ESQL026 analyzer remains as protection against future re-introduction.
- ADR-025 and ADR-040 document the architectural rejection permanently.
