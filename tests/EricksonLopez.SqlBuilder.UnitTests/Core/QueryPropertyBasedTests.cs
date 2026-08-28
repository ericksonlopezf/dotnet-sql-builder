// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.MariaDb;
using EricksonLopez.SqlBuilder.MySql;
using EricksonLopez.SqlBuilder.Oracle;
using EricksonLopez.SqlBuilder.PostgreSql;
using EricksonLopez.SqlBuilder.Sqlite;
using EricksonLopez.SqlBuilder.SqlServer;
using EricksonLopez.SqlBuilder.Testing;
using EricksonLopez.SqlBuilder.Testing.Domain;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests.Core;

public class QueryPropertyBasedTests
{
    private static readonly ISqlCompiler[] AllCompilers = new ISqlCompiler[]
    {
        new SqlServerCompiler(),
        new PostgreSqlCompiler(),
        new MySqlCompiler(),
        new MariaDbCompiler(),
        new SqliteCompiler(),
        new OracleCompiler()
    };

    [Property]
    public void QueryImmutability_BranchingNeverMutatesParentQuery(NonNegativeInt extraWhereId, NonNegativeInt limit)
    {
        var baseQuery = Sql.From<User>().Where(u => u.IsActive);
        var baseNodeCount = baseQuery.Nodes.Length;
        var initialSqlServerSql = new SqlServerCompiler().Compile(baseQuery).Sql;

        var branched = baseQuery
            .Where(u => u.Id > extraWhereId.Get)
            .OrderBy(u => u.Username)
            .Limit(limit.Get);

        // Invariant 1: Base query node count remains invariant
        baseQuery.Nodes.Length.Should().Be(baseNodeCount);

        // Invariant 2: Base query compilation remains deterministic and unchanged
        new SqlServerCompiler().Compile(baseQuery).Sql.Should().Be(initialSqlServerSql);

        // Invariant 3: Branched query contains additional nodes
        branched.Nodes.Length.Should().BeGreaterThan(baseNodeCount);
    }

    [Property]
    public void LimitOffsetInvariants_PreservesPositiveValuesAcrossAllDialects(PositiveInt limit, PositiveInt offset)
    {
        var l = limit.Get;
        var o = offset.Get;

        var query = Sql.From<User>()
            .Where(u => u.IsActive)
            .OrderBy(u => u.Id)
            .Limit(l)
            .Offset(o);

        foreach (var compiler in AllCompilers)
        {
            var result = compiler.Compile(query);

            result.Sql.Should().NotBeNullOrWhiteSpace();
            var hasLimit = result.Sql.Contains(l.ToString()) || result.Parameters.Values.Any(v => v is int iv && iv == l);
            var hasOffset = result.Sql.Contains(o.ToString()) || result.Parameters.Values.Any(v => v is int iv && iv == o);

            hasLimit.Should().BeTrue();
            hasOffset.Should().BeTrue();
        }
    }

    [Property]
    public void ParameterManager_UniqueKeyRegistration_NeverCollidesOrLosesParameters(PositiveInt count)
    {
        // We do not artificially limit count.Get here to allow FsCheck to explore boundaries.
        // PositiveInt sizes grow according to FsCheck's size parameter.
        var n = count.Get;
        var pm = new ParameterManager();

        for (int i = 0; i < n; i++)
        {
            var pName = pm.Add($"val_{i}");
            pName.Should().NotBeNullOrWhiteSpace();
        }

        var dictionary = pm.GetParameters();
        dictionary.Count.Should().Be(n);
    }

    [Property]
    public void PaginationMath_CalculatesConsistentOffsetAcrossDialects(PositiveInt page, PositiveInt pageSize)
    {
        var p = page.Get;
        var s = pageSize.Get;
        var expectedOffset = (p - 1) * s;

        var query = Sql.From<User>()
            .OrderBy(u => u.Id)
            .Offset(expectedOffset)
            .Limit(s);

        foreach (var compiler in AllCompilers)
        {
            var result = compiler.Compile(query);
            result.Sql.Should().NotBeNullOrWhiteSpace();

            if (expectedOffset > 0)
            {
                var hasOffset = result.Sql.Contains(expectedOffset.ToString()) || result.Parameters.Values.Any(v => v is int iv && iv == expectedOffset);
                hasOffset.Should().BeTrue();
            }
        }
    }

    [Property]
    public void BetweenBoundaries_ContainsBothBoundsInParametersOrSql(int val1, int val2)
    {
        var lower = Math.Min(val1, val2);
        var upper = Math.Max(val1, val2);

        var query = Sql.From<User>()
            .Where(u => u.Id.Between(lower, upper));

        foreach (var compiler in AllCompilers)
        {
            var result = compiler.Compile(query);
            result.Sql.Should().NotBeNullOrWhiteSpace();

            var hasLower = result.Parameters.Values.Any(v => v is int iv && iv == lower) || result.Sql.Contains(lower.ToString());
            var hasUpper = result.Parameters.Values.Any(v => v is int iv && iv == upper) || result.Sql.Contains(upper.ToString());

            hasLower.Should().BeTrue();
            hasUpper.Should().BeTrue();
        }
    }
}
