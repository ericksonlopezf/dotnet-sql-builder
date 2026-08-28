# ADR-007: No Change Tracking

## Status
Accepted

## Context
Change tracking is a core ORM feature that observes property mutations on live entity objects and automatically generates SQL UPDATE statements at flush time.

## Critical Distinction
**`ApplyDiff()` is NOT change tracking.**

| Concept | Change Tracking | ApplyDiff |
|---------|----------------|-----------|
| Who observes mutations | The library (auto) | The user (explicit) |
| When does diff occur | At flush/SaveChanges | At explicit `.ApplyDiff(original, current)` call |
| Entity state | Mutable (tracked) | Immutable (two snapshots) |
| Implementation | Property change notifications / proxy | Source-generated structural comparison |
| AOT compatible | ❌ (proxy generation) | ✅ (struct comparison) |

## Problem
Adding change tracking would require:
1. Mutable entity state — contradicts immutable AST design
2. Property change notification hooks (INotifyPropertyChanged or IL-proxy)
3. An identity map (per-session entity cache)
4. Thread-safety mechanisms for the tracker
5. Full invalidation logic when entities are evicted

## Decision
Zero change tracking. Ever. `ApplyDiff()` is the sanctioned diff mechanism — stateless, explicit, reflection-free, AOT-compatible.

## Consequences
### Positive
- No mutable shared state
- No thread-safety issues
- No hidden SQL generation
- AOT compatible

### Negative
- Users must explicitly provide `(original, current)` snapshots
- N+1 management is user responsibility

### Mitigation
- `QueryMultipleAsync` and multi-mapping guidance for efficient loading
- `ApplyDiff()` covers the most common update case without tracking complexity

## API Impact
None — `ApplyDiff()` API remains unchanged.

## Reconsideration Criteria
This decision is foundational. Reconsidering it would require a different product vision.
