# 04. Advanced SELECT Queries

The `SelectQuery<T>` builder is the most robust, supporting grouping, complex logical conditions, and counts.

## Logical Groups (AND / OR)

```csharp
var query = Sql.From<Customer>()
    .Where(c => c.IsActive == true && (c.Name == "A" || c.Name == "B"));
```

## Grouping (GROUP BY / HAVING)

The `GroupBy` method accepts column names as `string`, and `Having` takes strongly typed or raw expressions.

```csharp
var query = Sql.From<Order>()
    .GroupBy("CustomerId")
    .Having(o => o.TotalAmount > 1000m);
```

## Raw Aggregate Functions

For complex calculations for which you do not have a mapped C# model:

```csharp
var query = Sql.From<Order>()
    .RawSelect("CustomerId, COUNT(*) as OrderCount, SUM(TotalAmount) as Total")
    .GroupBy("CustomerId");
```
