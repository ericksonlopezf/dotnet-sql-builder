// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.PostgreSql;
using Xunit;

namespace EricksonLopez.SqlBuilder.Testing.UnitTests;

public class SnapshotAssertTests
{
    [Fact]
    public void MatchesSnapshot_NormalizeWhitespace_ControlsTolerance()
    {
        var compiler = new PostgreSqlCompiler();
        var query = new RawQuery("SELECT   id   FROM    t");

        // normalizeWhitespace true matches across different whitespace
        SnapshotAssert.MatchesSnapshot(query, compiler, "SELECT \r\n id \t FROM t", normalizeWhitespace: true);

        // normalizeWhitespace false fails when whitespace differs
        var ex = Assert.Throws<InvalidOperationException>(() =>
            SnapshotAssert.MatchesSnapshot(query, compiler, "SELECT \r\n id \t FROM t", normalizeWhitespace: false));

        Assert.Contains("Snapshot SQL Mismatch.", ex.Message);
        Assert.Contains("Expected:\nSELECT \r\n id \t FROM t", ex.Message);
        Assert.Contains("Actual:\nSELECT   id   FROM    t", ex.Message);
    }

    [Fact]
    public void MatchesSnapshot_WhenSqlDiffers_ThrowsWithExactMessage()
    {
        var compiler = new PostgreSqlCompiler();
        var query = Sql.From<TestingUser>().Select("name");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            SnapshotAssert.MatchesSnapshot(query, compiler, "SELECT \"id\" FROM \"testingusers\""));

        Assert.Contains("Snapshot SQL Mismatch.\nExpected:\nSELECT \"id\" FROM \"testingusers\"\nActual:\nSELECT \"name\" FROM \"testingusers\"", ex.Message);
    }

    [Fact]
    public void MatchesSnapshot_EmptyQuery_NormalizedToEmptyString()
    {
        var compiler = new PostgreSqlCompiler();
        var emptyQuery = new StubQuery("   \t  \r\n ");

        SnapshotAssert.MatchesSnapshot(emptyQuery, compiler, "");
        SnapshotAssert.MatchesSnapshot(emptyQuery, compiler, "   ");

        // Kills mutant returning "Stryker was here!" on empty string
        var qExpected = new StubQuery("Stryker was here!");
        Assert.Throws<InvalidOperationException>(() =>
            SnapshotAssert.MatchesSnapshot(qExpected, compiler, ""));
    }

    [Fact]
    public void MatchesContract_WhenMatching_Passes()
    {
        var query = Sql.From<TestingUser>().Where(u => u.Id == 1);
        var contract = query.GetContract();

        SnapshotAssert.MatchesContract(query, contract.Fingerprint);
    }

    [Fact]
    public void MatchesContract_WhenDifferent_ThrowsWithExactMessage()
    {
        var query = Sql.From<TestingUser>().Where(u => u.Id == 1);
        var contract = query.GetContract();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            SnapshotAssert.MatchesContract(query, "expected_fake_fingerprint"));

        Assert.Contains($"Query Contract Mismatch.\nExpected Fingerprint:\nexpected_fake_fingerprint\nActual Fingerprint:\n{contract.Fingerprint}", ex.Message);
    }

    [Fact]
    public async Task Verify_ExecutesVerification()
    {
        var unverifiedResult = new SqlResult("UNVERIFIED_" + Guid.NewGuid(), new Dictionary<string, object>());
        await Assert.ThrowsAnyAsync<Exception>(() => SnapshotAssert.Verify(unverifiedResult));

        var compiler = new PostgreSqlCompiler();
        var unverifiedQuery = new StubQuery("UNVERIFIED_" + Guid.NewGuid());
        await Assert.ThrowsAnyAsync<Exception>(() => SnapshotAssert.Verify(unverifiedQuery, compiler));

        var astQuery = Sql.From<TestingUser>().Where(u => u.Id == 999999);
        await Assert.ThrowsAnyAsync<Exception>(() => SnapshotAssert.VerifyContract(astQuery));
    }
}
