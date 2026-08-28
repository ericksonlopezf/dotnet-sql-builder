// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.Result;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Annotations;
using EricksonLopez.SqlBuilder.Dapper;
using EricksonLopez.SqlBuilder.Filters;
using EricksonLopez.SqlBuilder.Sqlite;
using Microsoft.Data.Sqlite;

namespace EricksonLopez.SqlBuilder.Samples.Level08_Customization;

// ─── Types for demonstration ──────────────────────────────────────────────────

public class TagCollection
{
    public List<string> Tags { get; set; } = new();
    public override string ToString() => string.Join(",", Tags);
}

[System.Text.Json.Serialization.JsonSerializable(typeof(TagCollection))]
internal sealed partial class TagCollectionJsonContext : System.Text.Json.Serialization.JsonSerializerContext { }

[SqlEntity("inventory_products")]
public partial class InventoryProduct
{
    [DatabaseGenerated] public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public TagCollection Tags { get; set; } = new();
}

[SqlEntity("widgets")]
public partial class Widget
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}

// ─── 1. ITypeHandler — Custom type serialization ──────────────────────────────

/// <summary>
/// Serializes/deserializes TagCollection to/from JSON for database storage.
/// Implements both Dapper's SqlMapper.TypeHandler and SqlBuilder's ITypeHandler.
/// </summary>
#pragma warning disable CS8765
public sealed class TagCollectionTypeHandler : SqlMapper.TypeHandler<TagCollection>, ITypeHandler
{
    public override TagCollection Parse(object value)
    {
        if (value is TagCollection tc) return tc;
        var json = value?.ToString();
        if (string.IsNullOrEmpty(json)) return new TagCollection();
        return JsonSerializer.Deserialize(json, TagCollectionJsonContext.Default.TagCollection)
               ?? new TagCollection();
    }

    public override void SetValue(IDbDataParameter parameter, TagCollection value)
    {
        parameter.Value = value == null 
            ? DBNull.Value 
            : (object)JsonSerializer.Serialize(value, TagCollectionJsonContext.Default.TagCollection);
        parameter.DbType = DbType.String;
    }

    // Explicit ITypeHandler implementation
    void ITypeHandler.SetValue(IDbDataParameter parameter, object? value)
    {
        if (value is TagCollection tc)
            SetValue(parameter, tc);
        else if (value != null)
        {
            parameter.Value = value.ToString();
            parameter.DbType = DbType.String;
        }
        else
        {
            parameter.Value = DBNull.Value;
        }
    }

    object? ITypeHandler.Parse(Type destinationType, object? value)
    {
        if (value is TagCollection tc)
        {
            // When ParameterManager calls Parse on a TagCollection object to prepare it for SQL binding,
            // serialize it to string (or format for DB parameter)
            return JsonSerializer.Serialize(tc, TagCollectionJsonContext.Default.TagCollection);
        }
        return Parse(value!);
    }
}
#pragma warning restore CS8765

// ─── 2. IParameterManager — Custom parameter management ──────────────────────

/// <summary>
/// A custom IParameterManager that logs every parameter being added.
/// Useful for debugging, auditing, or overriding naming conventions.
/// </summary>
public sealed class LoggingParameterManager : IParameterManager
{
    private readonly IParameterManager _inner;
    private readonly List<string> _log = new();

    public LoggingParameterManager(ISqlCompiler compiler)
    {
        _inner = compiler.CreateParameterManager();
    }

    public string Add(object? value)
    {
        var name = _inner.Add(value);
        _log.Add($"  [PARAM] Added unnamed → {name} = {value}");
        return name;
    }

    public string AddNamed(string name, object? value)
    {
        var paramName = _inner.AddNamed(name, value);
        _log.Add($"  [PARAM] Added named → {paramName} = {value}");
        return paramName;
    }

    public IReadOnlyDictionary<string, object?> GetParameters()
        => _inner.GetParameters();

    public IReadOnlyList<string> GetLog() => _log.AsReadOnly();
}

// ─── 3. ISqlFilter<T> — Custom reusable query filters ────────────────────────

/// <summary>
/// Specification filter: products with Price >= minPrice.
/// </summary>
public sealed class PriceAboveFilter : ISqlFilter<InventoryProduct>
{
    private readonly decimal _minPrice;
    public PriceAboveFilter(decimal minPrice) => _minPrice = minPrice;

    public SelectQuery<InventoryProduct> Apply(SelectQuery<InventoryProduct> query)
        => query.Where(p => p.Price >= _minPrice);
}

/// <summary>
/// Specification filter: products with a non-empty Name.
/// </summary>
public sealed class HasNameFilter : ISqlFilter<InventoryProduct>
{
    public SelectQuery<InventoryProduct> Apply(SelectQuery<InventoryProduct> query)
        => query.Where(p => p.Name != null);
}

// ─── Main Sample ─────────────────────────────────────────────────────────────

