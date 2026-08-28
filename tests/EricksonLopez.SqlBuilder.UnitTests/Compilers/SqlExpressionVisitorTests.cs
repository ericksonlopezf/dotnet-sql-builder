// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Testing;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

public class SqlExpressionVisitorTests
{

    private (string Sql, ParameterManager Pm) Parse(Expression<Func<DummyEntity, bool>> expr)
    {
        var sb = new StringBuilder();
        var pm = new ParameterManager();
        var parser = new SqlExpressionVisitor(sb, pm, null);
        parser.Parse(expr.Body);
        return (sb.ToString(), pm);
    }

    [Fact]
    public void BinaryExpression_Equal_ParsesCorrectly()
    {
        var (sql, pm) = Parse(x => x.Id == 1);
        sql.Should().Be("(id = @p0)");
        pm.GetParameters()["p0"].Should().Be(1);
    }

    [Fact]
    public void BinaryExpression_NotEqual_ParsesCorrectly()
    {
        var (sql, pm) = Parse(x => x.Id != 1);
        sql.Should().Be("(id != @p0)");
        pm.GetParameters()["p0"].Should().Be(1);
    }

    [Fact]
    public void BinaryExpression_GreaterThan_ParsesCorrectly()
    {
        var (sql, pm) = Parse(x => x.Id > 1);
        sql.Should().Be("(id > @p0)");
        pm.GetParameters()["p0"].Should().Be(1);
    }

    [Fact]
    public void BinaryExpression_AndAlso_ParsesCorrectly()
    {
        var (sql, pm) = Parse(x => x.Id == 1 && x.Name == "Erick");
        sql.Should().Be("((id = @p0) AND (name = @p1))");
        pm.GetParameters()["p0"].Should().Be(1);
        pm.GetParameters()["p1"].Should().Be("Erick");
    }

    [Fact]
    public void MethodCall_Contains_ParsesToLike()
    {
        var (sql, pm) = Parse(x => x.Name.Contains("Erick"));
        sql.Should().Be("name LIKE @p0 ESCAPE '\\'");
        pm.GetParameters()["p0"].Should().Be("%Erick%");
    }

    [Fact]
    public void MethodCall_StartsWith_ParsesToLike()
    {
        var (sql, pm) = Parse(x => x.Name.StartsWith("Erick"));
        sql.Should().Be("name LIKE @p0 ESCAPE '\\'");
        pm.GetParameters()["p0"].Should().Be("Erick%");
    }

    [Fact]
    public void MethodCall_EndsWith_ParsesToLike()
    {
        var (sql, pm) = Parse(x => x.Name.EndsWith("Erick"));
        sql.Should().Be("name LIKE @p0 ESCAPE '\\'");
        pm.GetParameters()["p0"].Should().Be("%Erick");
    }

    [Fact]
    public void MethodCall_EnumerableContains_ParsesToIn()
    {
        var list = new[] { 1, 2, 3 };
        var (sql, pm) = Parse(x => Enumerable.Contains(list, x.Id));
        sql.Should().Be("id IN (@p0, @p1, @p2)");
        pm.GetParameters()["p0"].Should().Be(1);
        pm.GetParameters()["p1"].Should().Be(2);
        pm.GetParameters()["p2"].Should().Be(3);
    }

    [Fact]
    public void MethodCall_EnumerableContains_Empty_ParsesToSelect1Where1Eq0()
    {
        var list = System.Array.Empty<int>();
        var (sql, _) = Parse(x => Enumerable.Contains(list, x.Id));
        sql.Should().Be("1=0");
    }

    [Fact]
    public void MethodCall_ListContains_ParsesToIn()
    {
        var list = new List<int> { 1, 2, 3 };
        var (sql, pm) = Parse(x => list.Contains(x.Id));
        sql.Should().Be("id IN (@p0, @p1, @p2)");
        pm.GetParameters()["p0"].Should().Be(1);
    }
    
    [Fact]
    public void MethodCall_ListContains_Empty_ParsesToSelect1Where1Eq0()
    {
        var list = new List<int>();
        var (sql, _) = Parse(x => list.Contains(x.Id));
        sql.Should().Be("1=0");
    }

    [Fact]
    public void Evaluate_Field_ParsesCorrectly()
    {
        var filter = new DummyFilter { Value = 42 };
        var (sql, pm) = Parse(x => x.Id == filter.Value);
        sql.Should().Be("(id = @p0)");
        pm.GetParameters()["p0"].Should().Be(42);
    }

    [Fact]
    public void Evaluate_Property_ParsesCorrectly()
    {
        var filter = new DummyFilter { Value = 42 };
        var (sql, pm) = Parse(x => x.Id == filter.PropValue);
        sql.Should().Be("(id = @p0)");
        pm.GetParameters()["p0"].Should().Be(42);
    }

