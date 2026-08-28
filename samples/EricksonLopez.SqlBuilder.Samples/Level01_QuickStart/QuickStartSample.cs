// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Annotations;
using EricksonLopez.SqlBuilder.Dapper;
using EricksonLopez.SqlBuilder.Sqlite;
using Microsoft.Data.Sqlite;

namespace EricksonLopez.SqlBuilder.Samples.Level01_QuickStart;

/// <summary>
/// Mapping for the 'users' table. The [SqlEntity] and [DatabaseGenerated] attributes
/// provide essential metadata for SqlBuilder.
/// </summary>
[SqlEntity("users")]
public partial class User
{
    [DatabaseGenerated] // Indicates the database generates this value (e.g. AUTOINCREMENT)
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public static class QuickStartSample
{
    public static async Task RunAsync()
    {
        Console.WriteLine("\n=== LEVEL 1: QUICK START ===");

        // 1. Minimal Configuration
        // Register SQLite compiler globally for Dapper
        DapperExtensions.RegisterCompiler<SqliteConnection>(() => new SqliteCompiler());
        
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        // 2. Database Preparation (Sample setup only)
        await connection.ExecuteAsync(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                email TEXT NOT NULL
            )");

        // 3. First Functional Operation (Insert)
        var newUser = new User { Name = "John Doe", Email = "john@example.com" };
        
        // Sql.Insert() infers table name from [SqlEntity]
        var insertQuery = Sql.Insert(newUser).Returning(u => u.Id);
        
        // Execute using transparent Dapper Extension Method
        var newId = (await connection.QueryAsync<int>(insertQuery)).Single();
        Console.WriteLine($"[+] Inserted user with Id: {newId}");

        // 4. First Functional Operation (Select)
        var selectQuery = Sql.From<User>()
                             .Where(u => u.Id == newId);
                             
        var fetchedUsers = await connection.QueryAsync<User>(selectQuery);
        var fetchedUser = fetchedUsers.SingleOrDefault();
        Console.WriteLine($"[+] Retrieved from DB: {fetchedUser?.Name} ({fetchedUser?.Email})");

        // 5. First Functional Operation (Delete)
        var deleteQuery = Sql.Delete<User>()
                             .Where(u => u.Id == newId);
                             
        await connection.ExecuteAsync(deleteQuery);
        Console.WriteLine($"[+] User deleted from DB.");
    }
}



