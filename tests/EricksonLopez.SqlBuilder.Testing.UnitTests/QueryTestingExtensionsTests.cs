// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.PostgreSql;
using Xunit;

namespace EricksonLopez.SqlBuilder.Testing.UnitTests;

public class QueryTestingExtensionsTests
{
    [Fact]
    public void ShouldGenerate_WhenMatchingWithoutParameters_Passes()
    {
        var compiler = new PostgreSqlCompiler();
        var query = Sql.From<TestingUser>().Select("id");

        query.ShouldGenerate(compiler, "SELECT \"id\" FROM \"testingusers\"");
    }

    [Fact]
    public void ShouldGenerate_WhenMatchingWithParameters_Passes()
    {
        var compiler = new PostgreSqlCompiler();
        var query = Sql.From<TestingUser>().Where(u => u.Id == 10 && u.Age > 20);

        query.ShouldGenerate(compiler, "SELECT * FROM \"testingusers\" WHERE ((id = @p0) AND (age > @p1))", 10, 20);
    }

    [Fact]
    public void ShouldGenerate_WhenSqlDiffers_ThrowsWithExactMessage()
    {
        var compiler = new PostgreSqlCompiler();
        var query = Sql.From<TestingUser>().Select("id");

        var ex = Assert.Throws<Exception>(() =>
            query.ShouldGenerate(compiler, "SELECT \"name\" FROM \"testingusers\""));

        Assert.Contains("SQL mismatch.\nExpected:\nSELECT \"name\" FROM \"testingusers\"\nActual:\nSELECT \"id\" FROM \"testingusers\"", ex.Message);
    }

    [Fact]
    public void ShouldGenerate_WhenParameterCountDiffers_ThrowsWithExactMessage()
    {
        var compiler = new PostgreSqlCompiler();
        var query = Sql.From<TestingUser>().Where(u => u.Id == 10);

        var ex = Assert.Throws<Exception>(() =>
            query.ShouldGenerate(compiler, "SELECT * FROM \"testingusers\" WHERE (id = @p0)", 10, 20));

        Assert.Contains("Parameter count mismatch. Expected 2, but got 1.", ex.Message);
    }

    [Fact]
    public void ShouldGenerate_WhenParameterValueDiffers_ThrowsWithExactMessage()
    {
        var compiler = new PostgreSqlCompiler();
        var query = Sql.From<TestingUser>().Where(u => u.Id == 10);

        var ex = Assert.Throws<Exception>(() =>
            query.ShouldGenerate(compiler, "SELECT * FROM \"testingusers\" WHERE (id = @p0)", 99));

        Assert.Contains("Parameter at index 0 mismatch. Expected '99', but got '10'.", ex.Message);
    }

    [Fact]
    public void ShouldGenerate_WhitespaceAndEmptySql_NormalizedCorrectly()
    {
        var compiler = new PostgreSqlCompiler();
        var wsQuery = new StubQuery("  SELECT   1 \r\n \t ");

        wsQuery.ShouldGenerate(compiler, "SELECT 1");

        var emptyQuery = new StubQuery("   \t  \r\n ");
        emptyQuery.ShouldGenerate(compiler, "");
        emptyQuery.ShouldGenerate(compiler, "   ");

        // Kills mutant returning "Stryker was here!" on empty string
        var qExpected = new StubQuery("Stryker was here!");
        Assert.Throws<Exception>(() => qExpected.ShouldGenerate(compiler, ""));
    }

    [Fact]
    public void ShouldGenerate_WhenExpectedParametersIsNull_DoesNotThrowNullReferenceException()
    {
        var compiler = new PostgreSqlCompiler();
        var query = new StubQuery("SELECT 1");
        // Passing null array should safely skip parameter verification without throwing NullReferenceException
        query.ShouldGenerate(compiler, "SELECT 1", (object?[]?)null!);
    }

    [Fact]
    public void ShouldGenerate_WhenNoExpectedParametersProvided_SkipsParameterValidation()
    {
        var compiler = new PostgreSqlCompiler();
        // Query has 1 parameter (id = @p0)
        var queryWithParams = Sql.From<TestingUser>().Where(u => u.Id == 10);
        // When no expected parameters are provided, ShouldGenerate skips parameter checking even if the query has parameters (kills mutant 1005)
        queryWithParams.ShouldGenerate(compiler, "SELECT * FROM \"testingusers\" WHERE (id = @p0)");
    }

    [Fact]
    public async Task VerifyQueryAsync_ExecutesVerification()
    {
        var compiler = new PostgreSqlCompiler();
        var unverifiedQuery = new StubQuery("UNVERIFIED_" + Guid.NewGuid());
        var task = unverifiedQuery.VerifyQueryAsync(compiler);
        Assert.NotNull(task);
        var ex = await Record.ExceptionAsync(() => task);
        Assert.NotNull(ex);
        Assert.IsNotType<ArgumentNullException>(ex);
    }
}
