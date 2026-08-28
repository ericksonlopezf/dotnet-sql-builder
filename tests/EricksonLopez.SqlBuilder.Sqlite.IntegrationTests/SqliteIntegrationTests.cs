// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Dapper;
using EricksonLopez.SqlBuilder.Dapper;
using EricksonLopez.SqlBuilder.Sqlite;
using EricksonLopez.SqlBuilder.Testing.DataBuilders;
using EricksonLopez.SqlBuilder.Testing.Domain;
using Microsoft.Data.Sqlite;
using Xunit;

namespace EricksonLopez.SqlBuilder.Sqlite.IntegrationTests;

[Trait("Category", "Integration")]
public class SqliteIntegrationTests : IAsyncLifetime
{
    private const string ConnectionString = "Data Source=:memory:;";
    private SqliteConnection _connection = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection(ConnectionString);
        await _connection.OpenAsync();
        
        await _connection.ExecuteAsync(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                username TEXT NOT NULL,
                email TEXT NOT NULL,
                password_hash TEXT NOT NULL,
                first_name TEXT NULL,
                last_name TEXT NULL,
                is_active INTEGER NOT NULL,
                email_verified INTEGER NOT NULL,
                created_at TEXT NOT NULL,
                last_login_at TEXT NULL,
                locked_until TEXT NULL,
                failed_login_attempts INTEGER NOT NULL
            );
        ");

        DapperExtensions.RegisterCompiler<SqliteConnection>(() => new SqliteCompiler());
        global::Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    public async Task DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task ExhaustiveCrud_ShouldWorkPerfectly()
    {
        // 1. INSERT (Single)
        var user1 = ObjectMother.CreateUser(id: 1, name: "Alice");
        user1.Id = 0; // let AUTOINCREMENT handle it
        var insertQuery = Sql.Insert(user1);
        var insertedCount = await _connection.ExecuteAsync(insertQuery);
        insertedCount.Should().Be(1);

        var insertedUser = (await _connection.QueryAsync<User>(Sql.From<User>().Select("*").Where(u => u.Username == "Alice"))).Single();
        insertedUser.Id.Should().BeGreaterThan(0);
        
        // 2. INSERT BULK (Massive)
        var usersToInsert = new List<User>
        {
            ObjectMother.CreateUser(id: 2, name: "Bob"),
            ObjectMother.CreateUser(id: 3, name: "Charlie"),
            ObjectMother.CreateUser(id: 4, name: "Diana")
        };
        foreach (var u in usersToInsert)
        {
            u.Id = 0; // auto-increment
        }

        var bulkInsertCount = await _connection.BulkInsertAsync(usersToInsert);
        // BulkInsertAsync returns count
        
        // 3. SELECT (Distinct, Limits, Filtering)
        var selectQuery = Sql.From<User>()
                             .Select("id", "username", "first_name", "last_name", "email", "password_hash", "is_active", "email_verified", "created_at", "failed_login_attempts")
                             .Where(u => u.IsActive == true)
                             .OrderByDescending(u => u.Id)
                             .Limit(2);
        var selected = (await _connection.QueryAsync<User>(selectQuery)).ToList();
        selected.Should().NotBeNull();
        selected.Count.Should().Be(2);

        // 4. UPDATE (Single)
        insertedUser.FirstName = "AliceUpdated";
        var updateQuery = Sql.Update<User>().Set(u => u.FirstName, "AliceUpdated").Where(u => u.Id == insertedUser.Id);
        var updateCount = await _connection.ExecuteAsync(updateQuery);
        updateCount.Should().Be(1);

        var updatedUser = (await _connection.QueryAsync<User>(Sql.From<User>().Select("*").Where(u => u.Id == insertedUser.Id))).Single();
        updatedUser.FirstName.Should().Be("AliceUpdated");
        
        // 5. UPDATE BULK (Massive)
        var bulkUpdateQuery = Sql.Update<User>()
            .Set(u => u.IsActive, false)
            .Where(u => u.Username == "Bob" || u.Username == "Charlie");
        var bulkUpdateCount = await _connection.ExecuteAsync(bulkUpdateQuery);
        bulkUpdateCount.Should().Be(2);

        // 6. DELETE (Single)
        var deleteQuery = Sql.Delete<User>().Where(u => u.Id == insertedUser.Id);
        var deleteCount = await _connection.ExecuteAsync(deleteQuery);
        deleteCount.Should().Be(1);

        // 7. DELETE BULK (Massive)
        var deleteBulkQuery = Sql.Delete<User>().Where(u => u.IsActive == false);
        var deleteBulkCount = await _connection.ExecuteAsync(deleteBulkQuery);
        deleteBulkCount.Should().Be(2);
        
        // Verify remaining
        var remaining = (await _connection.QueryAsync<User>(Sql.From<User>().Select("*"))).ToList();
        remaining.Count.Should().Be(1); // Diana should be the only one left
        remaining.Single().Username.Should().Be("Diana");
    }
}





