# 03. CRUD Operations

This section details how to perform insertions, updates, and deletions.

## INSERT

```csharp
var newCustomer = new Customer { Name = "Acme Corp", IsActive = true };

var query = Sql.Insert(newCustomer);
var result = query.Build(new SqlServerCompiler());

// SQL: INSERT INTO [Customers] ([Name], [IsActive]) VALUES (@p0, @p1)
```

## UPDATE

```csharp
var query = Sql.Update<Customer>()
    .Set(c => c.Name, "New Name")
    .Set(c => c.IsActive, false)
    .Where(c => c.Id == 10);

var result = query.Build(new SqlServerCompiler());

// SQL: UPDATE [Customers] SET [Name] = @p0, [IsActive] = @p1 WHERE [Id] = @p2
```

## DELETE

```csharp
var query = Sql.Delete<Customer>().Where(c => c.Id == 10);
var result = query.Build(new SqlServerCompiler());

// SQL: DELETE FROM [Customers] WHERE [Id] = @p0
```

## UPSERT (OnConflict / DoUpdate)

Instead of fragile and error-prone `MERGE` statements (which trigger Roslyn error **`ESQL026`**), use native conflict resolution:

### PostgreSQL / SQLite (ON CONFLICT DO UPDATE)
```csharp
var customer = new Customer { Id = 10, Name = "Acme Corp", IsActive = true };

var query = Sql.Insert(customer)
    .OnConflict(c => c.Id)
    .DoUpdate(c => new { c.Name, c.IsActive });
```

### MySQL (ON DUPLICATE KEY UPDATE)
```csharp
var query = Sql.Insert(customer)
    .OnConflict()
    .DoUpdate(c => new { c.Name, c.IsActive });
```
