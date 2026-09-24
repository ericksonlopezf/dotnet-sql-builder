# Level 8: Customization & Extensibility

## Overview

Level 8 explains how to extend or replace core framework abstractions: custom parameter management, custom `ITypeHandler<T>` value object mappings, reusable query specifications (`ISqlFilter<T>`), direct compiler invocation, and identifier escaping.

---

## Extensibility Points

1. **Custom `IParameterManager`**:
   Inspect or transform parameter names and values during query compilation (e.g., prefixing, audit logging, parameter sanitization).
2. **Specification Pattern (`ISqlFilter<T>`)**:
   Create composable domain filters that encapsulate query predicates into domain units.
3. **Low-Level `ISqlCompiler`**:
   Invoke compilers directly to inspect AST nodes, escape custom identifiers, or render specialized SQL dialects.

---

## Code Examples

### Custom Parameter Logger
```csharp
public class LoggingParameterManager : IParameterManager
{
    private readonly List<string> _log = new();
    public string Add(object? value, string? prefix = null)
    {
        var paramName = $"@p{_log.Count}";
        _log.Add($"Added {paramName} = {value}");
        return paramName;
    }
    // ...
}
```

### Reusable Specification Filter
```csharp
public class MinimumPriceFilter : ISqlFilter<Product>
{
    private readonly decimal _minPrice;
    public MinimumPriceFilter(decimal minPrice) => _minPrice = minPrice;
    public SelectQuery<Product> Apply(SelectQuery<Product> query) => query.Where(p => p.Price >= _minPrice);
}
```

---

## Execution Output

```text
=== LEVEL 8: CUSTOMIZATION AND EXTENSIBILITY ===
[+] 1. ITypeHandler — Complex types (JSON in TEXT column)
[+] 2. Custom IParameterManager — Parameter logging
[+] 3. ISqlFilter<T> — Reusable filters as Specifications
[+] 4. ISqlCompiler — Low-level compilation with IParameterManager
[+] 5. ISqlCompiler.Escape — Escapado de identificadores
[+] 6. DapperExtensions.RegisterTypeHandler — Dual registration SqlBuilder+Dapper
```
