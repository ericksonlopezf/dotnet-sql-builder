// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data; // needed for IDbConnection, IDbTransaction etc.
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Result;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Annotations;
using EricksonLopez.SqlBuilder.Dapper;
using EricksonLopez.SqlBuilder.Filters;
using EricksonLopez.SqlBuilder.Sqlite;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace EricksonLopez.SqlBuilder.Samples.Level10_EnterpriseArchitecture;

// ─── Entities ────────────────────────────────────────────────────────────────

[SqlEntity("enterprise_users")]
public partial class EnterpriseUser
{
    [DatabaseGenerated] public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public int Version { get; set; }
}

[SqlEntity("audit_logs")]
public partial class AuditLog
{
    [DatabaseGenerated] public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public DateTime OccurredAt { get; set; }
}

// ─── Query Filters (Specifications) ─────────────────────────────────────────

public class AdminUsersFilter : ISqlFilter<EnterpriseUser>
{
    public SelectQuery<EnterpriseUser> Apply(SelectQuery<EnterpriseUser> query)
        => query.Where(u => u.Role == "Admin");
}

// ─── Repository Pattern ──────────────────────────────────────────────────────

public interface IUserRepository
{
    Task<EnterpriseUser?> GetByIdAsync(int id);
    Task CreateAsync(EnterpriseUser user);
    Task<int> UpdateWithConcurrencyAsync(EnterpriseUser original, EnterpriseUser current);
    Task<IReadOnlyList<EnterpriseUser>> GetPagedAsync(int pageNumber, int pageSize);
    Task<IReadOnlyList<EnterpriseUser>> FindAsync(params ISqlFilter<EnterpriseUser>[] filters);
}

public sealed class UserRepository : IUserRepository
{
    private readonly IDbConnection _connection;

    public UserRepository(IDbConnection connection) => _connection = connection;

    public async Task<EnterpriseUser?> GetByIdAsync(int id)
    {
        var result = await Sql.From<EnterpriseUser>()
            .Where(u => u.Id == id)
            .Limit(1)
            .ToResultAsync(_connection);

        return result.IsSuccess ? result.Value!.FirstOrDefault() : null;
    }

    public async Task CreateAsync(EnterpriseUser user)
    {
        await _connection.ExecuteAsync(Sql.Insert(user));
    }

    public async Task<int> UpdateWithConcurrencyAsync(EnterpriseUser original, EnterpriseUser current)
    {
        try
        {
            // ApplyDiff compares original and current, emitting only changed columns as SET assignments
            var updateQuery = ((Sql.Update<EnterpriseUser>() as IUpdateSetBuilder<EnterpriseUser>)!
                .ApplyDiff(original, current) as UpdateQuery<EnterpriseUser>)!
                .Where(u => u.Id == original.Id)
                .WithConcurrencyToken(u => u.Version, expectedValue: original.Version);

            return await _connection.ExecuteWithConcurrencyCheckAsync<EnterpriseUser>(updateQuery);
        }
        catch (DbConcurrencyException)
        {
            throw; // Caller handles concurrency conflict
        }
    }

    public async Task<IReadOnlyList<EnterpriseUser>> GetPagedAsync(int pageNumber, int pageSize)
    {
        var pagedResult = await Sql.From<EnterpriseUser>()
            .OrderBy(u => u.Id)
            .ToPagedListAsync(_connection, pageNumber, pageSize);

        // IPagedList<T> extends IReadOnlyList<T> — cast directly, no .Items property
        return pagedResult.IsSuccess ? (IReadOnlyList<EnterpriseUser>)pagedResult.Value! : Array.Empty<EnterpriseUser>();
    }

    public async Task<IReadOnlyList<EnterpriseUser>> FindAsync(params ISqlFilter<EnterpriseUser>[] filters)
    {
        var query = Sql.From<EnterpriseUser>().ApplyFilters(filters);
        var result = await query.ToResultAsync(_connection);
        return result.IsSuccess ? result.Value! : Array.Empty<EnterpriseUser>();
    }
}

