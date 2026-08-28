// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Oracle;
using Xunit;

namespace EricksonLopez.SqlBuilder.Oracle.UnitTests;

public class OracleVisitorTests
{
    [Fact]
    public void Visit_ReturningNode_EmptyColumns_ThrowsNotSupportedException()
    {
        var compiler = new OracleCompiler();
        var context = new CompilationContext(new ParameterManager());
        var visitor = (OracleVisitor)compiler.CreateVisitor(context);

        var node = new ReturningNode(Array.Empty<string>());
        
        var act = () => visitor.Visit(node);
        
        act.Should().Throw<NotSupportedException>()
            .WithMessage("Oracle RETURNING clause requires explicit column names. Use .Returning(\"col1\", \"col2\") instead of .Returning().");
    }

    [Fact]
    public void Visit_ReturningNode_WithColumns_AppendsCorrectSql()
    {
        var compiler = new OracleCompiler();
        var context = new CompilationContext(new ParameterManager());
        var visitor = (OracleVisitor)compiler.CreateVisitor(context);

        var node = new ReturningNode(new[] { "Id", "Name" });
        
        visitor.Visit(node);
        
        context.Sql.ToString().Should().Be("RETURNING \"ID\", \"NAME\" INTO :out_id, :out_name ");
    }

    [Fact]
    public void Visit_OnConflictNode_ThrowsNotSupportedException()
    {
        var compiler = new OracleCompiler();
        var context = new CompilationContext(new ParameterManager());
        var visitor = (OracleVisitor)compiler.CreateVisitor(context);

        var node = new OnConflictNode(Array.Empty<string>(), null, null, null);
        
        var act = () => visitor.Visit(node);
        
        act.Should().Throw<NotSupportedException>()
            .WithMessage("Oracle does not support ON CONFLICT syntax. Use Sql.Raw() with a MERGE INTO statement instead.");
    }

    [Fact]
    public void Visit_WindowFunctionNode_WithFilter_ThrowsNotSupportedException()
    {
        var compiler = new OracleCompiler();
        var context = new CompilationContext(new ParameterManager());
        var visitor = (OracleVisitor)compiler.CreateVisitor(context);

        var node = new WindowFunctionNode(
            "SUM", "Amount", null, null,
            Array.Empty<string>(), Array.Empty<string>(), Array.Empty<bool>(),
            "sum_val", FilterRaw: "Status = 'Active'");

        var act = () => visitor.Visit(node);

        act.Should().Throw<NotSupportedException>()
            .WithMessage("Oracle does not support the FILTER (WHERE ...) clause on window functions. Use conditional aggregation with CASE expressions or Sql.Raw() instead.");
    }
}


