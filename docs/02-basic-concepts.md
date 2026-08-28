# 02. Basic Concepts

To fully leverage EricksonLopez.SqlBuilder, it is necessary to understand the three pillars of its use: Entities, Compilers, and QueryBuilders.

## 1. Defining Entities
The `[SqlEntity]` attribute is used to indicate that the class represents a table.

```csharp
using EricksonLopez.SqlBuilder.Annotations;

[SqlEntity("Customers")]
public partial class Customer
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool IsActive { get; set; }
}
```
> **Important**: The class must be `partial` so that the Source Generator can inject the auto-generated metadata and filter code.

## 2. The Compiler
Before building the SQL string, you need to instantiate a compiler specific to the database you are using.

```csharp
var compiler = new PostgreSqlCompiler();
// var compiler = new SqlServerCompiler();
// var compiler = new SqliteCompiler();
```

## 3. Building Queries
The entry point is the static `Sql` class that exposes factory methods.

```csharp
var query = Sql.From<Customer>()
               .Where(c => c.IsActive == true)
               .OrderBy(c => c.Name);

var result = query.Build(compiler);

Console.WriteLine(result.Sql);
// Output: SELECT "Id", "Name", "IsActive" FROM "Customers" WHERE "IsActive" = @p0 ORDER BY "Name" ASC

Console.WriteLine(result.Parameters["@p0"]); // Output: True
```