    [Fact]
    public void Evaluate_MethodCall_ParsesCorrectly()
    {
        var filter = new DummyFilter { Value = 42 };
        var (sql, pm) = Parse(x => x.Id == filter.GetValue());
        sql.Should().Be("(id = @p0)");
        pm.GetParameters()["p0"].Should().Be(42);
    }

    [Fact]
    public void Evaluate_Null_ParsesToNull()
    {
        string? val = null;
        var (sql, _) = Parse(x => x.Name == val);
        sql.Should().Be("(name IS NULL)");
    }

    [Fact]
    public void Constant_Null_ParsesToNull()
    {
        var (sql, _) = Parse(x => x.Name == null);
        sql.Should().Be("(name IS NULL)");
    }

    [Fact]
    public void UnsupportedNodeType_ThrowsException()
    {
        System.Action act = () => Parse(x => x.Id == (x.IsActive ? 1 : 0));
        act.Should().Throw<System.NotSupportedException>();
    }

    [Fact]
    public void ClosureArrayEvaluate_EvaluatesSuccessfully()
    {
        var sb = new System.Text.StringBuilder();
        var pm = new ParameterManager();
        var parser = new SqlExpressionVisitor(sb, pm, null);
        System.Linq.Expressions.Expression<System.Func<DummyEntity, bool>> expr = x => Enumerable.Contains(new[] { 1, 2 }, x.Id);
        parser.Parse(expr);
        sb.ToString().Should().Be("id IN (@p0, @p1)");
    }


    private class DummyFilter
    {
        public int Value;
        public int PropValue => Value;
        public int GetValue() => Value;
        public int GetValueWithArg(int x) => Value + x;
    }

    [Fact]
    public void Parse_EvaluateMethodCallWithArgs_EvaluatesSuccessfully()
    {
        var sb = new System.Text.StringBuilder();
        var pm = new ParameterManager();
        var parser = new SqlExpressionVisitor(sb, pm, null);
        var filter = new DummyFilter { Value = 5 };
        
        System.Linq.Expressions.Expression<System.Func<DummyEntity, bool>> expr = x => x.Id == filter.GetValueWithArg(10);
        
        parser.Parse(expr);
        sb.ToString().Should().Be("(id = @p0)");
        pm.GetParameters().Values.Should().Contain(15);
    }

    [Fact]
    public void Parse_ContainsWithNullEnumerable_GeneratesFalseCondition()
    {
        var sb = new System.Text.StringBuilder();
        var pm = new ParameterManager();
        var parser = new SqlExpressionVisitor(sb, pm, null);
        List<int> list = null!;
        System.Linq.Expressions.Expression<System.Func<DummyEntity, bool>> expr = x => list.Contains(x.Id);
        
        parser.Parse(expr);
        sb.ToString().Should().Be("1=0");
    }

    [Fact]
    public void Parse_ContainsWithNullArray_GeneratesFalseCondition()
    {
        var sb = new System.Text.StringBuilder();
        var pm = new ParameterManager();
        var parser = new SqlExpressionVisitor(sb, pm, null);
        int[] arr = null!;
        System.Linq.Expressions.Expression<System.Func<DummyEntity, bool>> expr = x => arr.Contains(x.Id);
        
        parser.Parse(expr);
        sb.ToString().Should().Be("1=0");
    }

    [Fact]
    public void Parse_EvaluateComplexExpression_EvaluatesSuccessfully()
    {
        var sb = new System.Text.StringBuilder();
        var pm = new ParameterManager();
        var parser = new SqlExpressionVisitor(sb, pm, null);
        
        int v = 2;
        System.Linq.Expressions.Expression<System.Func<DummyEntity, bool>> expr = x => new[] { v }.Contains(x.Id); 
        
        parser.Parse(expr);
        sb.ToString().Should().Be("id IN (@p0)");
    }


    [Fact]
    public void Parse_UnsupportedOperator_ThrowsNotSupportedException()
    {
        var sb = new System.Text.StringBuilder();
        var pm = new ParameterManager();
        var parser = new SqlExpressionVisitor(sb, pm, null);
        
        System.Linq.Expressions.Expression<System.Func<DummyEntity, bool>> expr = x => x.Id == 1; // Unused, we replace the body
        var powerExpr = System.Linq.Expressions.Expression.MakeBinary(System.Linq.Expressions.ExpressionType.Power, System.Linq.Expressions.Expression.Constant(2.0), System.Linq.Expressions.Expression.Constant(3.0), false, typeof(System.Math).GetMethod("Pow"));
        expr = System.Linq.Expressions.Expression.Lambda<System.Func<DummyEntity, bool>>(System.Linq.Expressions.Expression.Equal(powerExpr, System.Linq.Expressions.Expression.Constant(1.0)), expr.Parameters); // '+' is ExpressionType.Add, which is unsupported.
        
        var act = () => parser.Parse(expr);
        act.Should().Throw<System.NotSupportedException>().WithMessage("Operator Power is not supported.");
    }
    
