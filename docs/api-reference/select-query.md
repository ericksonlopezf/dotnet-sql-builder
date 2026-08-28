# SelectQuery&lt;T&gt; Class

**Namespace:** `EricksonLopez.SqlBuilder`  
**Assembly:** `EricksonLopez.SqlBuilder.dll`

Represents an immutable SQL `SELECT` query under construction. Each chained method returns a new query instance preserving the preceding AST state.

## Syntax
```csharp
public class SelectQuery<T> : ISqlQuery
```

---

## Methods

### `Where(Expression<Func<T, bool>> predicate)`

Applies a typed filter predicate to the query.

#### Signature
```csharp
public SelectQuery<T> Where(Expression<Func<T, bool>> predicate)
```

#### Parameters
`predicate` (`Expression<Func<T, bool>>`)  
The boolean lambda expression evaluated against the entity.

#### Return Value
`SelectQuery<T>`  
A new immutable query instance containing the added WHERE node.

#### Remarks
The expression is translated into parameterized SQL via `SqlExpressionVisitor`.

#### Basic Example
```csharp
var query = Sql.From<User>().Where(u => u.Email == "test@example.com");
```

#### Advanced Example
```csharp
var query = Sql.From<User>()
    .Where(u => u.Age > 18 && (u.IsActive || u.Id == 99));
```

---

### `Build(ISqlCompiler compiler)`

Compiles the immutable AST into executable SQL and parameter bindings.

#### Signature
```csharp
public SqlResult Build(ISqlCompiler compiler)
```

#### Parameters
`compiler` (`ISqlCompiler`)  
The dialect compiler instance (e.g., `new PostgreSqlCompiler()`, `new SqlServerCompiler()`).

#### Return Value
`SqlResult`  
Contains the rendered `Sql` string and the `Parameters` dictionary.
