# 05. Joins and Relationships

The SqlBuilder provides support for `INNER JOIN`, `LEFT JOIN`, and `RIGHT JOIN`. For performance and simplicity of the AOT compiler, the conditions are specified in a strongly typed raw format.

```csharp
var query = Sql.From<Order>()
    .InnerJoin("Customers", "c", "c.Id = Order.CustomerId")
    .Where(o => o.TotalAmount > 500m);

var result = query.Build(new PostgreSqlCompiler());
```

### Projecting Columns from Multiple Tables

```csharp
var query = Sql.From<Order>()
    .InnerJoin("Customers", "c", "c.Id = Order.CustomerId")
    .Select("Order.Id", "Order.TotalAmount", "c.Name", "c.Email");
```
This will return a SQL string that will select only the requested columns.
