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
using EricksonLopez.SqlBuilder.Oracle;
using EricksonLopez.SqlBuilder.Testing.DataBuilders;
using EricksonLopez.SqlBuilder.Testing.Domain;
using Oracle.ManagedDataAccess.Client;
using Testcontainers.Oracle;
using Xunit;

namespace EricksonLopez.SqlBuilder.Oracle.IntegrationTests;

[Trait("Category", "Integration")]
public class OracleBooleanHandler : SqlMapper.TypeHandler<bool>
{
    public override void SetValue(IDbDataParameter parameter, bool value) => parameter.Value = value ? 1 : 0;
    public override bool Parse(object value) => Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture) == 1;
}

[Trait("Category", "Integration")]
public class OracleIntegrationTests : IAsyncLifetime
{
    private readonly OracleContainer _oracleContainer = new OracleBuilder().Build();

    public async Task InitializeAsync()
    {
        await _oracleContainer.StartAsync();
        
        using var connection = new OracleConnection(_oracleContainer.GetConnectionString());
        await connection.ExecuteAsync(@"
            CREATE TABLE ""USERS"" (
                ""ID"" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                ""USERNAME"" VARCHAR2(100) NOT NULL,
                ""EMAIL"" VARCHAR2(100) NOT NULL,
                ""PASSWORD_HASH"" VARCHAR2(100) NOT NULL,
                ""FIRST_NAME"" VARCHAR2(100) NULL,
                ""LAST_NAME"" VARCHAR2(100) NULL,
                ""IS_ACTIVE"" NUMBER(1) NOT NULL,
                ""EMAIL_VERIFIED"" NUMBER(1) NOT NULL,
                ""CREATED_AT"" TIMESTAMP NOT NULL,
                ""LAST_LOGIN_AT"" TIMESTAMP NULL,
                ""LOCKED_UNTIL"" TIMESTAMP NULL,
                ""FAILED_LOGIN_ATTEMPTS"" INT NOT NULL
            )
        ");

        DapperExtensions.RegisterCompiler<OracleConnection>(() => new OracleCompiler());
        global::Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
        SqlMapper.AddTypeHandler(new OracleBooleanHandler());
    }

    public async Task DisposeAsync()
    {
        await _oracleContainer.DisposeAsync();
    }

    [Fact]
    public async Task ExhaustiveCrud_ShouldWorkPerfectly()
    {
        using var connection = new OracleConnection(_oracleContainer.GetConnectionString());
        
        // 1. INSERT (Single)
        var user1 = ObjectMother.CreateUser(id: 1, name: "Alice");
        user1.Id = 0; // let IDENTITY handle it
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
        // BulkInsertAsync returns count
        
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





