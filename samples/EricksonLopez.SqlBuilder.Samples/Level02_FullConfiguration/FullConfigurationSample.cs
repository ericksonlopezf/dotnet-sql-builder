// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Annotations;
using EricksonLopez.SqlBuilder.Dapper;
using EricksonLopez.SqlBuilder.Sqlite;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace EricksonLopez.SqlBuilder.Samples.Level02_FullConfiguration;

[System.Text.Json.Serialization.JsonSerializable(typeof(Metadata))]
internal sealed partial class MetadataJsonContext : System.Text.Json.Serialization.JsonSerializerContext { }

#pragma warning disable CS8765
// 1. Serialization (Basic type mapping customization)
public class JsonTypeHandler<T> : SqlMapper.TypeHandler<T>, ITypeHandler
{
    public override T Parse(object value)
    {
        if (value is T tValue) return tValue;
        var json = value?.ToString();
        return string.IsNullOrEmpty(json) ? default : (T)JsonSerializer.Deserialize(json, typeof(T), MetadataJsonContext.Default)!;
    }

    public override void SetValue(IDbDataParameter parameter, T value)
    {
        parameter.Value = value == null ? DBNull.Value : JsonSerializer.Serialize(value, typeof(T), MetadataJsonContext.Default);
        parameter.DbType = DbType.String;
    }

    // Explicit implementation for EricksonLopez.SqlBuilder.Abstractions.ITypeHandler
    void ITypeHandler.SetValue(IDbDataParameter parameter, object? value)
    {
        SetValue(parameter, (T)value!);
    }

    object? ITypeHandler.Parse(Type destinationType, object? value)
    {
        return Parse(value!);
    }
}
#pragma warning restore CS8765

public class Metadata { public string CreatedBy { get; set; } = string.Empty; }

[SqlEntity("products")]
public partial class Product
{
    [DatabaseGenerated] public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public Metadata Meta { get; set; } // Complex type that needs JSON serialization
}

public static class FullConfigurationSample
{
    public static async Task RunAsync()
    {
        Console.WriteLine("\n=== LEVEL 2: FULL CONFIGURATION ===");

        // 2. Logging and Diagnostics
        // In a production application we would use LoggerFactory.Create(builder => ...)
        var loggerFactory = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
        
        SqlBuilderDiagnostics.LoggerFactory = loggerFactory;
        SqlBuilderDiagnostics.LogParameters = true; // For development debugging
        SqlBuilderDiagnostics.SlowQueryThresholdMs = 50; // Record queries > 50ms

        // 3. Dapper Configuration and Type Handlers
        SqlMapper.AddTypeHandler(new JsonTypeHandler<Metadata>()); // For Dapper
        Sql.RegisterTypeHandler<Metadata>(new JsonTypeHandler<Metadata>());  // For explicit SqlBuilder parameters
        DapperExtensions.RegisterCompiler<SqliteConnection>(() => new SqliteCompiler());

        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await connection.ExecuteAsync(@"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                price DECIMAL NOT NULL,
                meta TEXT NULL
            )");

        // 4. AOT Builders (High Performance)
        var product = new Product { Name = "Laptop", Price = 1500m, Meta = new Metadata { CreatedBy = "System" } };
        
        // Sql.InsertAot creates an InsertBuilder<T> (Fast-path) if available;
        // Sql.Insert is used for this showcase.
        var aotInsert = Sql.Insert(product)
            .Build(new SqliteCompiler()); 

        await connection.ExecuteAsync(aotInsert.Sql, aotInsert.Parameters);
        Console.WriteLine("[+] Product inserted with AOT Builder (excluding generated, ignoring nulls).");

        // 5. Extension Methods
        // Update price
        var updateQuery = Sql.Update<Product>()
            .Set(p => p.Price, 2000m)
            .Where(p => p.Name == "Laptop");
        await connection.ExecuteAsync(updateQuery);

        // Extension Methods Usage (OrderByDynamic and Pagination)
        var selectQuery = Sql.From<Product>()
            .OrderByDynamic("Name", descending: true)
            .Limit(10)
            .Offset(0);

        var items = await connection.QueryAsync<Product>(selectQuery);
        foreach (var item in items)
        {
            Console.WriteLine($"[+] Product retrieved: {item.Name} - Price: {item.Price} - Created by: {item.Meta?.CreatedBy}");
        }
    }
}




