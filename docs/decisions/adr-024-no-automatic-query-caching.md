# ADR-024: No Automatic Query Caching

## Status
Accepted

## Date
2026-08-12

## Context
Some libraries (RepoDB, certain Dapper extensions) automatically cache compiled queries in a static `ConcurrentDictionary<string, CompiledQuery>`. The idea is that repeated calls to build the "same" query skip the expression compilation step.

## Problem with Automatic Caching

1. **Cache invalidation is hard:** When is a cached query stale? If the user adds a new property to an entity, should the cache be invalidated? How?

2. **Memory leaks:** A query cache that grows unboundedly (e.g., keyed by query text including parameter values) is a slow memory leak.

3. **Hidden state:** An invisible static cache creates non-deterministic behavior — the first call builds the query, subsequent calls return the cached version. Hard to debug when something changes.

4. **Thread safety complexity:** ConcurrentDictionary add-if-absent races can result in double compilation unless carefully implemented.

5. **NativeAOT incompatibility:** `Expression.Compile()` in a static constructor runs at startup under NativeAOT — unpredictable timing.

6. **Wrong layer:** Query caching is an application concern. The application knows its query patterns; the library does not.

## Options Considered

### Option A: Built-in transparent cache (RepoDB-style)
- Rejected: hidden state, cache invalidation, memory leak risk

### Option B: Explicit cache API (`QueryCache.GetOrAdd(key, () => query)`)
- Rejected: the library should not define caching primitives; `IMemoryCache` or `ConcurrentDictionary` already serve this purpose

### Option C: No caching — document the correct pattern
- **Chosen**: Correct, explicit, user-controlled

### Option D: Compiled query via Source Generator (zero runtime cost)
- **Complementary**: The AOT render path generates SQL at compile time — no runtime caching needed

## Decision

**Zero built-in caching.** All compilation happens inline when `.Build()` or `.Compile()` is called.

**Correct caching patterns (user responsibility):**

```csharp
// Pattern 1: Static pre-built query (immutable — safe to cache)
private static readonly SelectQuery<User> _activeUsersQuery = Sql.From<User>()
    .Where(u => u.Active)
    .OrderBy(u => u.Name);

// Per-request: add pagination without mutation
var page = _activeUsersQuery.Page(pageNumber, pageSize);
var result = compiler.Compile(page);

// Pattern 2: Cache compiled SQL string (expression compilation is expensive, SQL string is not)
private static readonly ConcurrentDictionary<string, CompiledQuery> _cache = new();

var compiled = _cache.GetOrAdd(
    "active-users-page",
    _ => compiler.Compile(Sql.From<User>().Where(u => u.Active))
);

// Pattern 3: AOT path — compile-time generated SQL string (zero runtime cost)
// Source Generator produces GetSql() method — no runtime compilation at all
```

**Why the static query pattern works:**
Because `SelectQuery<T>` is immutable (see ADR-017), a static base query is safe to share across threads and requests. Adding `.Page()` / `.Where()` creates a new instance — the static instance is never mutated.

## Consequences

### Positive
- ✅ No hidden state — predictable, debuggable behavior
- ✅ No memory leaks from unbounded query caches
- ✅ Thread-safe by construction (immutability, not caching)
- ✅ User controls caching strategy at the appropriate layer

### Negative
- ❌ Expression compilation (`Expression.Compile()`) runs on first use — one-time cost
- ❌ Users must implement their own caching if they want to amortize compilation cost

## Performance Guidance
For applications that need to minimize compilation overhead:
1. **Preferred:** Use the AOT render path (Source Generator generates SQL at compile time)
2. **Alternative:** Cache at the `SelectQuery<T>` level (static readonly field)
3. **Last resort:** Cache the `CompiledQuery` result string

## Reconsideration Criteria
If benchmarks show that expression compilation is the dominant cost and static fields are insufficient (unlikely for typical query patterns), evaluate an optional explicit cache extension.

## References
- [FEATURE_MATRIX.md §18 — Anti-Feature Matrix](../../FEATURE_MATRIX.md)
- [ADR-017: Immutable AST](./adr-017-immutable-ast-record-semantics.md)
- [ADR-014: Zero-Allocation Benchmark Proof](./adr-014-zero-allocation-benchmark-proof.md)
- [ADR-006: Source Generator Strategy](./adr-006-source-generator-strategy.md)
