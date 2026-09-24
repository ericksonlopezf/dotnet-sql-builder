// Copyright © Erickson Lopez. MIT License.
using System.Collections.Generic;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.PostgreSql;
using Xunit;

namespace EricksonLopez.SqlBuilder.Testing.UnitTests;

public class QueryComparerTests
{
    [Fact]
    public void Compare_WhenIdenticalQueries_ReturnsAreEqualTrue()
    {
        var compiler = new PostgreSqlCompiler();
        var q1 = Sql.From<TestingUser>().Where(u => u.Id == 10);
        var q2 = Sql.From<TestingUser>().Where(u => u.Id == 10);

        var result = QueryComparer.Compare(q1, q2, compiler);

        Assert.True(result.AreEqual);
        Assert.Empty(result.Differences);
    }

    [Fact]
    public void Compare_NormalizeWhitespace_ControlsFormattingTolerance()
    {
        var compiler = new PostgreSqlCompiler();
        var qWithSpaces = new RawQuery("SELECT   *   FROM   t");
        var qClean = new RawQuery("SELECT * FROM t");

        // When normalizeWhitespace is true, extra spaces are ignored (both when expected has spaces and when actual has spaces)
        var normalizedResult1 = QueryComparer.Compare(qWithSpaces, qClean, compiler, normalizeWhitespace: true);
        Assert.True(normalizedResult1.AreEqual);

        // Kills mutant 939 (where Normalize(resultActual.Sql) is skipped)
        var normalizedResult2 = QueryComparer.Compare(qClean, qWithSpaces, compiler, normalizeWhitespace: true);
        Assert.True(normalizedResult2.AreEqual);

        // When normalizeWhitespace is false, exact string comparison is used
        var rawResult = QueryComparer.Compare(qWithSpaces, qClean, compiler, normalizeWhitespace: false);
        Assert.False(rawResult.AreEqual);
        Assert.Contains(rawResult.Differences, d => d.StartsWith("SQL mismatch:\nExpected: SELECT   *   FROM   t\nActual:   SELECT * FROM t"));
    }

    [Fact]
    public void Compare_WhenSqlDiffers_ReportsSqlMismatch()
    {
        var compiler = new PostgreSqlCompiler();
        var q1 = new RawQuery("SELECT a FROM t");
        var q2 = new RawQuery("SELECT b FROM t");

        var result = QueryComparer.Compare(q1, q2, compiler);

        Assert.False(result.AreEqual);
        Assert.Contains(result.Differences, d => d.Contains("SQL mismatch:"));
    }

    [Fact]
    public void Compare_WhenParameterCountDiffers_ReportsParameterCountMismatch()
    {
        var compiler = new PostgreSqlCompiler();
        var q1 = new RawQuery("SELECT 1", new Dictionary<string, object?> { { "p0", 1 } });
        var q2 = new RawQuery("SELECT 1", new Dictionary<string, object?> { { "p0", 1 }, { "p1", 2 } });

        var result = QueryComparer.Compare(q1, q2, compiler);

        Assert.False(result.AreEqual);
        Assert.Contains(result.Differences, d => d.Contains("Parameter count mismatch: Expected 1, Actual 2"));
    }

    [Fact]
    public void Compare_WhenParameterMissingInActual_ReportsMissingParameter()
    {
        var compiler = new PostgreSqlCompiler();
        var q1 = new RawQuery("SELECT 1", new Dictionary<string, object?> { { "p0", 1 } });
        var q2 = new RawQuery("SELECT 1", new Dictionary<string, object?> { { "p1", 1 } });

        var result = QueryComparer.Compare(q1, q2, compiler);

        Assert.False(result.AreEqual);
        Assert.Contains(result.Differences, d => d.Contains("Missing parameter 'p0' in actual query."));
    }

    [Fact]
    public void Compare_WhenParameterValueDiffers_ReportsParameterValueMismatch()
    {
        var compiler = new PostgreSqlCompiler();
        var q1 = new RawQuery("SELECT 1", new Dictionary<string, object?> { { "p0", "expectedValue" } });
        var q2 = new RawQuery("SELECT 1", new Dictionary<string, object?> { { "p0", "actualValue" } });

        var result = QueryComparer.Compare(q1, q2, compiler);

        Assert.False(result.AreEqual);
        Assert.Contains(result.Differences, d => d.Contains("Parameter value mismatch for 'p0': Expected 'expectedValue', Actual 'actualValue'"));
    }

    [Fact]
    public void Compare_WhenAstNodeCountDiffers_ReportsNodeCountMismatch()
    {
        var compiler = new PostgreSqlCompiler();
        var q1 = Sql.From<TestingUser>().Where(u => u.Age > 18);
        var q2 = Sql.From<TestingUser>().Where(u => u.Age > 18).OrderBy(u => u.Name);

        var result = QueryComparer.Compare(q1, q2, compiler);

        Assert.False(result.AreEqual);
        Assert.Contains(result.Differences, d => d.Contains("AST Node count mismatch:"));
    }

    [Fact]
    public void Compare_WhenAstNodeTypeDiffers_ReportsNodeTypeMismatch()
    {
        var compiler = new PostgreSqlCompiler();
        var q1 = Sql.From<TestingUser>().OrderBy(u => u.Name);
        var q2 = Sql.From<TestingUser>().Where(u => u.Age > 18);

        var result = QueryComparer.Compare(q1, q2, compiler);

        Assert.False(result.AreEqual);
        Assert.Contains(result.Differences, d => d.Contains("AST Node type mismatch at index"));
    }

    [Fact]
    public void Compare_WhenNormalizeWhitespaceFalse_AndBothRawSqlMatch_AreEqualIsTrue()
    {
        var compiler = new PostgreSqlCompiler();
        var q1 = new RawQuery("SELECT   id   FROM   t");
        var q2 = new RawQuery("SELECT   id   FROM   t");

        var res = QueryComparer.Compare(q1, q2, compiler, normalizeWhitespace: false);
        Assert.True(res.AreEqual);
    }

    [Fact]
    public void Compare_EmptyAndWhitespaceSql_ReturnsEmptyNormalized()
    {
        var compiler = new PostgreSqlCompiler();
        var q1 = new StubQuery("   \t  \r\n  ");
        var q2 = new StubQuery("");

        var result = QueryComparer.Compare(q1, q2, compiler);
        Assert.True(result.AreEqual);

        // Kills mutant returning "Stryker was here!" on empty string
        var qExpected = new StubQuery("Stryker was here!");
        var qActualEmpty = new StubQuery("");
        var resEmpty = QueryComparer.Compare(qExpected, qActualEmpty, compiler);
        Assert.False(resEmpty.AreEqual);

        // Kills mutant replacing " " separator with ""
        var qWithSpace = new StubQuery("SELECT 1");
        var qNoSpace = new StubQuery("SELECT1");
        var resSpace = QueryComparer.Compare(qWithSpace, qNoSpace, compiler);
        Assert.False(resSpace.AreEqual);
    }

    [Fact]
    public void QueryComparerResult_ConstructsCorrectly()
    {
        var diffs = new List<string> { "diff1", "diff2" };
        var res = new QueryComparerResult(false, diffs);

        Assert.False(res.AreEqual);
        Assert.Equal(2, res.Differences.Count);
        Assert.Equal("diff1", res.Differences[0]);
    }
}
