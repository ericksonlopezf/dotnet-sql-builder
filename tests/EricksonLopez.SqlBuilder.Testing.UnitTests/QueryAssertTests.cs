// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.PostgreSql;
using Xunit;

namespace EricksonLopez.SqlBuilder.Testing.UnitTests;

public class QueryAssertTests
{
    [Fact]
    public void ParametersMatch_WhenMatching_Passes()
    {
        var result = new SqlResult("SELECT 1", new Dictionary<string, object>
        {
            { "p0", 42 },
            { "p1", "hello" }
        });

        QueryAssert.ParametersMatch(result, ("p0", 42), ("p1", "hello"));
    }

    [Fact]
    public void ParametersMatch_WhenCountDiffers_ThrowsEqualException()
    {
        var result = new SqlResult("SELECT 1", new Dictionary<string, object>
        {
            { "p0", 42 }
        });

        Assert.Throws<Xunit.Sdk.EqualException>(() =>
            QueryAssert.ParametersMatch(result, ("p0", 42), ("p1", 99)));
    }

    [Fact]
    public void ParametersMatch_WhenKeyMissing_ThrowsWithDescriptiveMessage()
    {
        var result = new SqlResult("SELECT 1", new Dictionary<string, object>
        {
            { "p0", 42 }
        });

        var ex = Assert.Throws<Xunit.Sdk.TrueException>(() =>
            QueryAssert.ParametersMatch(result, ("p99", 42)));

        Assert.Contains("Missing parameter: p99", ex.Message);
    }

    [Fact]
    public void ParametersMatch_WhenValueDiffers_ThrowsEqualException()
    {
        var result = new SqlResult("SELECT 1", new Dictionary<string, object>
        {
            { "p0", 42 }
        });

        Assert.Throws<Xunit.Sdk.EqualException>(() =>
            QueryAssert.ParametersMatch(result, ("p0", 99)));
    }

    [Fact]
    public void SqlMatches_NormalizesWhitespaceAndCompares()
    {
        var compiler = new PostgreSqlCompiler();
        var query = Sql.From<TestingUser>().Where(u => u.Id == 1);

        QueryAssert.SqlMatches(query, compiler, "SELECT   *   FROM \r\n \"testingusers\"   WHERE (id = @p0)");

        Assert.Throws<Xunit.Sdk.EqualException>(() =>
            QueryAssert.SqlMatches(query, compiler, "SELECT id FROM \"testingusers\""));
    }

    [Fact]
    public void SqlMatches_EmptyAndWhitespaceQueries_HandledGracefully()
    {
        var compiler = new PostgreSqlCompiler();
        var emptyQuery = new StubQuery("   \r\n\t   ");
        QueryAssert.SqlMatches(emptyQuery, compiler, "");
        QueryAssert.SqlMatches(emptyQuery, compiler, "   ");
    }

    [Fact]
    public void SqlMatches_DialectShortcuts_ExecuteCorrectly()
    {
        var query = Sql.From<TestingUser>().Where(u => u.Id == 1);

        QueryAssert.SqlMatchesPostgreSql(query, "SELECT * FROM \"testingusers\" WHERE (id = @p0)");
        QueryAssert.SqlMatchesSqlServer(query, "SELECT * FROM [testingusers] WHERE (id = @p0)");
        QueryAssert.SqlMatchesSqlite(query, "SELECT * FROM \"testingusers\" WHERE (id = @p0)");
        QueryAssert.SqlMatchesMySql(query, "SELECT * FROM `testingusers` WHERE (id = @p0)");
        QueryAssert.SqlMatchesOracle(query, "SELECT * FROM \"TESTINGUSERS\" WHERE (id = :p0)");
    }

    [Fact]
    public void QueriesMatch_WhenEquivalent_Passes()
    {
        var compiler = new PostgreSqlCompiler();
        var q1 = Sql.From<TestingUser>().Where(u => u.Age > 18);
        var q2 = Sql.From<TestingUser>().Where(u => u.Age > 18);

        QueryAssert.QueriesMatch(q1, q2, compiler);
    }

    [Fact]
    public void QueriesMatch_WhenDifferent_ThrowsXunitExceptionWithDescriptiveMessage()
    {
        var compiler = new PostgreSqlCompiler();
        var q1 = Sql.From<TestingUser>().Where(u => u.Age > 18);
        var q2 = Sql.From<TestingUser>().Where(u => u.IsActive);

        var ex = Assert.Throws<Xunit.Sdk.XunitException>(() =>
            QueryAssert.QueriesMatch(q1, q2, compiler));

        Assert.StartsWith("Queries do not match:\n", ex.Message);
        Assert.Contains("SQL mismatch:", ex.Message);
    }

    [Fact]
    public void QueriesMatch_WhenMultipleDifferences_SeparatesWithNewlines()
    {
        var compiler = new PostgreSqlCompiler();
        // q1 has WHERE (id = @p0) [1 param]
        var q1 = Sql.From<TestingUser>().Where(u => u.Id == 10);
        // q2 has SELECT "name" [0 params]
        var q2 = Sql.From<TestingUser>().Select("name");

        var ex = Assert.Throws<Xunit.Sdk.XunitException>(() =>
            QueryAssert.QueriesMatch(q1, q2, compiler));

        Assert.Contains("SQL mismatch:\n", ex.Message);
        Assert.Contains("\nParameter count mismatch:", ex.Message);
    }

    [Fact]
    public void SqlMatches_NormalizeWhitespace_DetectsStrykerMutations()
    {
        var compiler = new PostgreSqlCompiler();

        // Kills mutant replacing empty string with "Stryker was here!"
        var queryForEmpty = new StubQuery("Stryker was here!");
        Assert.Throws<Xunit.Sdk.EqualException>(() =>
            QueryAssert.SqlMatches(queryForEmpty, compiler, ""));

        // Kills mutant replacing space separator " " with ""
        var queryForSpace = new StubQuery("SELECT 1");
        Assert.Throws<Xunit.Sdk.EqualException>(() =>
            QueryAssert.SqlMatches(queryForSpace, compiler, "SELECT1"));
    }

    [Fact]
    public async Task VerifySql_InvokesSnapshotAssert()
    {
        var unverifiedResult = new SqlResult("UNVERIFIED_RESULT_" + Guid.NewGuid(), new Dictionary<string, object>());
        var task1 = QueryAssert.VerifySql(unverifiedResult);
        Assert.NotNull(task1);
        var ex1 = await Record.ExceptionAsync(() => task1);
        Assert.NotNull(ex1);
        Assert.IsNotType<ArgumentNullException>(ex1);

        var compiler = new PostgreSqlCompiler();
        var unverifiedQuery = new StubQuery("UNVERIFIED_QUERY_" + Guid.NewGuid());
        var task2 = QueryAssert.VerifySql(unverifiedQuery, compiler);
        Assert.NotNull(task2);
        var ex2 = await Record.ExceptionAsync(() => task2);
        Assert.NotNull(ex2);
        Assert.IsNotType<ArgumentNullException>(ex2);
    }
}
