// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using EricksonLopez.SqlBuilder.SqlServer;

namespace EricksonLopez.SqlBuilder.Benchmarks;

/// <summary>
/// Category A benchmarks — AST construction and compilation performance.
/// Measures zero-alloc AOT path vs reflection path and raw string baseline.
/// </summary>
/// <remarks>
/// Run with: dotnet run -c Release --filter *CategoryA*
/// Compare against baseline (raw string interpolation) to validate overhead.
/// </remarks>
[MemoryDiagnoser]
[BenchmarkCategory("CategoryA")]
public class CategoryABenchmarks
{
    private static readonly SqlServerCompiler Compiler = new SqlServerCompiler();

    // ── Baselines (raw string SQL — zero framework overhead) ──────────────────

    /// <summary>
    /// Baseline: Raw string SQL construction. Zero allocation. Reference point.
    /// </summary>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("CategoryA", "Select", "Baseline")]
    public string Baseline_RawString_SimpleSelect()
    {
        return "SELECT * FROM [customers] WHERE [is_active] = @p0";
    }

    [Benchmark]
    [BenchmarkCategory("CategoryA", "Select", "Baseline")]
    public string Baseline_RawString_ComplexSelect()
    {
        return "SELECT * FROM [orders] o INNER JOIN [customers] c ON c.Id = o.CustomerId WHERE [total_amount] > @p0 ORDER BY [order_date] DESC";
    }

    // ── EricksonLopez.SqlBuilder (Visitor path) ───────────────────────────────

    [Benchmark]
    [BenchmarkCategory("CategoryA", "Select", "SqlBuilder")]
    public string SqlBuilder_SimpleSelect()
    {
        var query = Sql.From<Customer>().Where(c => c.IsActive == true);
        return query.Build(Compiler).Sql;
    }

    [Benchmark]
    [BenchmarkCategory("CategoryA", "Select", "SqlBuilder")]
    public string SqlBuilder_ComplexSelect()
    {
        var query = Sql.From<Order>()
            .InnerJoin("Customer", "c", "c.Id = Order.CustomerId")
            .Where(o => o.TotalAmount > 1000m)
            .OrderByDescending(o => o.OrderDate);
        return query.Build(Compiler).Sql;
    }

    [Benchmark]
    [BenchmarkCategory("CategoryA", "Select", "SqlBuilder")]
    public string SqlBuilder_SelectWithPagination()
    {
        var query = Sql.From<Order>()
            .Where(o => o.TotalAmount > 100m)
            .OrderBy(o => o.OrderDate)
            .Limit(20)
            .Offset(40);
        return query.Build(Compiler).Sql;
    }

    [Benchmark]
    [BenchmarkCategory("CategoryA", "Select", "SqlBuilder")]
    public string SqlBuilder_SelectWithGroupBy()
    {
        var query = Sql.From<Order>()
            .GroupBy("CustomerId")
            .Having(o => o.TotalAmount > 5000m);
        return query.Build(Compiler).Sql;
    }

    [Benchmark]
    [BenchmarkCategory("CategoryA", "Select", "SqlBuilder")]
    public string SqlBuilder_SelectWithCte()
    {
        var cte = Sql.From<Order>().Where(o => o.TotalAmount > 0m);
        var query = Sql.From<Customer>()
            .CTE("high_value_orders", cte);
        return query.Build(Compiler).Sql;
    }

    // ── INSERT benchmarks ─────────────────────────────────────────────────────

    [Benchmark]
    [BenchmarkCategory("CategoryA", "Insert", "Baseline")]
    public string Baseline_RawString_Insert()
    {
        return "INSERT INTO [customers] ([name], [email], [is_active]) VALUES (@p0, @p1, @p2)";
    }

    [Benchmark]
    [BenchmarkCategory("CategoryA", "Insert", "SqlBuilder")]
    public string SqlBuilder_InsertSingleEntity()
    {
        var entity = new Customer { Name = "Alice", Email = "alice@test.com", IsActive = true };
        return Sql.Insert(entity).Build(Compiler).Sql;
    }

    // ── UPDATE benchmarks ─────────────────────────────────────────────────────

    [Benchmark]
    [BenchmarkCategory("CategoryA", "Update", "Baseline")]
    public string Baseline_RawString_Update()
    {
        return "UPDATE [customers] SET [is_active] = @p0 WHERE [id] = @p1";
    }

