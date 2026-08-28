# Source Generator Reference — EricksonLopez.SqlBuilder

> **Package:** `EricksonLopez.SqlBuilder.SourceGenerators`
> **Type:** `IIncrementalGenerator` (Roslyn)
> **ADR:** [ADR-006](decisions/adr-006-source-generator-strategy.md), [ADR-001](decisions/adr-001-stryker-source-generator-exclusion.md)
> Last audit: 2026-08-14

---

## Purpose

The Source Generator eliminates reflection from the hot path by emitting all entity metadata,
reader parsers, and bulk serializers at compile time. It is the foundation of the AOT strategy.

---

## Trigger

The generator activates when a class, struct, or record is decorated with `[SqlEntity]`:

```csharp
using EricksonLopez.SqlBuilder.Annotations;

[SqlEntity("orders")]            // explicit table name
public partial class Order       // must be partial
{
    [SqlKey]                     // marks as primary key (ColumnFlags.PrimaryKey)
    public int Id { get; set; }
    
    public string CustomerId { get; set; } = "";
    public decimal TotalAmount { get; set; }
    
    [SqlGenerated]               // excludes from INSERT (ColumnFlags.Generated)
    public DateTime CreatedAt { get; set; }
    
    [SqlIgnore]                  // excludes from all SQL operations
    public string ComputedField => $"{CustomerId}-{Id}";
    
    [SqlIndex]                   // marks as indexed column
    public string Status { get; set; } = "";
}
```

---

## Generated Output

For the `Order` class above, the generator emits the following (partial class):

### 1. Table & Column Constants

```csharp
partial class Order : ISqlEntity, IEntityMetadataProvider<Order>, IBulkSerializer<Order>
{
    public const string TableName = "orders";

    public static class Columns
    {
        public const string Id           = "id";
        public const string CustomerId   = "customer_id";
        public const string TotalAmount  = "total_amount";
        public const string CreatedAt    = "created_at";
        public const string Status       = "status";
    }

    public static readonly string SelectAllTemplate =
        $"SELECT {Columns.Id}, {Columns.CustomerId}, {Columns.TotalAmount}, {Columns.CreatedAt}, {Columns.Status} FROM {TableName}";

    public static readonly IReadOnlyDictionary<string, string> PropertyMap = new Dictionary<string, string>
    {
        { nameof(Id),          Columns.Id },
        { nameof(CustomerId),  Columns.CustomerId },
        { nameof(TotalAmount), Columns.TotalAmount },
        { nameof(CreatedAt),   Columns.CreatedAt },
        { nameof(Status),      Columns.Status },
    };
```

### 2. ISqlEntity Implementation

```csharp
    public string GetTableName()   => TableName;
    public string[] GetColumnNames() => new[] { Columns.Id, Columns.CustomerId, Columns.TotalAmount, Columns.Status };
    // (CreatedAt excluded — [SqlGenerated])

    public object?[] GetValues()   => new object?[] { this.Id, this.CustomerId, this.TotalAmount, this.Status };
    public string[] GetAllColumnNames() => new[] { Columns.Id, Columns.CustomerId, Columns.TotalAmount, Columns.CreatedAt, Columns.Status };
    public object?[] GetAllValues() => new object?[] { this.Id, this.CustomerId, this.TotalAmount, this.CreatedAt, this.Status };
    public string[] GetIndexedColumns() => new string[] { Columns.Status };
    public IReadOnlyDictionary<string, string> GetPropertyMap() => PropertyMap;
```

### 3. SqlAlias (Typed Alias Helper)

```csharp
    public class SqlAlias
    {
        public string TableAlias { get; }
        public SqlAlias(string alias) { TableAlias = alias; }

        public string Id          => $"{TableAlias}.{Columns.Id}";
        public string CustomerId  => $"{TableAlias}.{Columns.CustomerId}";
        public string TotalAmount => $"{TableAlias}.{Columns.TotalAmount}";
        // ... all columns
    }
```

**Usage:**
```csharp
var o = new Order.SqlAlias("o");
Sql.From<Order>()
   .Select(o.Id, o.CustomerId, o.TotalAmount)
   .InnerJoin<Customer>("c", $"c.id = {o.CustomerId}");
```

### 4. Parser (O(1) IDataReader Hydration)

