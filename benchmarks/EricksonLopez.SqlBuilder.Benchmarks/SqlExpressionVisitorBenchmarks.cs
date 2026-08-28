// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;

namespace EricksonLopez.SqlBuilder.Benchmarks;

/// <summary>
/// Benchmarks for AST creation.
/// </summary>
[MemoryDiagnoser]
public class SqlExpressionVisitorBenchmarks
{
    private class User 
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public bool IsActive { get; set; }

    }

    /// <summary>Benchmarks manual AST instantiation.</summary>
    [Benchmark(Baseline = true)]
    public ISqlNode ManualAstInstantiation()
    {
        return new ExpressionWhereNode(
            Expression.Equal(
                Expression.Property(
                    Expression.Parameter(typeof(User), "u"),
                    typeof(User).GetProperty("Id")!
                ),
                Expression.Constant(1)
            ),
            IsOr: false
        );
    }

    private static readonly Expression<Func<User, bool>> PrecompiledExpr = u => u.Id == 1 && u.IsActive;
    private static readonly Func<User, bool> CompiledDelegate = PrecompiledExpr.Compile();
    private static readonly User SampleUser = new() { Id = 1, IsActive = true, Name = "Test" };

    /// <summary>Benchmarks AST generation using expression trees.</summary>
    [Benchmark]
    public ISqlNode SqlExpressionVisitor()
    {
        Expression<Func<User, bool>> expr = u => u.Id == 1;
        return new ExpressionWhereNode(expr.Body, IsOr: false);
    }

    /// <summary>
    /// PERF-002: Measures dynamic Expression.Compile() cost (cold runtime compilation).
    /// </summary>
    [Benchmark]
    public bool Expression_Compile_Cold()
    {
        Expression<Func<User, bool>> expr = u => u.Id == 1 && u.IsActive;
        var compiled = expr.Compile();
        return compiled(SampleUser);
    }

    /// <summary>
    /// PERF-002: Measures pre-compiled delegate invocation (warm execution).
    /// </summary>
    [Benchmark]
    public bool Expression_Compiled_Warm()
    {
        return CompiledDelegate(SampleUser);
    }

    /// <summary>
    /// PERF-002: Measures zero-reflection AST visitor node creation.
    /// </summary>
    [Benchmark]
    public ISqlNode Expression_AstVisitor_Evaluation()
    {
        return new ExpressionWhereNode(PrecompiledExpr.Body, IsOr: false);
    }
}




