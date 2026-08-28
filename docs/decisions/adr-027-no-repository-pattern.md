# ADR-027: No Repository Pattern Implementation

## Status

Accepted

## Date

2026-08-14

## Context

Repository pattern (`IRepository<T>`, `IReadRepository<T>`) is a common abstraction in DDD applications. Some libraries (RepoDb, Dommel) ship pre-built repository implementations. Should SqlBuilder ship `IRepository<T>` or `ICrudRepository<T>`?

## Decision

No. SqlBuilder will not ship a repository implementation.

## Rationale

1. **Repository is application architecture, not infrastructure.** SqlBuilder builds SQL. What consumes that SQL is the application's concern.

2. **Repository implementations are business-specific.** A `UserRepository` needs to know what "active user" means. A `OrderRepository` needs to know what "fulfillable order" means. These are not library concerns.

3. **API explosion.** Every `T` needs `GetByIdAsync(TKey id)`, `GetAllAsync()`, `FindAsync(spec)`, `AddAsync(T)`, `UpdateAsync(T)`, `DeleteAsync(TKey)`. This is 6+ methods per entity. With generics it's 6 extension points that all conflict with DI, cancellation, transactions, etc.

4. **Users already have this.** Application code using SqlBuilder naturally produces repositories:
   ```csharp
   public class OrderRepository
   {
       private readonly IDbConnection _conn;
       private readonly SqlServerCompiler _compiler;

       public Task<IEnumerable<Order>> GetActiveAsync(CancellationToken ct)
           => _conn.QueryAsync<Order>(
               Sql.From<Order>().Where(o => o.IsActive),
               _compiler, cancellationToken: ct);
   }
   ```

5. **Tight coupling.** A shipped repository would need to know about: connection lifetime, DI, cancellation, pagination, transactions. This pulls in ADR-023 (no DI), ADR-004 (UoW is optional), ADR-012 (pagination strategy) all at once.

## Consequences

### Positive

- SqlBuilder stays a SQL compiler; consumers own their repository layer
- No framework coupling; no opinionated DI integration
- No "magic" repository that users fight against

### Negative

- Users write their own repository classes (this is the correct behavior)

## Reconsideration Criteria

Never. If a repository is needed, users build it on top of SqlBuilder. That is precisely the correct layering.