public static class CustomizationSample
{
    public static async Task RunAsync()
    {
        Console.WriteLine("\n=== LEVEL 8: CUSTOMIZATION AND EXTENSIBILITY ===");

        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        // Register type handlers BEFORE any use
        var tagHandler = new TagCollectionTypeHandler();
        DapperExtensions.RegisterTypeHandler<TagCollection>(tagHandler); // Registers with both SqlBuilder and Dapper
        DapperExtensions.RegisterCompiler<SqliteConnection>(() => new SqliteCompiler());

        await connection.ExecuteAsync(@"
            CREATE TABLE inventory_products (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT NOT NULL, price DECIMAL NOT NULL, tags TEXT NULL);
            CREATE TABLE widgets (id INTEGER PRIMARY KEY, name TEXT NOT NULL, is_enabled BOOLEAN NOT NULL);
            INSERT INTO widgets VALUES (1, 'Widget Alpha', 1), (2, 'Widget Beta', 0);
        ");

        // ────────────────────────────────────────────────────────────────────
        // 1. ITypeHandler — Complex type serialization
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 1. ITypeHandler — Tipos complejos (JSON en columna TEXT)");

        var product = new InventoryProduct
        {
            Name = "Laptop",
            Price = 1500m,
            Tags = new TagCollection { Tags = new List<string> { "electronics", "portable", "premium" } }
        };

        await connection.ExecuteAsync(Sql.Insert(product));
        Console.WriteLine("    Product with TagCollection inserted.");

        var fetchedProducts = await connection.QueryAsync<InventoryProduct>(
            Sql.From<InventoryProduct>().Where(p => p.Name == "Laptop"));
        var fetched = fetchedProducts.FirstOrDefault();
        Console.WriteLine($"    Recuperado: {fetched?.Name}, Tags: [{string.Join(", ", fetched?.Tags?.Tags ?? new List<string>())}]");

        // ────────────────────────────────────────────────────────────────────
        // 2. Custom IParameterManager — Parameter logging
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 2. Custom IParameterManager — Parameter logging");

        var compiler = new SqliteCompiler();
        var loggingParams = new LoggingParameterManager(compiler);

        var query = Sql.From<Widget>()
            .Where(w => w.IsEnabled == true)
            .And(w => w.Name == "Widget Alpha");

        var result = compiler.Compile(query, loggingParams);

        Console.WriteLine($"    SQL compilado: {result.Sql}");
        Console.WriteLine("    Logged parameters:");
        foreach (var logEntry in loggingParams.GetLog())
        {
            Console.WriteLine(logEntry);
        }

        // ────────────────────────────────────────────────────────────────────
        // 3. ISqlFilter<T> — Reusable filters (Specification Pattern)
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 3. ISqlFilter<T> — Reusable filters as Specifications");

        // Insert more products for filtering demo
        var products = new[]
        {
            new InventoryProduct { Name = "Mouse", Price = 50m, Tags = new TagCollection() },
            new InventoryProduct { Name = "Keyboard", Price = 80m, Tags = new TagCollection() },
            new InventoryProduct { Name = "Monitor", Price = 400m, Tags = new TagCollection() },
        };
        foreach (var p in products)
        {
            await connection.ExecuteAsync(Sql.Insert(p));
        }

        // Combine multiple filters (all are AND-ed together)
        var filteredQuery = Sql.From<InventoryProduct>()
            .ApplyFilters(
                new PriceAboveFilter(75m),
                new HasNameFilter());

        var filtered = await connection.QueryAsync<InventoryProduct>(filteredQuery);
        Console.WriteLine($"    Products with price >= 75: {filtered.Count()} found.");
        foreach (var p in filtered)
        {
            Console.WriteLine($"      - {p.Name}: ${p.Price}");
        }

        // ────────────────────────────────────────────────────────────────────
        // 4. ISqlCompiler.Compile(query, paramManager) — Low-level compilation
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 4. ISqlCompiler — Low-level compilation with IParameterManager");

        ISqlCompiler sqliteCompiler = new SqliteCompiler();
        IParameterManager paramManager = sqliteCompiler.CreateParameterManager();

        var rawQuery = Sql.From<Widget>().Where(w => w.Id == 1);
        var rawResult = sqliteCompiler.Compile(rawQuery, paramManager);

        Console.WriteLine($"    SQL compiled with IParameterManager: {rawResult.Sql}");
        Console.WriteLine($"    Parameters: {string.Join(", ", rawResult.Parameters.Select(p => $"{p.Key}={p.Value}"))}");

        // ────────────────────────────────────────────────────────────────────
        // 5. ISqlCompiler.Escape — Escapado de identificadores
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 5. ISqlCompiler.Escape — Escapado de identificadores");

        var escaped = sqliteCompiler.Escape("my table");
        Console.WriteLine($"    Escaped: {escaped}");

        var escapedId = sqliteCompiler.EscapeIdentifier("user_name");
        Console.WriteLine($"    EscapeIdentifier: {escapedId}");

        // ────────────────────────────────────────────────────────────────────
        // 6. DapperExtensions.RegisterTypeHandler — Dual registration (SqlBuilder+Dapper)
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 6. DapperExtensions.RegisterTypeHandler — Dual registration SqlBuilder+Dapper");
        Console.WriteLine("    DapperExtensions.RegisterTypeHandler<TagCollection>(handler)");
        Console.WriteLine("    → Automatically registers with Sql.RegisterTypeHandler<T>() AND SqlMapper.AddTypeHandler()");
        Console.WriteLine("    → Single registration point for both layers");
    }
}





