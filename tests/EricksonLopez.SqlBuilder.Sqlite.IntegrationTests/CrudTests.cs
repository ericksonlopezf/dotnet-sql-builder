// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Dapper;
using EricksonLopez.SqlBuilder.Testing.Domain;
using Xunit;

namespace EricksonLopez.SqlBuilder.Sqlite.IntegrationTests;

[Collection("SqliteCollection")]
[Trait("Category", "Integration")]
public class CrudTests
{
    private readonly SqliteFixture _fixture;

    public CrudTests(SqliteFixture fixture)
    {
        _fixture = fixture;
        EricksonLopez.SqlBuilder.Dapper.DapperExtensions.RegisterCompiler<Microsoft.Data.Sqlite.SqliteConnection>(() => new SqliteCompiler());
        global::Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    [Fact]
    public async Task Can_Insert_And_Select_User()
    {
        // Arrange
        using var connection = _fixture.CreateConnection();
        var insertQuery = Sql.Insert<CrudUser>(new CrudUser 
            {
                Name = "John Doe",
                Age = 30,
                IsActive = true
            })
            .Returning(u => u.Id);

        // Act - Insert
        var insertedId = (await connection.QueryAsync<int>(insertQuery)).Single();

        insertedId.Should().BeGreaterThan(0);

        // Act - Select
        var selectQuery = Sql.From<CrudUser>()
            .Select("*")
            .Where(u => u.Id == insertedId);
        
        var user = (await connection.QueryAsync<CrudUser>(selectQuery)).SingleOrDefault();

        // Assert
        user.Should().NotBeNull();
        user!.Id.Should().Be(insertedId);
        user.Name.Should().Be("John Doe");
        user.Age.Should().Be(30);
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Can_Update_User()
    {
        // Arrange
        using var connection = _fixture.CreateConnection();
        var insertQuery = Sql.Insert<CrudUser>(new CrudUser { Name = "Update Me", Age = 20, IsActive = true })
            .Returning(u => u.Id);
        
        var insertedId = (await connection.QueryAsync<int>(insertQuery)).Single();

        // Act - Update
        var updateQuery = Sql.Update<CrudUser>()
            .Set(u => u.Name, "Updated Name")
            .Set(u => u.Age, 25)
            .Where(u => u.Id == insertedId);

        var rowsAffected = await connection.ExecuteAsync(updateQuery);

        rowsAffected.Should().Be(1);

        // Assert - Verify Update
        var selectQuery = Sql.From<CrudUser>().Select("*").Where(u => u.Id == insertedId);
        var user = (await connection.QueryAsync<CrudUser>(selectQuery)).Single();

        user.Name.Should().Be("Updated Name");
        user.Age.Should().Be(25);
    }

    [Fact]
    public async Task Can_Delete_User()
    {
        // Arrange
        using var connection = _fixture.CreateConnection();
        var insertQuery = Sql.Insert<CrudUser>(new CrudUser { Name = "Delete Me", Age = 99, IsActive = false })
            .Returning(u => u.Id);
        
        var insertedId = (await connection.QueryAsync<int>(insertQuery)).Single();

        // Act - Delete
        var deleteQuery = Sql.Delete<CrudUser>()
            .Where(u => u.Id == insertedId);

        var rowsAffected = await connection.ExecuteAsync(deleteQuery);

        rowsAffected.Should().Be(1);

        // Assert - Verify Delete
        var selectQuery = Sql.From<CrudUser>().Select("*").Where(u => u.Id == insertedId);
        var user = (await connection.QueryAsync<CrudUser>(selectQuery)).SingleOrDefault();

        user.Should().BeNull();
    }

    [Fact]
    public async Task Can_BulkInsert_Users()
    {
        // Arrange
        using var connection = _fixture.CreateConnection();
        var name1 = Guid.NewGuid().ToString("N");
        var name2 = Guid.NewGuid().ToString("N");
        var users = new[]
        {
            new CrudUser { Name = name1, Age = 20, IsActive = true },
            new CrudUser { Name = name2, Age = 25, IsActive = false }
        };

        // Act - Bulk Insert
        var bulkInsert = Sql.BulkInsert(users);
        await connection.ExecuteAsync(bulkInsert);

        // Assert
        var names = new List<string> { name1, name2 };
        var selectQuery = Sql.From<CrudUser>().Where(u => names.Contains(u.Name));
        var insertedUsers = (await connection.QueryAsync<CrudUser>(selectQuery)).ToList();
        
        insertedUsers.Should().HaveCount(2);
        insertedUsers.Should().AllSatisfy(u => u.Id.Should().BeGreaterThan(0));
    }

    [Fact]
    public async Task Can_BulkUpdate_Users()
    {
        // Arrange
        using var connection = _fixture.CreateConnection();
        var name1 = Guid.NewGuid().ToString("N");
        var name2 = Guid.NewGuid().ToString("N");
        var users = new[]
        {
            new CrudUser { Name = name1, Age = 20, IsActive = true },
            new CrudUser { Name = name2, Age = 25, IsActive = true }
        };

        await connection.ExecuteAsync(Sql.BulkInsert(users));
        var names = new List<string> { name1, name2 };
        var insertedUsers = (await connection.QueryAsync<CrudUser>(Sql.From<CrudUser>().Where(u => names.Contains(u.Name)))).ToList();
        var insertedIds = insertedUsers.Select(u => u.Id).ToList();

        // Act - Bulk Update
        var bulkUpdate = Sql.Update<CrudUser>()
            .Set(u => u.IsActive, false)
            .Set(u => u.Age, 99)
            .Where(u => insertedIds.Contains(u.Id));

        var rowsAffected = await connection.ExecuteAsync(bulkUpdate);

        // Assert
        rowsAffected.Should().Be(2);
        var updatedUsers = (await connection.QueryAsync<CrudUser>(Sql.From<CrudUser>().Where(u => insertedIds.Contains(u.Id)))).ToList();
        
        updatedUsers.Should().HaveCount(2);
        updatedUsers.Should().AllSatisfy(u => u.IsActive.Should().BeFalse());
        updatedUsers.Should().AllSatisfy(u => u.Age.Should().Be(99));
    }

    [Fact]
    public async Task Can_BulkDelete_Users()
    {
        // Arrange
        using var connection = _fixture.CreateConnection();
        var name1 = Guid.NewGuid().ToString("N");
        var name2 = Guid.NewGuid().ToString("N");
        var users = new[]
        {
            new CrudUser { Name = name1, Age = 20, IsActive = true },
            new CrudUser { Name = name2, Age = 25, IsActive = true }
        };

        await connection.ExecuteAsync(Sql.BulkInsert(users));
        var names = new List<string> { name1, name2 };
        var insertedUsers = (await connection.QueryAsync<CrudUser>(Sql.From<CrudUser>().Where(u => names.Contains(u.Name)))).ToList();
        var insertedIds = insertedUsers.Select(u => u.Id).ToList();

        // Act - Bulk Delete
        var bulkDelete = Sql.Delete<CrudUser>()
            .Where(u => insertedIds.Contains(u.Id));

        var rowsAffected = await connection.ExecuteAsync(bulkDelete);

        // Assert
        rowsAffected.Should().Be(2);
        var remainingUsers = (await connection.QueryAsync<CrudUser>(Sql.From<CrudUser>().Where(u => insertedIds.Contains(u.Id)))).ToList();
        
        remainingUsers.Should().BeEmpty();
    }
}





