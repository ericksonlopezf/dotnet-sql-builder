// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.PostgreSql;
using EricksonLopez.SqlBuilder.SqlServer;
using EricksonLopez.SqlBuilder.Testing;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests.Concurrency;

public class ConcurrencyStressTests
{
    private static readonly PostgreSqlCompiler PgCompiler = new();
    private static readonly SqlServerCompiler MsCompiler = new();

    [Theory]
    [InlineData(1)]
    [InlineData(8)]
    [InlineData(32)]
    [InlineData(64)]
    [InlineData(128)]
    public void ConcurrentQueryCompilation_AcrossThreads_IsThreadSafeAndDeterministic(int degreeOfParallelism)
    {
        const int iterationsPerThread = 50;
        var results = new ConcurrentBag<(string Sql, int ParamCount)>();

        Parallel.For(0, degreeOfParallelism, new ParallelOptions { MaxDegreeOfParallelism = degreeOfParallelism }, threadIndex =>
        {
            for (int i = 0; i < iterationsPerThread; i++)
            {
                var query = Sql.From<DummyEntity>()
                    .Where(u => u.Id == 100)
                    .And(u => u.Name == "admin")
                    .OrderBy(u => u.Id)
                    .Limit(10)
                    .Offset(20);

                var compiled = PgCompiler.Compile(query);
                results.Add((compiled.Sql, compiled.Parameters.Count));
            }
        });

        results.Count.Should().Be(degreeOfParallelism * iterationsPerThread);
        
        // Every single compiled result across all threads must be 100% identical
        var first = results.First();
        foreach (var r in results)
        {
            r.Sql.Should().Be(first.Sql);
            r.ParamCount.Should().Be(first.ParamCount);
        }
    }

    [Fact]
    public void ConcurrentSubqueryAndCteComposition_NoCrossTalkBetweenThreads()
    {
        const int threadCount = 64;
        var errors = new ConcurrentBag<Exception>();

        Parallel.For(0, threadCount, new ParallelOptions { MaxDegreeOfParallelism = threadCount }, threadId =>
        {
            try
            {
                for (int i = 0; i < 20; i++)
                {
                    var cteQuery = Sql.From<DummyEntity>()
                        .Where(u => u.Id > threadId);

                    var mainQuery = Sql.From<DummyEntity>()
                        .CTE("sub", cteQuery)
                        .Where(u => u.Name == $"user_{threadId}_{i}");

                    var res = PgCompiler.Compile(mainQuery);
                    res.Sql.Should().Contain("WITH \"sub\" AS (SELECT * FROM \"dummy_entity\" WHERE (id > @p0))");
                    res.Sql.Should().Contain("WHERE (name = @p1)");
                    res.Parameters["p0"].Should().Be(threadId);
                    res.Parameters["p1"].Should().Be($"user_{threadId}_{i}");
                }
            }
            catch (Exception ex)
            {
                errors.Add(ex);
            }
        });

        errors.Should().BeEmpty();
    }
}
