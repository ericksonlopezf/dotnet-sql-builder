# ADR-023: No Dependency Injection or Logging as Core Dependencies

## Status
Accepted

## Date
2026-08-12

## Context
Many .NET libraries add `Microsoft.Extensions.DependencyInjection` and `Microsoft.Extensions.Logging` as dependencies, even to their core packages. This creates friction for users on alternative DI containers (Autofac, DryIoc, Pure DI) and for console applications that don't need a full `IServiceCollection` setup.

## Problem
- `Microsoft.Extensions.DependencyInjection.Abstractions` adds 100KB+ of transitive dependencies
- `ILogger<T>` in Core forces a logging framework on all users â€” even those using Serilog, NLog, or structured logging at a different layer
- Service registrations in Core create assumptions about application architecture
- Users in NativeAOT contexts may need to avoid the MEL stack entirely

## Options Considered

### Option A: Add `AddSqlBuilder(IServiceCollection)` to Core + `ILogger<T>` in Core
- Rejected: forces DI and logging on all users; changes Core into a framework integration

### Option B: No DI, no logging in Core â€” provide optional `ServiceCollectionExtensions` in a separate package
- **Partially chosen**: DI registration is provided as convenience extension methods in the Dapper package (since it's already infrastructure-facing)

### Option C: Use `ILoggerFactory` via optional constructor parameter (null = no logging)
- Rejected: null parameter is a code smell; better to use a separate extension mechanism

### Option D: Separate `EricksonLopez.SqlBuilder.Extensions.DependencyInjection` package
- Considered but not implemented: the Dapper package already contains `IServiceCollection` extensions where needed

## Decision

**Core Package (`EricksonLopez.SqlBuilder`):**
- âŒ No `Microsoft.Extensions.DependencyInjection` dependency
- âš ï¸ Uses lightweight `Microsoft.Extensions.Logging.Abstractions` strictly for optional query diagnostics via `SqlBuilderDiagnostics.LoggerFactory` (never pulls full `Microsoft.Extensions.Logging` implementation)
- âŒ No `ILogger<T>` in public query builder APIs
- âŒ No `AddSqlBuilder(IServiceCollection)` extension method in Core
- âœ… Compilers are instantiated directly: `new PostgreSqlCompiler()`

**Dapper Package (`EricksonLopez.SqlBuilder.Dapper`):**
- âœ… Optional `IServiceCollection.AddSqlBuilderDapper()` extension (convenience, not required)
- âœ… `ActivitySource` and `Meter` for OpenTelemetry (no `ILogger` dependency)
- âœ… `ILogger<T>` may appear in optional diagnostic extension methods, but is never required

**Observability (OpenTelemetry):**
OpenTelemetry's `ActivitySource` and `Meter` are used for distributed tracing and metrics. These are part of `System.Diagnostics` (BCL) â€” not an external dependency â€” so they are acceptable in the Dapper package.

```csharp
// âœ… Correct â€” no DI required:
var compiler = new SqlServerCompiler();
var result = compiler.Compile(Sql.From<User>().Where(u => u.Active));
using var conn = new SqlConnection(connStr);
var users = await conn.QueryAsync<User>(result);

// âœ… Optional â€” with DI:
services.AddSqlBuilderDapper();
// â†’ registers SqlServerCompiler, IDbConnectionFactory, etc.
```

## Consequences

### Positive
- âœ… Zero framework coupling â€” library works in any application model
- âœ… No DI container requirements â€” works with Pure DI, manual composition
- âœ… Console apps, Lambda functions, NativeAOT apps can use Core with no framework baggage
- âœ… Testability â€” no mocking of `IServiceProvider` needed

### Negative
- âŒ DI users must wire up the compiler themselves (or use the optional extension)
- âŒ No automatic lifetime management â€” compiler instances are singletons by convention (they are stateless)

## Compiler Lifetime Guidance
All `*Compiler` classes are **stateless** â€” they hold no mutable state. They should be registered as singletons when used with DI, or instantiated once as `static readonly` in non-DI scenarios.

```csharp
// Non-DI (recommended pattern):
private static readonly PostgreSqlCompiler _compiler = new();
```

## Reconsideration Criteria
If .NET ships a "zero-overhead DI" primitive that adds no dependencies (unlikely), evaluate adopting it.

## References
- [FEATURE_MATRIX.md Â§18 â€” Anti-Feature Matrix](../master-feature-matrix.md)
- [ADR-003: Polly Not a Core Dependency](./adr-003-polly-not-core-dependency.md)
- [ADR-013: AOT Guarantees](./adr-013-aot-guarantees.md)
