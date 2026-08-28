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
using EricksonLopez.SqlBuilder.MySql;
using EricksonLopez.SqlBuilder.Testing.DataBuilders;
using EricksonLopez.SqlBuilder.Testing.Domain;
using MySqlConnector;
using Testcontainers.MySql;
using Xunit;

namespace EricksonLopez.SqlBuilder.MySql.IntegrationTests;

[Trait("Category", "Integration")]
public class MySqlIntegrationTests : IAsyncLifetime
{
    private readonly MySqlContainer _mySqlContainer = new MySqlBuilder().Build();

    public async Task InitializeAsync()
    {
        await _mySqlContainer.StartAsync();
        
        using var connection = new MySqlConnection(_mySqlContainer.GetConnectionString());
        await connection.ExecuteAsync(@"
            CREATE TABLE users (
                id INT PRIMARY KEY AUTO_INCREMENT,
                username VARCHAR(100) NOT NULL,
                email VARCHAR(100) NOT NULL,
                password_hash VARCHAR(100) NOT NULL,
                first_name VARCHAR(100) NULL,
                last_name VARCHAR(100) NULL,
                is_active BOOLEAN NOT NULL,
                email_verified BOOLEAN NOT NULL,
                created_at DATETIME NOT NULL,
                last_login_at DATETIME NULL,
                locked_until DATETIME NULL,
                failed_login_attempts INT NOT NULL
            );
        ");

        DapperExtensions.RegisterCompiler<MySqlConnection>(() => new MySqlCompiler());
        global::Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    public async Task DisposeAsync()
    {
        await _mySqlContainer.DisposeAsync();
    }

    [Fact]
    public async Task ExhaustiveCrud_ShouldWorkPerfectly()
    {
        using var connection = new MySqlConnection(_mySqlContainer.GetConnectionString());
        
        // 1. INSERT (Single)
        var user1 = ObjectMother.CreateUser(id: 1, name: "Alice");
        user1.Id = 0; // let auto-increment handle it
        var insertQuery = Sql.Insert(user1);
        var insertedCount = await connection.ExecuteAsync(insertQuery);
        insertedCount.Should().Be(1);

        var insertedUser = (await connection.QueryAsync<User>(Sql.From<User>().Select("*").Where(u => u.Username == "Alice"))).Single();
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

        var bulkInsertCount = await connection.BulkInsertAsync(usersToInsert);
        // BulkInsertAsync returns 0 or number of rows depending on dialect
        
        // 3. SELECT (Distinct, Limits, Filtering)
        var selectQuery = Sql.From<User>()
                             .Select("id", "username", "first_name", "last_name", "email", "password_hash", "is_active", "email_verified", "created_at", "failed_login_attempts")
                             .Where(u => u.IsActive == true)
                             .OrderByDescending(u => u.Id)
                             .Limit(2);
        var selected = (await connection.QueryAsync<User>(selectQuery)).ToList();
        selected.Should().NotBeNull();
        selected.Count.Should().Be(2);

        // 4. UPDATE (Single)
        insertedUser.FirstName = "AliceUpdated";
        var updateQuery = Sql.Update<User>().Set(u => u.FirstName, "AliceUpdated").Where(u => u.Id == insertedUser.Id);
        var updateCount = await connection.ExecuteAsync(updateQuery);
        updateCount.Should().Be(1);

        var updatedUser = (await connection.QueryAsync<User>(Sql.From<User>().Select("*").Where(u => u.Id == insertedUser.Id))).Single();
        updatedUser.FirstName.Should().Be("AliceUpdated");
        
        // 5. UPDATE BULK (Massive)
        var bulkUpdateQuery = Sql.Update<User>()
            .Set(u => u.IsActive, false)
            .Where(u => u.Username == "Bob" || u.Username == "Charlie");
        var bulkUpdateCount = await connection.ExecuteAsync(bulkUpdateQuery);
        bulkUpdateCount.Should().Be(2);

        // 6. DELETE (Single)
        var deleteQuery = Sql.Delete<User>().Where(u => u.Id == insertedUser.Id);
        var deleteCount = await connection.ExecuteAsync(deleteQuery);
        deleteCount.Should().Be(1);

        // 7. DELETE BULK (Massive)
        var deleteBulkQuery = Sql.Delete<User>().Where(u => u.IsActive == false);
        var deleteBulkCount = await connection.ExecuteAsync(deleteBulkQuery);
        deleteBulkCount.Should().Be(2);
        
        // Verify remaining
        var remaining = (await connection.QueryAsync<User>(Sql.From<User>().Select("*"))).ToList();
        remaining.Count.Should().Be(1); // Diana should be the only one left
        remaining.Single().Username.Should().Be("Diana");
    }
}