    [Benchmark]
    [BenchmarkCategory("CategoryA", "Update", "SqlBuilder")]
    public string SqlBuilder_UpdateWithWhere()
    {
        var query = Sql.Update<Customer>()
            .Set<bool>(c => c.IsActive, false)
            .Where(c => c.Id == 1);
        return query.Build(Compiler).Sql;
    }

    // ── DELETE benchmarks ─────────────────────────────────────────────────────

    [Benchmark]
    [BenchmarkCategory("CategoryA", "Delete", "Baseline")]
    public string Baseline_RawString_Delete()
    {
        return "DELETE FROM [customers] WHERE [id] = @p0";
    }

    [Benchmark]
    [BenchmarkCategory("CategoryA", "Delete", "SqlBuilder")]
    public string SqlBuilder_DeleteWithWhere()
    {
        var query = Sql.Delete<Customer>().Where(c => c.Id == 1);
        return query.Build(Compiler).Sql;
    }

    // ── Raw SQL benchmarks (parameter binding only) ───────────────────────────

    [Benchmark]
    [BenchmarkCategory("CategoryA", "Raw", "SqlBuilder")]
    public string SqlBuilder_RawQuery()
    {
        var name = "Alice";
        var query = Sql.Raw($"SELECT * FROM customers WHERE name = {name}");
        return query.Build(Compiler).Sql;
    }

    // ── Cursor pagination benchmarks ──────────────────────────────────────────

    [Benchmark]
    [BenchmarkCategory("CategoryA", "Pagination", "SqlBuilder")]
    public string SqlBuilder_KeysetPagination_SingleKey()
    {
        var lastId = 1000;
        var query = Sql.From<Order>()
            .Where(o => o.Id > lastId)
            .OrderBy(o => o.Id)
            .Limit(20);
        return query.Build(Compiler).Sql;
    }

    [Benchmark]
    [BenchmarkCategory("CategoryA", "Pagination", "SqlBuilder")]
    public string SqlBuilder_CompositeCursorPagination()
    {
        var query = Sql.From<Order>()
            .SeekAfter(
                new Abstractions.Nodes.CursorKey("order_date", new DateTime(2024, 1, 1)),
                new Abstractions.Nodes.CursorKey("id", 1000))
            .OrderBy(o => o.OrderDate)
            .ThenBy(o => o.Id)
            .Limit(20);
        return query.Build(Compiler).Sql;
    }

    // ── CASE expression benchmarks ────────────────────────────────────────────

    [Benchmark]
    [BenchmarkCategory("CategoryA", "Case", "SqlBuilder")]
    public string SqlBuilder_CaseExpression()
    {
        var query = Sql.From<Order>()
            .SelectCase(c => c
                .When("total_amount > {0}", 1000m).Then("'Premium'")
                .When("total_amount > {0}", 500m).Then("'Standard'")
                .Else("'Basic'")
                .As("tier"));
        return query.Build(Compiler).Sql;
    }

    // ── AST Collection benchmarks (PERF-004: ImmutableArray vs List) ───────────

    private static readonly Abstractions.ISqlNode SampleNode1 = new Abstractions.Nodes.RawSelectNode("id, name", null, false);
    private static readonly Abstractions.ISqlNode SampleNode2 = new Abstractions.Nodes.RawWhereNode("is_active = 1");
    private static readonly Abstractions.ISqlNode SampleNode3 = new Abstractions.Nodes.LimitOffsetNode(10, 0);

    /// <summary>
    /// PERF-004: Measures ImmutableArray&lt;ISqlNode&gt; construction and enumeration (production AST engine).
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("CategoryA", "AST", "ImmutableArray")]
    public int Ast_ImmutableArray_AllocationAndEnumeration()
    {
        var builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<Abstractions.ISqlNode>(3);
        builder.Add(SampleNode1);
        builder.Add(SampleNode2);
        builder.Add(SampleNode3);
        var array = builder.MoveToImmutable();

        int count = 0;
        foreach (var node in array)
        {
            if (node != null) count++;
        }
        return count;
    }

    /// <summary>
    /// PERF-004: Measures standard List&lt;ISqlNode&gt; allocation and enumeration (comparison baseline).
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("CategoryA", "AST", "List")]
    public int Ast_List_AllocationAndEnumeration()
    {
        var list = new List<Abstractions.ISqlNode>(3)
        {
            SampleNode1,
            SampleNode2,
            SampleNode3
        };

        int count = 0;
        foreach (var node in list)
        {
            if (node != null) count++;
        }
        return count;
    }
}




