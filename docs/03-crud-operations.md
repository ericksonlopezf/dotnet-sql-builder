# 03. CRUD Operations

This section details how to perform insertions, updates, and deletions.

## INSERT

```csharp
var newCustomer = new Customer { Name = "Acme Corp", IsActive = true };

var query = Sql.Insert(newCustomer);
var result = query.Build(new SqlServerCompiler());

// SQL: INSERT INTO [Customers] ([Name], [IsActive]) VALUES (@Name, @IsActive)
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

## MERGE / UPSERT
(Adaptive syntax depending on the engine)

```csharp
var query = Sql.Merge<Customer>().Into("Customers")
    // ...
```
