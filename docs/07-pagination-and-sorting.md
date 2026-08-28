# 07. Pagination and Sorting

One of the great benefits of the Agnostic (Multi-engine) approach is that pagination changes radically depending on whether you are in SQL Server, Oracle, or PostgreSQL. The compiler handles it automatically.

```csharp
var query = Sql.From<Customer>()
    .OrderBy(c => c.Name)
    .Offset(10)
    .Limit(5);
```

### Behavior by Engine

- **PostgreSQL / MySQL / SQLite**:
  `ORDER BY "Name" ASC LIMIT 5 OFFSET 10`

- **SQL Server**:
  `ORDER BY [Name] ASC OFFSET 10 ROWS FETCH NEXT 5 ROWS ONLY`

- **Oracle**:
  `ORDER BY "Name" ASC OFFSET 10 ROWS FETCH NEXT 5 ROWS ONLY`
