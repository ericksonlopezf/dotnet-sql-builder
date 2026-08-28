# API Reference - EricksonLopez.SqlBuilder

Detailed Microsoft Learn style reference for the main methods of the `Sql` static class, the entry point of the library.

---

## `Sql.Select<T>()`

Starts building a `SELECT` query strongly typed to the specified generic type.

### Signature
```csharp
public static SelectQuery<T> Select<T>()
```

### Type Parameters
- `T`: The entity or model type the query will be based on. The table name is inferred via the `[SqlEntity]` attribute.

### Return
- `SelectQuery<T>`: An immutable builder representing the initial query structure.

### Remarks
The `SelectQuery<T>` object is immutable. Each invocation of methods like `.Where()` will return a *new* builder instance with the added node.

### Basic Example
```csharp
var query = Sql.From<User>()
               .Where(u => u.IsActive);
```

### Advanced Example
```csharp
var query = Sql.From<User>()
               .Select("id", "name")
               .Where(u => u.Role == "Admin")
               .OrderBy(u => u.CreatedAt)
               .Limit(10);
```

### Best Practices
- Prefer using lambda expressions `u => u.Property` over raw strings to allow safe refactoring and protection against SQL Injection.

---

## `Sql.Insert<T>(T entity)`

Generates an `INSERT` query from the provided model.

### Signature
```csharp
public static InsertQuery<T> Insert<T>(T entity)
```

### Parameters
- `entity` (`T`): The entity instance to insert.

### Return
- `InsertQuery<T>`: A builder that allows configuring `RETURNING`, `ON CONFLICT`, or other post-insertion behaviors clauses.

### Remarks
The library uses reflection or AOT optimizations (via `Sql.InsertAot`) to extract the entity's property values. Properties marked with `[DatabaseGenerated]` will be ignored during insertion.

### Basic Example
```csharp
var user = new User { Name = "Alice" };
var insertQuery = Sql.Insert(user).Returning(u => u.Id);
```

### Common Errors
- Providing manual values for properties marked as auto-generated; they will be ignored in the database unless their inclusion is forced via an explicit `InsertBuilder`.

---

## `Sql.Update<T>()`

Starts building a typed `UPDATE` query.

### Signature
```csharp
public static IUpdateSetBuilder<T> Update<T>()
```
```csharp
public static IUpdateSetBuilder<T> Update<T>(T entity)
```

### Parameters
- `entity` (`T`) (Optional): If provided, update values will be taken from the mapped properties of the entity.

### Return
- `IUpdateSetBuilder<T>`: A fluent interface that forces defining `SET` clauses or advancing to `WHERE`.

### When NOT to use it
- If you only want to update a couple of properties of a very large object (and didn't send the full entity). Instead, use `.Set(u => u.Prop, value)`.

### Basic Example
```csharp
var update = Sql.Update<User>()
                .Set(u => u.IsActive, false)
                .Where(u => u.LastLogin < DateTime.UtcNow.AddYears(-1));
```

---

## `Sql.Delete<T>()`

Starts building a safe `DELETE` query.

### Signature
```csharp
public static IDeleteFromBuilder<T> Delete<T>()
```

### Return
- `IDeleteFromBuilder<T>`: The entry point to apply filters to the deletion.

### Remarks
By design, `DeleteQuery<T>` explicitly requires a `.Where()` or a `.WhereAll()`. A `DELETE` query cannot be compiled without this scope confirmation.

### Basic Example
```csharp
var delete = Sql.Delete<User>()
                .Where(u => u.Id == 100);
```

### Exceptions
Will throw `InvalidOperationException` if `Build()` is called without specifying a filter clause.

---

## `Sql.Merge<T>()`

Starts a `MERGE` synchronization query (also known as UPSERT).

### Signature
```csharp
public static MergeQuery<T> Merge<T>()
```

### Return
- `MergeQuery<T>`: A builder that forces completing the `Using`, `On`, and cross-logical condition stages.

### Performance
Very efficient for massive conditional updates. Requires support in the underlying engine or it will be emulated (e.g. `INSERT ON CONFLICT` for PostgreSQL or SQLite).

---

## `Sql.MultiMap<TReturn>()`

Initiates a multi-mapping builder for joining multiple entities via Dapper.

### Signature
```csharp
public static MultiMapBuilder<TReturn> MultiMap<TReturn>()
```

### Return
- `MultiMapBuilder<TReturn>`: A fluent interface to configure the split-on parameters and the mapping function.

### Basic Example
```csharp
var query = Sql.From<Order>()
               .InnerJoin<User>((o, u) => o.UserId == u.Id);

var results = await connection.QueryAsync(
    query,
    Sql.MultiMap<Order>()
       .Map<User>((order, user) => 
       {
           order.User = user;
           return order;
       })
       .SplitOn("Id")
);
```

---

## `AotQueryExecutor`

A reflection-free ADO.NET execution layer available in `EricksonLopez.SqlBuilder.Aot`.

### Signature
```csharp
public static class AotQueryExecutor
{
    public static Task<List<T>> QueryAsync<T>(IDbConnection connection, ISqlQuery query, IDataReaderMapper<T> mapper, CancellationToken ct = default);
    public static Task<int> ExecuteAsync(IDbConnection connection, ISqlQuery query, CancellationToken ct = default);
}
```

### Remarks
This executor completely bypasses Dapper. It uses the source-generated `IDataReaderMapper<T>` to materialize results without any runtime reflection, making it fully NativeAOT safe.

### Basic Example
```csharp
var query = Sql.From<User>().Where(u => u.IsActive);
// Uses the source-generated mapper for User
var mapper = UserSqlMapper.Instance;

var users = await AotQueryExecutor.QueryAsync(connection, query, mapper);
```
