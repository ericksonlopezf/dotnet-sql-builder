# Level 08: Customization

**Context:** Custom implementations of every extensible public interface: `ITypeHandler<T>`, `ISqlFilter<T>`, `ISqlCompiler`, `IParameterManager`, `ITableNameResolver`.

---

## Extensible Public Interfaces

| Interface | Purpose | Register Via |
|-----------|---------|--------------|
| `ITypeHandler<T>` | Custom DB→CLR type mapping | `Sql.RegisterTypeHandler<T>(handler)` |
| `ISqlFilter<T>` | Reusable typed WHERE predicate | Used by Repository pattern |
| `ISqlCompiler` | Custom SQL dialect | Pass to `.Build(compiler)` |
| `IParameterManager` | Custom parameter name generation | Internal DI |
| `ITableNameResolver` | Override table name resolution | `SqlEntityCache<T>.OverrideTableName(name)` |

---

## ITypeHandler<T> Pattern

```csharp
// Maps CLR Money → decimal (persisted as INTEGER cents)
public class MoneyTypeHandler : ITypeHandler<Money>
{
    public void SetValue(IDbDataParameter parameter, Money value)
        => parameter.Value = (long)(value.Amount * 100);

    public Money Parse(object value)
        => new Money((long)value / 100m);
}

// Registration (once at startup)
Sql.RegisterTypeHandler<Money>(new MoneyTypeHandler());
```

## ISqlFilter<T> — Specification Pattern

```csharp
public class ActiveEmployeeFilter : ISqlFilter<Employee>
{
    public SelectQuery<Employee> Apply(SelectQuery<Employee> query)
        => query.Where(e => e.IsActive == true);
}

// Usage with Repository
var employees = await repository.GetAllAsync(new ActiveEmployeeFilter());
```

## ITableNameResolver Override

```csharp
// Override at runtime (useful for sharding/multi-tenant)
SqlEntityCache<Employee>.OverrideTableName("tenant_employees");
var query = Sql.From<Employee>(); // uses "tenant_employees"
```

---

## Reference

**Source:** [`Level08_Customization/CustomizationSample.cs`](../../samples/EricksonLopez.SqlBuilder.Samples/Level08_Customization/CustomizationSample.cs)
