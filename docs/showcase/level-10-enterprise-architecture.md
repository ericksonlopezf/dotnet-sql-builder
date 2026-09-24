# Level 10: Enterprise Architecture

**Context:** DI integration, Repository pattern, CQRS, CTE-based reports, multi-compiler cross-dialect queries, observability.

---

## Patterns Demonstrated

| Pattern | API Used |
|---------|---------|
| Repository Pattern | `Sql.From<T>()`, `ExecuteAsync`, `ISqlFilter<T>` |
| ApplyDiff UPDATE | `DiffUpdateExtensions.ApplyDiff<T>()` |
| Specification Pattern | `ISqlFilter<T>.Apply(SelectQuery<T>)` |
| CTE reports | `.CTE("alias", subquery)` on `SelectQuery<T>` (e.g., `.CTE("active", Sql.From<User>().Where(...))`) |
| Multi-compiler | Same query built with `SqliteCompiler`, `SqlServerCompiler`, `PostgreSqlCompiler` |
| CQRS separation | Commands: Insert/Update/Delete, Queries: Select/Project |
| OpenTelemetry | `WithTag(name)` + `SqlBuilderDiagnostics.ActivitySource` |

---

## Repository Pattern with ISqlFilter<T>

```csharp
public class UserRepository
{
    private readonly DbConnection _connection;
    private readonly ISqlCompiler _compiler;

    public async Task<IEnumerable<User>> GetAllAsync(ISqlFilter<User>? filter = null)
    {
        var query = Sql.From<User>();
        if (filter != null) query = filter.Apply(query);
        return await _connection.QueryAsync<User>(query, _compiler);
    }
}
```

## Multi-Compiler Cross-Dialect

```csharp
var baseQuery = Sql.From<User>().Where(u => u.Role == "Admin").OrderBy(u => u.Username).Limit(10);

var sqliteResult = baseQuery.Build(new SqliteCompiler());
var sqlServerResult = baseQuery.Build(new SqlServerCompiler());
var pgResult = baseQuery.Build(new PostgreSqlCompiler());
// Same AST, 3 different SQL strings
```

## CQRS Boundary

```csharp
// Command side: mutation builders
async Task CreateUser(CreateUserCommand cmd) =>
    await connection.ExecuteAsync(Sql.Insert(cmd.ToEntity()), compiler);

// Query side: projection builders
async Task<IEnumerable<UserDto>> GetActiveUsers() =>
    await connection.QueryAsync<UserDto>(Sql.From<User>().Where(u => u.IsActive), compiler);
```

---

## Reference

**Source:** [`Level10_EnterpriseArchitecture/EnterpriseArchitectureSample.cs`](../../samples/EricksonLopez.SqlBuilder.Samples/Level10_EnterpriseArchitecture/EnterpriseArchitectureSample.cs)
