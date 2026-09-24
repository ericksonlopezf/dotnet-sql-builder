# Level 10: Enterprise Architecture & Patterns

## Overview

Level 10 shows how **EricksonLopez.SqlBuilder** integrates into enterprise architectures: Clean Architecture, Onion Architecture, and Hexagonal / Ports & Adapters. It covers the Generic Repository Pattern with Dependency Injection, CQRS separation of commands and queries, multi-compiler dialect independence, and OpenTelemetry distributed tracing.

---

## Architectural Patterns

1. **Repository Pattern with DI**:
   Abstract database access behind `IUserRepository`, keeping domain logic decoupled from ADO.NET and Dapper execution details.
2. **CQRS (Command Query Responsibility Segregation)**:
   - **Commands**: `Sql.Insert`, `Sql.Update`, `Sql.Delete` handle write operations and concurrency checks.
   - **Queries**: `Sql.From` and projection pipelines handle read operations and pagination.
3. **Multi-Dialect Portability**:
   The exact same repository query can be compiled to SQLite for unit testing, PostgreSQL for local container testing, and SQL Server for enterprise production.

---

## Code Examples

### Clean Repository Implementation
```csharp
public class UserRepository : IUserRepository
{
    private readonly DbConnection _connection;
    private readonly ISqlCompiler _compiler;

    public UserRepository(DbConnection connection, ISqlCompiler compiler)
    {
        _connection = connection;
        _compiler = compiler;
    }

    public async Task<EnterpriseUser?> GetByIdAsync(int id)
    {
        var query = Sql.From<EnterpriseUser>().Where(u => u.Id == id);
        return (await _connection.QueryAsync<EnterpriseUser>(query)).SingleOrDefault();
    }
}
```

### CQRS Command / Query Separation
```csharp
// Command
var createCmd = Sql.Insert(user);
await connection.ExecuteAsync(createCmd);

// Query
var activeUsersQuery = Sql.From<EnterpriseUser>().Where(u => u.Role == "Admin");
var admins = await connection.QueryAsync<EnterpriseUser>(activeUsersQuery);
```

---

## Execution Output

```text
=== LEVEL 10: ENTERPRISE ARCHITECTURE (DI, REPO, CQRS) ===
[+] 1. Repository Pattern — Data access layer separation
[+] 2. ApplyDiff — Differential UPDATE (changed columns only)
[+] 3. Specification Pattern — ISqlFilter<T> applied by Repository
[+] 4. CTE — Common Table Expressions for complex reports
[+] 5. Multi-Compiler — Misma query, distintos dialectos SQL
[+] 6. CQRS Pattern — Separation of Commands and Queries
[+] 7. Observability — WithTag + OpenTelemetry (ActivitySource)
```
