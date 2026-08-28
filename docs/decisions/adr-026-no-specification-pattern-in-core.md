# ADR-026: No Specification Pattern Implementation in Core

## Status

Accepted

## Date

2026-08-14

## Context

The Specification pattern (from DDD) encapsulates query logic in `ISpecification<T>` objects that can be combined with `And()`, `Or()`, `Not()`. It is a common pattern for repository-based architectures.

Some libraries embed specification-to-query translation. Should EricksonLopez.SqlBuilder implement or ship a `Specification` integration?

## Problem

EricksonLopez ecosystem contains (or plans) an `EricksonLopez.Specification` library. Should SqlBuilder integrate with it? Should SqlBuilder ship its own specification support?

## Options Considered

### Option A: Specification support in Core (rejected)

Add `SelectQuery<T>.Where(ISpecification<T> spec)` to Core.

**Problems:**
- Requires Core to depend on `EricksonLopez.Specification` — violates ADR-023 (no unnecessary Core deps)
- Specification pattern is an application architecture pattern, not a SQL library concern
- Specifications may contain business logic that doesn't translate to SQL (e.g., in-memory checks)

### Option B: Optional adapter package (chosen, deferred)

An `EricksonLopez.SqlBuilder.Specification` adapter package that provides:

```csharp
public static class SpecificationExtensions
{
    public static SelectQuery<T> Where<T>(
        this SelectQuery<T> query,
        ISpecification<T> spec) where T : class, new()
    {
        return query.Where(spec.ToExpression());
    }
}
```

This requires `ISpecification<T>` to expose `Expression<Func<T, bool>> ToExpression()`.

### Option C: No integration (current state)

Users write:
```csharp
query.Where(spec.ToExpression());
```

This is a single line. The adapter would save one level of indirection.

## Decision

**Option C (current state) until EricksonLopez.Specification matures.**

If `EricksonLopez.Specification` ships a stable `ToExpression()` contract, create a thin adapter package (`EricksonLopez.SqlBuilder.Specification`) as Option B describes. This package must NOT be in Core.

## Decision Drivers

- **Scope:** Specification is an application-layer pattern. SqlBuilder is a SQL compiler.
- **Dependency direction:** Core must never depend on application patterns.
- **API cost:** Adding specification support to Core for marginal ergonomic benefit is not justified.

## Consequences

### Positive

- Core remains clean and dependency-free
- Users can easily adapt specifications via `spec.ToExpression()`

### Negative

- No built-in specification support — users must do the one-liner adaptation

## Reconsideration Criteria

If `EricksonLopez.Specification` ships with a stable `Expression<Func<T, bool>> ToExpression()` contract, create the adapter package.
