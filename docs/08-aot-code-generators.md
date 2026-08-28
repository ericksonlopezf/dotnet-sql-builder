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

The analyzer produces static filtering (`OrderFilter`) and projection classes that expose methods like `IdEq`, `TotalAmountGt`, eliminating the need for the engine to read `Expression<Func<T, bool>>` expression trees.

```csharp
// Auto-generated AOT Code (IL emitted at compile time)
public static class OrderFilter 
{
    public static SelectQuery<Order> IdEq(this SelectQuery<Order> query, int value)
    {
        return query.Where("Id = @Id").WithParam("Id", value);
    }
}
```

> [!WARNING]
> Remember that your class must use the `partial` modifier. Otherwise, the compiler will emit an error indicating that it cannot inject the auxiliary metadata for your entity.

## Strongly Typed Filters vs Expression Trees

The Source Generator also creates auxiliary filtering classes, for example, `OrderFilter`. This allows you to build Queries where the condition is tied AOT to the database fields **without going through expression trees**. 

Although `Sql.From<Order>().Where(o => o.Id == 1)` is supported, using AOT filters (`Sql.From<Order>().IdEq(1)`) prevents additional memory allocations caused by the C# compiler when creating `Expression` nodes.

*(See the [performance guide](13-performance-and-benchmarks.md) for more details)*.