```csharp
    public class Parser
    {
        private bool _initialized;
        private int _ordinal_Id;
        private int _ordinal_CustomerId;
        // ... per column

        public void Initialize(IDataReader reader)
        {
            if (_initialized) return;
            _ordinal_Id         = reader.GetOrdinal(Columns.Id);
            _ordinal_CustomerId = reader.GetOrdinal(Columns.CustomerId);
            // ...
            _initialized = true;
        }

        public Order Parse(IDataReader reader)
        {
            Initialize(reader);
            var entity = new Order();
            if (!reader.IsDBNull(_ordinal_Id))         entity.Id = (int)reader.GetInt32(_ordinal_Id);
            if (!reader.IsDBNull(_ordinal_CustomerId)) entity.CustomerId = reader.GetString(_ordinal_CustomerId);
            // ...
            return entity;
        }
    }

    public static Func<IDataReader, Order> GetReaderParser()
    {
        var parser = new Parser();
        return parser.Parse;
    }

    public static Order FromReader(IDataReader reader)
    {
        var parser = new Parser();
        return parser.Parse(reader);
    }
```

**Usage with `QueryAotAsync<T>`:**
```csharp
var orders = await connection.QueryAotAsync<Order>(
    Sql.From<Order>().Where(o => o.Status == "active"),
    Order.FromReader,   // ← Source Generator emits this
    compiler);
```

### 5. AotMetadata (IEntityMetadata<T>)

```csharp
    private sealed class AotMetadata : IEntityMetadata<Order>
    {
        public static readonly AotMetadata Instance = new();
        public string TableName => Order.TableName;
        private static readonly ColumnMetadata[] _columns;

        static AotMetadata()
        {
            _columns = new ColumnMetadata[]
            {
                new(Columns.Id,          "Id",          ColumnFlags.PrimaryKey),
                new(Columns.CustomerId,  "CustomerId",  ColumnFlags.None),
                new(Columns.TotalAmount, "TotalAmount", ColumnFlags.None),
                new(Columns.CreatedAt,   "CreatedAt",   ColumnFlags.Generated),
                new(Columns.Status,      "Status",      ColumnFlags.None),
            };
        }

        public ReadOnlySpan<ColumnMetadata> Columns => _columns;

        public bool IsNull(Order entity, int columnIndex) => columnIndex switch
        {
            0 => false,                       // Id: value type
            1 => entity.CustomerId is null,   // string
            // ...
            _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
        };

        public bool IsDefault(Order entity, int columnIndex) => columnIndex switch
        {
            0 => EqualityComparer<int>.Default.Equals(entity.Id, default!),
            // ...
            _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
        };
    }

    public static IEntityMetadata<Order> Metadata => AotMetadata.Instance;
```

---

## Attribute Reference

| Attribute | Target | Effect |
|-----------|--------|--------|
| `[SqlEntity]` | class/record/struct | Triggers generator; sets table name (pluralized snake_case by default) |
| `[SqlEntity("table_name")]` | class/record/struct | Explicit table name |
| `[SqlKey]` | property | Sets `ColumnFlags.PrimaryKey`; included in INSERT, WHERE by AOT renderer |
| `[SqlGenerated]` | property | Sets `ColumnFlags.Generated`; excluded from INSERT columns |
| `[SqlIgnore]` | property | Excluded from all SQL operations (not even in `GetAllColumnNames`) |
| `[SqlIndex]` | property | Adds column to `GetIndexedColumns()` result |

---

## Naming Convention

Column names are derived from property names via `ToSnakeCase`:

| Property Name | Column Name |
|---------------|------------|
| `Id` | `id` |
| `CustomerId` | `customer_id` |
| `TotalAmount` | `total_amount` |
| `CreatedAt` | `created_at` |
| `HTTPStatusCode` | `h_t_t_p_status_code` *(limitation — acronyms not detected)* |

> **Note:** The `HTTPStatusCode` → `h_t_t_p_status_code` behavior is a known limitation.
> Use `[SqlColumn("http_status_code")]` to override *(not yet implemented — TD future item)*.

---

## Known Constraints

| Constraint | Notes |
|-----------|-------|
| Type must be `partial` | ESQL-005 diagnostic emitted if not |
| Nested types not supported | The type must be directly inside a namespace |
| Record structs: partial supported | `partial record struct` works |
| Generic types: not supported | `Entity<T>` will not be processed |

---

## Project Setup

```xml
<ItemGroup>
  <!-- Build-time only; PrivateAssets prevents transitive dependency -->
  <PackageReference Include="EricksonLopez.SqlBuilder.SourceGenerators" 
                    Version="1.1.*" 
                    PrivateAssets="all" />
  
  <!-- Also add the Analyzers package -->
  <PackageReference Include="EricksonLopez.SqlBuilder.Analyzers"
                    Version="1.1.*"
                    PrivateAssets="all" />
</ItemGroup>
```

---

*This document must be updated when new attributes, generated members, or generator behaviors are added.*
