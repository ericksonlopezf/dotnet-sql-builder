// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.PostgreSql;
using EricksonLopez.SqlBuilder.Testing;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests.Fuzzing;

public class PropertyBasedQueryFuzzingTests
{
    private static readonly PostgreSqlCompiler Compiler = new();

    [Fact]
    public void Invariant_Determinism_CompilingSameQueryProducesBitIdenticalOutput()
    {
        var query = Sql.From<DummyEntity>()
            .Where(u => u.Id == 42 && u.Name == "test@example.com")
            .OrderBy(u => u.Id)
            .Limit(25)
            .Offset(50);

        var baseline = Compiler.Compile(query);

        for (int i = 0; i < 500; i++)
        {
            var next = Compiler.Compile(query);
            next.Sql.Should().Be(baseline.Sql);
            next.Parameters.Count.Should().Be(baseline.Parameters.Count);
            foreach (var kv in baseline.Parameters)
            {
                next.Parameters[kv.Key].Should().Be(kv.Value);
            }
        }
    }

    [Fact]
    public void Invariant_ParameterConsistency_EveryPlaceholderHasMatchingParameterEntry()
    {
        var query = Sql.From<DummyEntity>()
            .Where(u => u.Id == 10)
            .And(u => u.Name == "alice")
            .Or(u => u.Version == 5);

        var result = Compiler.Compile(query);

        // Regex for @p0, @p1, etc.
        var matches = Regex.Matches(result.Sql, @"@p\d+");
        matches.Count.Should().Be(result.Parameters.Count);

        foreach (Match match in matches)
        {
            var paramKey = match.Value.TrimStart('@');
            result.Parameters.Should().ContainKey(paramKey);
        }
    }

    [Fact]
    public void Invariant_Stability_RepeatedRenderingDoesNotMutateBuilder()
    {
        var query1 = Sql.From<DummyEntity>().Where(u => u.Id == 1);
        var initialNodesCount = query1.Nodes.Length;

        _ = Compiler.Compile(query1);
        _ = Compiler.Compile(query1);
        _ = Compiler.Compile(query1);

        query1.Nodes.Length.Should().Be(initialNodesCount);

        var query2 = query1.Where(u => u.Name == "admin");
        query1.Nodes.Length.Should().Be(initialNodesCount);
        query2.Nodes.Length.Should().Be(initialNodesCount + 1);
    }

    [Fact]
    public void Invariant_Safety_AdversarialValuesDoNotAlterSqlStructure()
    {
        string[] adversarialValues = new[]
        {
            "' OR 1=1 --",
            "'; DROP TABLE users; --",
            "\" OR \"\"=\"",
            "\0",
            "\r\nUNION SELECT * FROM passwords --",
            "{{format_injection}}",
            new string('A', 10000)
        };

        foreach (var val in adversarialValues)
        {
            var query = Sql.From<DummyEntity>().Where(u => u.Name == val);
            var result = Compiler.Compile(query);

            // The value must NEVER be embedded into the raw SQL string
            result.Sql.Should().NotContain(val);
            result.Sql.Should().Contain("@p0");
            result.Parameters["p0"].Should().Be(val);
        }
    }
}
