# Sql Class

**Namespace:** `EricksonLopez.SqlBuilder`  
**Assembly:** `EricksonLopez.SqlBuilder.dll`

Provides static factory methods acting as a unified entry point for constructing immutable query builders.

## Syntax
```csharp
public static class Sql
```

---

## Methods

### `From<T>()`

Initiates the construction of a `SELECT` query based on entity type `T`.

#### Signature
```csharp
public static SelectQuery<T> From<T>()
```

#### Type Parameters
`T`  
The entity type representing the source table. Must have the `[SqlEntity]` attribute or static entity metadata.

#### Return Value
`SelectQuery<T>`  
An immutable query builder ready to be chained with `.Where()`, `.Join()`, `.OrderBy()`, etc.

#### Exceptions
None. If entity metadata is missing, validation occurs during query compilation (`.Build()`).

#### Basic Example
```csharp
var query = Sql.From<User>().Where(u => u.Id == 1);
```

#### When to Use
Use as the standard entry point for read-only queries.

#### When NOT to Use
Do not use if constructing an un-aliased subquery where outer correlation requires manual alias scoping.

---

### `Insert<T>(T entity)`

Initiates the construction of an `INSERT` statement for the specified entity.

#### Signature
```csharp
public static InsertQuery<T> Insert<T>(T entity)
```

#### Parameters
`entity` (`T`)  
The entity instance containing values to insert.

#### Return Value
`InsertQuery<T>`  
An immutable insert query builder.

#### Remarks
Uses source-generated metadata when available for zero-allocation property resolution.