    [Fact]
    public void NativeAotSafe_ThrowsExpectedMessage_ForUnsupportedNodeType()
    {
        int a = 1;
        var act = () => Parse(x => x.Name == "123".Substring(a + 1));
        act.Should().Throw<System.NotSupportedException>()
            .WithMessage("*MethodCallExpression, UnaryExpression (Convert/ConvertChecked)*");
    }

    [Fact]
    public void ConvertChecked_ParsesSuccessfully()
    {
        var (sql, _) = Parse(x => x.Id == checked((int)2.5));
        sql.Should().Be("(id = @p0)");
    }

    [Fact]
    public void Parse_UnsupportedNodeType_ThrowsNotSupportedException()
    {
        var sb = new System.Text.StringBuilder();
        var pm = new ParameterManager();
        var parser = new SqlExpressionVisitor(sb, pm, null);
        var block = System.Linq.Expressions.Expression.Block(System.Linq.Expressions.Expression.Constant(1));
        
        System.Action act = () => parser.Parse(block);
        act.Should().Throw<System.NotSupportedException>();
    }

    [Fact]
    public void Parse_WithEscapeFunc_EscapesColumnNames()
    {
        var sb = new System.Text.StringBuilder();
        var pm = new ParameterManager();
        var parser = new SqlExpressionVisitor(sb, pm, c => $"\"{c}\"");
        System.Linq.Expressions.Expression<System.Func<DummyEntity, bool>> expr = x => x.Id == 1;
        
        parser.Parse(expr);
        sb.ToString().Should().Be("(\"id\" = @p0)");
    }

    [Fact]
    public void Parse_ComparisonAndLogicalOperators_GeneratesCorrectSql()
    {
        var sb = new System.Text.StringBuilder();
        var pm = new ParameterManager();
        var parser = new SqlExpressionVisitor(sb, pm, null);
        System.Linq.Expressions.Expression<System.Func<DummyEntity, bool>> expr = x => x.Id >= 1 || x.Id < 5 || x.Id <= 10;
        
        parser.Parse(expr);
        sb.ToString().Should().Be("(((id >= @p0) OR (id < @p1)) OR (id <= @p2))");
    }

    [Fact]
    public void Parse_NullExpression_DoesNothing()
    {
        var sb = new System.Text.StringBuilder();
        var pm = new ParameterManager();
        var parser = new SqlExpressionVisitor(sb, pm, null);
        
        parser.Parse(null!); // should hit node == null early exit
        sb.ToString().Should().Be(string.Empty);
    }

    [Theory]
    [InlineData("x => x.Id + 1 == 2", "((id + @p0) = @p1)")]
    [InlineData("x => checked(x.Id + 1) == 2", "((id + @p0) = @p1)")]
    [InlineData("x => x.Id - 1 == 2", "((id - @p0) = @p1)")]
    [InlineData("x => checked(x.Id - 1) == 2", "((id - @p0) = @p1)")]
    [InlineData("x => x.Id * 2 == 4", "((id * @p0) = @p1)")]
    [InlineData("x => checked(x.Id * 2) == 4", "((id * @p0) = @p1)")]
    [InlineData("x => x.Id / 2 == 1", "((id / @p0) = @p1)")]
    [InlineData("x => x.Id % 2 == 0", "((id % @p0) = @p1)")]
    [InlineData("x => (x.Id << 1) == 2", "((id << @p0) = @p1)")]
    [InlineData("x => (x.Id >> 1) == 0", "((id >> @p0) = @p1)")]
    public void Parse_ArithmeticAndBitwiseOperators_GeneratesCorrectSql(string expressionType, string expectedSql)
    {
        // We compile the expressions explicitly to avoid complex string parsing, 
        // using inline data as descriptions
        var sb = new System.Text.StringBuilder();
        var pm = new ParameterManager();
        var parser = new SqlExpressionVisitor(sb, pm, null);
        
        Expression<Func<DummyEntity, bool>> expr = expressionType switch
        {
            "x => x.Id + 1 == 2" => x => x.Id + 1 == 2,
            "x => checked(x.Id + 1) == 2" => x => checked(x.Id + 1) == 2,
            "x => x.Id - 1 == 2" => x => x.Id - 1 == 2,
            "x => checked(x.Id - 1) == 2" => x => checked(x.Id - 1) == 2,
            "x => x.Id * 2 == 4" => x => x.Id * 2 == 4,
            "x => checked(x.Id * 2) == 4" => x => checked(x.Id * 2) == 4,
            "x => x.Id / 2 == 1" => x => x.Id / 2 == 1,
            "x => x.Id % 2 == 0" => x => x.Id % 2 == 0,
            "x => (x.Id << 1) == 2" => x => (x.Id << 1) == 2,
            "x => (x.Id >> 1) == 0" => x => (x.Id >> 1) == 0,
            _ => throw new Exception("Unknown expression")
        };
        
        parser.Parse(expr.Body);
        sb.ToString().Should().Be(expectedSql);
    }
}




