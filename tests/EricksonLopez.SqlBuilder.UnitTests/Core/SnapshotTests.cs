// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Testing;
using VerifyXunit;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests.Core;

public class SnapshotTests
{
    [Fact]
    public Task SelectQuery_With_Complex_Conditions_Should_Match_Snapshot()
    {
        var query = Sql.From<TestEntity>()
            .Where(u => true)
            .And(u => false)
            .Or(u => true)
            .OrderBy(u => u.Name)
            .Limit(10);

        var compiler = new EricksonLopez.SqlBuilder.PostgreSql.PostgreSqlCompiler();
        var result = compiler.Compile(query);

        return Verifier.Verify(new { result.Sql, result.Parameters });
    }

    [Fact]
    public Task InsertQuery_With_OnConflict_Should_Match_Snapshot()
    {
        var query = Sql.Insert(new TestEntity { Id = 1, Name = "Test" })
            .OnConflict(x => x.Id)
            .DoUpdate(x => x.Name);

        var compiler = new EricksonLopez.SqlBuilder.PostgreSql.PostgreSqlCompiler();
        var result = compiler.Compile(query);

        return Verifier.Verify(new { result.Sql, result.Parameters });
    }
}





