# Conditional CASE Expressions in EricksonLopez.SqlBuilder

EricksonLopez.SqlBuilder provides a fluent, type-safe API to construct conditional `CASE WHEN ... THEN ... ELSE ... END` expressions in `SELECT`, `ORDER BY`, and `WHERE` clauses.

---

## 1. Constructing with `SelectCase`

The `SelectCase` method enables strongly-typed or predicate-based `CASE` blocks:

```csharp
var query = Sql.From<Order>()
    .Select("Id", "CustomerId", "Total")
    .SelectCase(c => c
        .When(o => o.Total > 1000, "VIP")
        .When(o => o.Total > 500, "Premium")
        .Else("Standard")
        .As("CustomerTier")
    );
```
Generated SQL:
```sql
SELECT Id, CustomerId, Total, 
       CASE 
           WHEN Total > 1000 THEN 'VIP'
           WHEN Total > 500 THEN 'Premium'
           ELSE 'Standard'
       END AS CustomerTier
FROM Orders
```

---

## 2. CASE Expressions with FormattableString

For conditions involving database functions or subqueries:

```csharp
var query = Sql.From<InventoryItem>()
    .Select("Sku")
    .SelectCase(c => c
        .WhenRaw($"stock_quantity <= 0", "OutOfStock")
        .WhenRaw($"stock_quantity < reorder_point", "Reorder")
        .Else("OptimalStock")
        .As("StockStatus")
    );
```

---

## 3. Key Benefits

- **Type-safe:** Eliminates fragile, concatenated SQL string snippets.
- **Dialect-aware:** Compiles accurately respecting dialect quoting and parameter binding conventions.
- **Thread-safe and Immutable:** Returns a new, immutable `SelectQuery<T>` instance.
