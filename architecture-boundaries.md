# Architecture Boundaries — EricksonLopez.SqlBuilder

> **Purpose:** Defines what belongs to EricksonLopez.SqlBuilder, what belongs to other layers,
> and what is permanently out of scope. This is the strategic identity document.
> Every feature proposal must be evaluated against these boundaries.
> Last audit: 2026-08-14

---

## The Core Identity

> **EricksonLopez.SqlBuilder is a SQL compiler + immutable AST + strongly typed query construction
> + dialect abstraction + optional execution adapters + compile-time safety + AOT infrastructure.**

It is NOT an ORM. It is NOT a data access framework. It is NOT a domain model.

---

## Boundary Model

```
┌──────────────────────────────────────────────────────────────────────┐
│                        Application / Domain                          │
│  Business logic, Domain entities, Application services, CQRS         │
│  Repository implementations, Specification pattern, Result types      │
└────────────────────────────────────────────────────────────────────┬─┘
                                                                     │ uses
┌────────────────────────────────────────────────────────────────────▼─┐
│                   EricksonLopez.SqlBuilder (execution)               │
│  DapperExtensions, UnitOfWork, Resilience, MultiMap, OpenTelemetry   │
│  "Wire the SQL to ADO.NET/Dapper; manage transactions"               │
└────────────────────────────────────────────────────────────────────┬─┘
                                                                     │ calls
┌────────────────────────────────────────────────────────────────────▼─┐
│                   EricksonLopez.SqlBuilder (core)                    │
│  SQL AST, Query builders, Dialect compilers, Source Generator        │
│  "Compile strongly typed queries to dialect-correct SQL + params"    │
└────────────────────────────────────────────────────────────────────┬─┘
                                                                     │ produces
┌────────────────────────────────────────────────────────────────────▼─┐
│                   ADO.NET / Dapper / raw IDbCommand                  │
│  Connection, IDbCommand, IDataReader, IDbTransaction                 │
└──────────────────────────────────────────────────────────────────────┘
```

---

## What Belongs TO SqlBuilder

### Category 1: Core SQL Compiler (Essential — ADR-017)

| Capability | Belongs | Rationale |
|-----------|---------|-----------|
| Immutable AST (`record` + `ImmutableArray`) | ✅ | Foundation of everything |
| `SelectQuery<T>` / `InsertQuery<T>` / `UpdateQuery<T>` / `DeleteQuery<T>` | ✅ | Core query types |
| `RawQuery` (FormattableString) | ✅ | Safe escape hatch |
| `ISqlCompiler` / `ISqlVisitor` abstraction | ✅ | Dialect abstraction contract |
| `SqlEntityCache<T>` (static generic init) | ✅ | AOT-safe metadata lookup |
| `SqlExpressionVisitor` (Expression Tree → SQL) | ✅ | Type safety bridge |
| `ParameterManager` | ✅ | Parameterized SQL security |
| `AotSqlRendererBase` | ✅ | NativeAOT execution path |

### Category 2: Dialect Compilers (Essential — ADR-009)

| Capability | Belongs | Package |
|-----------|---------|---------|
| SQL Server compiler (`FETCH NEXT`, `OUTPUT`, `CROSS APPLY`) | ✅ | `.SqlServer` |
| PostgreSQL compiler (`RETURNING`, `DISTINCT ON`, `LATERAL`, `COPY`) | ✅ | `.PostgreSql` |
| MySQL compiler (`ON DUPLICATE KEY`, backtick quoting) | ✅ | `.MySql` |
| SQLite compiler (`ON CONFLICT`, `RETURNING`) | ✅ | `.Sqlite` |
| Oracle compiler (`RETURNING INTO`, named params, UPPERCASE) | ✅ | `.Oracle` |

### Category 3: Source Generator (Strategic — ADR-006)

| Capability | Belongs |
|-----------|---------|
| `[SqlEntity]` metadata generation | ✅ |
| `IDataReader` parser generation | ✅ |
| `IBulkSerializer<T>` generation | ✅ |
| `SqlAlias` typed alias class | ✅ |
| `SelectAllTemplate` constant | ✅ |

### Category 4: Roslyn Analyzers (Strategic — ADR-014)

| Capability | Belongs |
|-----------|---------|
| DELETE/UPDATE without WHERE → Error | ✅ |
| SQL injection via string concat → Error | ✅ |
| Retry inside transaction → Warning | ✅ |
| Dialect-incompatible API → Warning | ✅ |
| `SELECT *` → Warning | ✅ |

### Category 5: Execution Adapters (Optional — ADR-002)