// ─── Dependency Injection Configuration ──────────────────────────────────────

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqlBuilderServices(
        this IServiceCollection services, string connectionString)
    {
        services.AddTransient<IDbConnection>(_ =>
        {
            var conn = new SqliteConnection(connectionString);
            conn.Open();
            return conn;
        });

        services.AddTransient<IUserRepository, UserRepository>();

        // Register the compiler once (before first usage)
        DapperExtensions.RegisterCompiler<SqliteConnection>(() => new SqliteCompiler());

        return services;
    }
}

// ─── Main Sample ─────────────────────────────────────────────────────────────

public static class EnterpriseArchitectureSample
{
    public static async Task RunAsync()
    {
        Console.WriteLine("\n=== LEVEL 10: ENTERPRISE ARCHITECTURE (DI, REPO, CQRS) ===");

        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        DapperExtensions.RegisterCompiler<SqliteConnection>(() => new SqliteCompiler());

        await connection.ExecuteAsync(@"
            CREATE TABLE enterprise_users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                username TEXT NOT NULL,
                email TEXT NOT NULL,
                role TEXT NOT NULL DEFAULT 'User',
                version INTEGER NOT NULL DEFAULT 1
            );
            CREATE TABLE audit_logs (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                action TEXT NOT NULL,
                entity_name TEXT NOT NULL,
                entity_id INTEGER NOT NULL,
                occurred_at DATETIME NOT NULL
            );
            INSERT INTO enterprise_users (username, email, role, version) VALUES
                ('alice', 'alice@example.com', 'Admin', 1),
                ('bob', 'bob@example.com', 'User', 1),
                ('carol', 'carol@example.com', 'User', 1);
        ");

        // ────────────────────────────────────────────────────────────────────
        // 1. Repository Pattern with DI
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 1. Repository Pattern — Data access layer separation");

        var services = new ServiceCollection()
            .AddSqlBuilderServices("Data Source=:memory:")
            .BuildServiceProvider();

        // The repository is injected by the DI container
        var userRepo = new UserRepository(connection);

        var alice = await userRepo.GetByIdAsync(1);
        Console.WriteLine($"    GetById(1): {alice?.Username} ({alice?.Email})");

        // ────────────────────────────────────────────────────────────────────
        // 2. ApplyDiff — Differential UPDATE (modified columns only)
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 2. ApplyDiff — Differential UPDATE (changed columns only)");

        var original = alice!;
        var updated = new EnterpriseUser
        {
            Id = original.Id,
            Username = original.Username, // same
            Email = "alice@newdomain.com",   // changed
            Role = original.Role,            // same
            Version = original.Version
        };

        // ApplyDiff will only emit SET for changed columns (email in this case)
        var diffSql = ((Sql.Update<EnterpriseUser>() as IUpdateSetBuilder<EnterpriseUser>)!
            .ApplyDiff(original, updated) as UpdateQuery<EnterpriseUser>)!
            .Where(u => u.Id == original.Id)
            .WithConcurrencyToken(u => u.Version, expectedValue: original.Version)
            .Build(new SqliteCompiler());

        Console.WriteLine($"    ApplyDiff SQL:\n    {diffSql.Sql}");

        // ────────────────────────────────────────────────────────────────────
        // 3. Specification Pattern via ISqlFilter<T>
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 3. Specification Pattern — ISqlFilter<T> applied by Repository");

        var admins = await userRepo.FindAsync(new AdminUsersFilter());
        Console.WriteLine($"    Administradores encontrados: {admins.Count}");
        foreach (var admin in admins)
        {
            Console.WriteLine($"      - {admin.Username} ({admin.Role})");
        }

        // ────────────────────────────────────────────────────────────────────
        // 4. CTE (Common Table Expressions) Enterprise Pattern
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 4. CTE — Common Table Expressions for complex reports");

        // CTE 1: Summarize audit logs per user
        var auditInsert = Sql.Insert(new AuditLog { Action = "CREATE", EntityName = "User", EntityId = 1, OccurredAt = DateTime.UtcNow });
        await connection.ExecuteAsync(auditInsert);

        var cteQuery = Sql.From<EnterpriseUser>()
            .Select("enterprise_users.id", "enterprise_users.username", "enterprise_users.role")
            .CTE("admin_users",
                Sql.From<EnterpriseUser>().Where(u => u.Role == "Admin"))
            .Join("admin_users", "au", "enterprise_users.id = au.id");

        var cteSql = cteQuery.Build(new SqliteCompiler());
        Console.WriteLine($"    CTE SQL:\n    {cteSql.Sql}");

        // ────────────────────────────────────────────────────────────────────
        // 5. Multi-compiler support — SqlServer vs PostgreSQL vs SQLite
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 5. Multi-Compiler — Misma query, distintos dialectos SQL");

        var universalQuery = Sql.From<EnterpriseUser>()
            .Where(u => u.Role == "Admin")
            .OrderBy(u => u.Username)
            .Limit(10);

        // Note: SqlServerCompiler and PostgreSqlCompiler would be from separate NuGet packages
        var sqliteResult = universalQuery.Build(new SqliteCompiler());

        Console.WriteLine($"    SQLite   : {sqliteResult.Sql}");
        Console.WriteLine("    SqlServer: (would use TOP 10, not LIMIT 10)");
        Console.WriteLine("    PostgreSQL: (identical to SQLite for basic queries)");

        // ────────────────────────────────────────────────────────────────────
        // 6. CQRS Pattern — Commands vs Queries separation
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 6. CQRS Pattern — Separation of Commands and Queries");

        // COMMAND: Write side — InsertQuery, UpdateQuery, DeleteQuery
        Console.WriteLine("    [COMMAND] Insert, Update, Delete → use immutable query builders");
        var insertCmd = Sql.Insert(new EnterpriseUser { Username = "dave", Email = "dave@example.com", Role = "User", Version = 1 });
        var updateCmd = Sql.Update<EnterpriseUser>().Set(u => u.Email, "dave2@example.com").Where(u => u.Username == "dave");
        var deleteCmd = Sql.Delete<EnterpriseUser>().Where(u => u.Username == "dave");

        await connection.ExecuteAsync(insertCmd);
        await connection.ExecuteAsync(updateCmd);
        await connection.ExecuteAsync(deleteCmd);

        // QUERY: Read side — SelectQuery + projections, pagination, streaming
        Console.WriteLine("    [QUERY]   Select, Project, Page → use immutable select builders");
        var listQuery = Sql.From<EnterpriseUser>()
            .OrderBy(u => u.Username)
            .Limit(3)
            .WithTag("cqrs-list-query");

        var allUsers = await connection.QueryAsync<EnterpriseUser>(listQuery);
        Console.WriteLine($"    Active users: {allUsers.Count()}");

        // ────────────────────────────────────────────────────────────────────
        // 7. Observabilidad — Tags + OpenTelemetry (conceptual demo)
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 7. Observabilidad — WithTag + OpenTelemetry (ActivitySource)");

        // Tags are added to OpenTelemetry Activity spans automatically
        SqlBuilderDiagnostics.LogParameters = false; // Production: don't log parameter values
        var taggedQuery = Sql.From<EnterpriseUser>()
            .Where(u => u.Role == "Admin")
            .WithTag("enterprise-admin-report");

        Console.WriteLine($"    Query tag: '{taggedQuery.Tag}'");
        Console.WriteLine($"    ActivitySource: '{SqlBuilderDiagnostics.ActivitySource.Name}'");
        Console.WriteLine($"    Log parameters: {SqlBuilderDiagnostics.LogParameters}");
    }
}




