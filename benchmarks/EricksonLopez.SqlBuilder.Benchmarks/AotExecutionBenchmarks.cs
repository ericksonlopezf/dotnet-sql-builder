// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Annotations;
using EricksonLopez.SqlBuilder.SqlServer;

namespace EricksonLopez.SqlBuilder.Benchmarks;

/// <summary>
/// Benchmarks for NativeAOT paths (zero-reflection compilation, source-generated metadata, parameter resolution).
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory("AotExecution")]
public class AotExecutionBenchmarks
{
    private static readonly SqlServerCompiler Compiler = new SqlServerCompiler();
    private static readonly Customer SampleCustomer = new Customer
    {
        Id = 42,
        Name = "Acme Corp",
        Email = "contact@acme.corp",
        CreatedAt = DateTime.UtcNow,
        IsActive = true
    };

    /// <summary>
    /// Baseline: Raw string SQL construction without builder.
    /// </summary>
    [Benchmark(Baseline = true)]
    public string Baseline_RawString()
    {
        return "SELECT * FROM [Customer] WHERE [Id] = @p0 AND [IsActive] = @p1";
    }

    /// <summary>
    /// Measures AST creation and compilation in zero-reflection NativeAOT pipeline.
    /// </summary>
    [Benchmark]
    public string SqlBuilder_AotSelect_Compile()
    {
        var query = Sql.From<Customer>()
            .Where(c => c.Id == 42 && c.IsActive == true);
        return query.Build(Compiler).Sql;
    }

    /// <summary>
    /// Measures Source Generator [SqlEntity] zero-reflection metadata retrieval.
    /// </summary>
    [Benchmark]
    public string[] SourceGen_GetColumnNames()
    {
        if (SampleCustomer is ISqlEntity entity)
        {
            return entity.GetColumnNames();
        }
        return Array.Empty<string>();
    }

    /// <summary>
    /// Measures Source Generator [SqlEntity] zero-reflection values extraction.
    /// </summary>
    [Benchmark]
    public object?[] SourceGen_GetValues()
    {
        if (SampleCustomer is ISqlEntity entity)
        {
            return entity.GetValues();
        }
        return Array.Empty<object?>();
    }

    /// <summary>
    /// Measures RawQuery compilation with FormattableString interpolation in NativeAOT mode.
    /// </summary>
    [Benchmark]
    public SqlResult RawQuery_FormattableString_Compile()
    {
        int customerId = 42;
        bool active = true;
        var query = Sql.Raw($"SELECT * FROM [Customer] WHERE [Id] = {customerId} AND [IsActive] = {active}");
        return query.Build(Compiler);
    }

    /// <summary>
    /// PERF-001: Measures AOT INSERT statement rendering with stackalloc column bitmask.
    /// </summary>
    [Benchmark]
    public SqlResult AotPath_RenderInsert_Stackalloc()
    {
        var insert = Sql.Insert(SampleCustomer);
        return insert.Build(Compiler);
    }

    /// <summary>
    /// PERF-001: Measures AOT UPDATE statement rendering with stackalloc column bitmask.
    /// </summary>
    [Benchmark]
    public SqlResult AotPath_RenderUpdate_Stackalloc()
    {
        var update = Sql.Update(SampleCustomer).Where(c => c.Id == 42);
        return update.Build(Compiler);
    }
}