| Capability | Belongs | Rationale |
|-----------|---------|-----------|
| Dapper `QueryAsync<T>` / `ExecuteAsync` extensions | ✅ | Primary execution adapter |
| `QueryAotAsync<T>` (reflection-free) | ✅ | NativeAOT execution |
| `IAsyncEnumerable<T>` streaming | ✅ | Modern async pattern |
| Multi-mapping 2–8+ types | ✅ | Extends Dapper limitation |
| OpenTelemetry instrumentation | ✅ | Production observability |

### Category 6: Infrastructure Patterns (Supporting — ADR-004)

| Capability | Belongs | Rationale |
|-----------|---------|-----------|
| `IUnitOfWork` + `ISavepoint` | ✅ | Essential for transaction safety |
| `IBulkStrategy` plugin model | ✅ | Native bulk without commercial deps |
| `SqlResiliencePipeline` (Polly v8 wrapper) | ✅ | Optional; strict ESQL012 guard |

---

## What Does NOT Belong to SqlBuilder

### Category A: ORM Features (Permanent — ADR-007)

| Capability | Status | Why Not |
|-----------|--------|---------|
| Change tracking | 🚫 Permanent | Requires mutable in-memory state and snapshot buffers; violates AST immutability and concurrency invariants |
| Navigation properties | 🚫 Permanent | Introduces unpredictable implicit query generation (N+1 trap) |
| Identity map / first-level cache | 🚫 Permanent | Introduces hidden mutable state and thread-safety hazards |
| Lazy loading | 🚫 Permanent | Induces implicit side effects during traversal; unpredictable execution |
| Explicit loading | 🚫 Permanent | Outside the scope of AST compilation and hydration |

### Category B: Query Provider (Permanent — ADR-008)

| Capability | Status | Why Not |
|-----------|--------|---------|
| `IQueryable<T>` provider | 🚫 Permanent | 50+ operators; impossible to guarantee AOT safety and predictable SQL translation |
| LINQ expression translation | 🚫 Permanent | Requires extensive fallback handling; incompatible with strict NativeAOT compilation |
| Dynamic query via LINQ trees | 🚫 Permanent | Introduces severe runtime translation complexity |

### Category C: Schema Management (Permanent)

| Capability | Status | Why Not |
|-----------|--------|---------|
| Database migrations | 🚫 Permanent | Different infrastructure domain; out of scope for a query compiler |
| Schema creation / DDL | 🚫 Permanent | Infrastructure operational concern, not query concern |
| Schema diffing | 🚫 Permanent | Separate lifecycle from query execution |
| Table/index management | 🚫 Permanent | Architectural distinct responsibility |

### Category D: Business Logic (Permanent)

| Capability | Status | Why Not |
|-----------|--------|---------|
| Soft delete global filter | 🚫 Permanent | Implicit business rule; explicit predicate is required for predictability |
| Multi-tenancy global filter | 🚫 Permanent | Implicit hidden dependency; introduces scoping complexities incompatible with caching |
| Audit field automation | 🚫 Permanent | Domain rule; not a query compiler capability |
| Domain validation | 🚫 Permanent | Must be enforced in domain/application layer |
| Soft delete recovery | 🚫 Permanent | Application specific business logic |

### Category E: Hidden Infrastructure (Permanent — ADR-024)

| Capability | Status | Why Not |
|-----------|--------|---------|
| Automatic query result caching | 🚫 Permanent | Cache invalidation lifecycle is context-dependent and impossible to guarantee implicitly |
| Compiled SQL caching (automatic) | 🚫 Permanent | Predictable eviction policies belong in the application |
| Second-level cache | 🚫 Permanent | Requires state synchronization beyond query boundaries |
| Connection pooling | 🚫 Permanent | Handled natively by ADO.NET connection pools |
| Connection management | 🚫 Permanent | Lifecycle must be explicitly managed by application DI |

### Category F: Framework Coupling (Permanent — ADR-023)

| Capability | Status | Why Not |
|-----------|--------|---------|
| `IServiceCollection` auto-registration | 🚫 Permanent | Forces framework dependency; impedes minimal API integration and NativeAOT |
| `ILogger` in Core | 🚫 Permanent | Imposes logging abstraction; OpenTelemetry provides non-intrusive observability |
| `IConfiguration` integration | 🚫 Permanent | Configuration binding is an application bootstrapping concern |
| `IHostedService` SQL runners | 🚫 Permanent | Execution lifecycle falls strictly within application boundaries |

### Category G: Dangerous Abstractions (Permanent)

