# 08. AOT Code Generators

The project uses a Roslyn Source Generator compiler (`EricksonLopez.SqlBuilder.SourceGenerators`) to precalculate and generate essential metadata for Models decorated with `[SqlEntity]` at compile time.

This results in **zero reflections** at runtime when building a query, achieving extreme performance that significantly outperforms Entity Framework and SqlKata.

> [!TIP]
> Since Roslyn generates the code as an `IIncrementalGenerator`, there is no impact on execution time (Run-time). All the heavy lifting of discovering properties and columns happens in your IDE (Compile-time).

## What code is generated under the hood?

When you decorate your model:

```csharp
[SqlEntity("orders")]
public partial class Order {
    public int Id { get; set; }
    public decimal TotalAmount { get; set; }
}
```

The generator produces static filtering extension methods (`OrderFilters`) and strongly typed DTO filter classes (`OrderFilter : ISqlFilter<Order>`) that expose methods like `WhereIdEq` and `WhereTotalAmountGt`, eliminating the need for the engine to parse `Expression<Func<T, bool>>` expression trees at runtime.

```csharp
// Auto-generated AOT Code (emitted at compile time by FilterGenerator)
public static class OrderFilters 
{
    public static SelectQuery<Order> WhereIdEq(this SelectQuery<Order> query, int value)
    {
        return query.Where((FormattableString)$"id = {value}");
    }

    public static SelectQuery<Order> WhereTotalAmountGt(this SelectQuery<Order> query, decimal value)
    {
        return query.Where((FormattableString)$"total_amount > {value}");
    }
}
```

> [!WARNING]
> Remember that your class must use the `partial` modifier. Otherwise, the compiler will emit an error indicating that it cannot inject the auxiliary metadata for your entity.

## Strongly Typed Filters vs Expression Trees

The Source Generator creates auxiliary filtering extensions (`OrderFilters`) and DTO classes (`OrderFilter`). This allows you to build Queries where the condition is tied AOT to the database fields **without going through expression trees**. 

Although `Sql.From<Order>().Where(o => o.Id == 1)` is supported, using AOT filters (`Sql.From<Order>().WhereIdEq(1)` or `.ApplyFilter(new OrderFilter { IdEq = 1 })`) prevents additional memory allocations caused by the C# compiler when creating `Expression` nodes and avoids trimming warnings.

*(See the [performance guide](13-performance-and-benchmarks.md) for more details)*.