| Capability | Status | Why Not |
|-----------|--------|---------|
| Generic MERGE abstraction (single API across all dialects) | 🚫 Permanent | Semantics differ fundamentally per dialect; abstraction leaks; correctness risks are high. SQL Server MERGE has known concurrency bugs. Use dialect-specific `OnConflict` or raw `MERGE` |
| Automatic retry of mutations | 🚫 Permanent | Non-idempotent mutations + retry = duplicate data. ESQL012 enforces this boundary |
| Distributed transactions | 🚫 Permanent | MSDTC/XA; app infrastructure concern; not AOT-compatible |
| Specification pattern implementation | 🚫 Permanent | Application-layer pattern; builds on SqlBuilder, not inside it |
| Repository pattern implementation | 🚫 Permanent | Same |
| CQRS infrastructure | 🚫 Permanent | Application architecture; not library concern |

---

## Boundary with Dapper

| Operation | SqlBuilder | Dapper | Notes |
|-----------|-----------|--------|-------|
| SQL string construction | ✅ | ❌ | SqlBuilder's core job |
| Parameter binding (`IDbCommand`) | Produces dict | Consumes dict | SqlBuilder provides; Dapper binds |
| `IDataReader` hydration | Via SrcGen | Native | SqlBuilder AOT path; Dapper reflection path |
| Connection management | ❌ | ❌ | Application concern |
| Bulk insert | ✅ (strategy) | ❌ | SqlBuilder plugin model |
| Multi-mapping >7 | ✅ | ❌ (limits at 7) | SqlBuilder extends |
| `CommandDefinition` | ❌ | ✅ | Let Dapper handle command details |
| `TypeHandler<T>` | Bridged via `RegisterTypeHandler` | Native | Dual registration |

## Boundary with Application Architecture

| Concern | SqlBuilder | Application | Notes |
|---------|-----------|-------------|-------|
| Query building | ✅ | ❌ | |
| Business queries | ❌ | ✅ | App calls SqlBuilder |
| Domain logic | ❌ | ✅ | |
| Result mapping to DTO | ❌ | ✅ | App decides DTO shape |
| Error handling | ❌ | ✅ | SqlBuilder throws; app handles |
| Retry policy | Optional adapter | App decision | ESQL012 enforces boundary |
| Tenancy | ❌ | ✅ | App adds `.Where(x => x.TenantId == id)` |
| Soft delete | ❌ | ✅ | App adds `.Where(x => !x.Deleted)` |

## Boundary with EricksonLopez Ecosystem

| Library | Integration | Pattern | Package |
|---------|------------|---------|---------|
| `EricksonLopez.DomainPrimitive` | Optional | Value types as SQL parameters via `RegisterTypeHandler<T>` | No direct dep |
| `EricksonLopez.Result` | Optional | Query execution can return `Result<T>` in app layer | No direct dep |
| `EricksonLopez.Specification` | Optional adapter | `ISpecification<T>` → `.Where(spec.ToExpression())` | Adapter pkg |
| `EricksonLopez.Pagination` | Optional integration | `PagedQuery<T>` → `.Limit(page.Size).Offset(page.Offset)` | Adapter pkg |
| `EricksonLopez.Events` / `EricksonLopez.Outbox` | No integration | Separate concern | No dep |
| `EricksonLopez.Mapper` | No integration | Hydration is SrcGen responsibility | No dep |

---

## The Permanent "NO" List

These will never be built regardless of community demand, ecosystem trends, or implementation quality:

1. **Change tracking** — violates immutability at the architectural level
2. **LINQ `IQueryable<T>` provider** — impossible to correctly implement all operators; silent query failures
3. **Navigation properties / lazy loading** — N+1 by design; ORM territory
4. **Automatic query caching** — hidden state invalidation is not solvable correctly
5. **Generic cross-dialect MERGE abstraction** — semantic differences make a safe abstraction impossible
6. **DI / `IServiceCollection` in Core** — framework coupling; breaks AOT
7. **Database migrations** — fundamentally different tool category
8. **Automatic tenant / soft-delete filters** — hidden state; business logic
9. **IL emit / dynamic proxies** — breaks NativeAOT permanently
10. **Automatic retry of mutations** — non-idempotent operations cannot be safely retried

Each of these has an ADR documenting the rationale in `docs/decisions/`.

---

> **Guiding question for any new feature proposal:**
>
> *Does this make EricksonLopez.SqlBuilder a better SQL compiler and query construction library?*
>
> *Or does it make it a smaller ORM?*
>
> If the answer is the second, the feature should be rejected.
