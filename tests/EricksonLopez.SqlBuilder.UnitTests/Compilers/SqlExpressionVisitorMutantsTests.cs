// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using NSubstitute;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

public class SqlExpressionVisitorMutantsTests
{
    private class Dummy
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
    
    private class OtherClass
    {
        public static bool ILike(string a, string b) => true;
        public static bool Any<T>(T a, T[] b) => true;
        public static bool All<T>(T a, T[] b) => true;
        public static string Coalesce(string a, string b) => a ?? b;
        public static bool Between<T>(T a, T b, T c) => true;
        public static bool In<T>(T a, T[] b) => true;
    }

    private class PgSql
    {
        public static bool ILike(string a, string b) => true;
        public static bool Any<T>(T a, T[] b) => true;
        public static bool All<T>(T a, T[] b) => true;
    }

    private readonly IParameterManager _parameters;
    private readonly StringBuilder _sql;
    private readonly List<object> _addedParams;

    public SqlExpressionVisitorMutantsTests()
    {
        _addedParams = new List<object>();
        _parameters = Substitute.For<IParameterManager>();
        _parameters.Add(Arg.Do<object>(p => _addedParams.Add(p))).Returns(c => "@p");
        _sql = new StringBuilder();
    }

    [Fact]
    public void UnsupportedBinaryOperator_ThrowsNotSupportedException()
    {
        var param = Expression.Parameter(typeof(Dummy), "x");
        var assign = Expression.Assign(Expression.Property(param, "Id"), Expression.Constant(1));
        var lambda = Expression.Lambda(assign, param);
        
        var visitor = new SqlExpressionVisitor(_sql, _parameters);
        Action act = () => visitor.Parse(lambda);
        act.Should().Throw<NotSupportedException>();
    }
    
    [Fact]
    public void FallbackEvaluation_ForUnrecognizedMethods_Called()
    {
        Expression<Func<Dummy, bool>> exprILike = x => OtherClass.ILike("foo", "test") == true;
        var v1 = new SqlExpressionVisitor(_sql, _parameters);
        v1.Parse(exprILike);
        _sql.ToString().Should().Be("(@p = @p)");
        _sql.Clear();

        Expression<Func<Dummy, bool>> exprAny = x => OtherClass.Any(1, new[] { 1 }) == true;
        var v2 = new SqlExpressionVisitor(_sql, _parameters);
        v2.Parse(exprAny);
        _sql.ToString().Should().Be("(@p = @p)");
        _sql.Clear();

        Expression<Func<Dummy, bool>> exprAll = x => OtherClass.All(1, new[] { 1 }) == true;
        var v3 = new SqlExpressionVisitor(_sql, _parameters);
        v3.Parse(exprAll);
        _sql.ToString().Should().Be("(@p = @p)");
        _sql.Clear();

        Expression<Func<Dummy, bool>> exprCoalesce = x => OtherClass.Coalesce("foo", "test") == "test";
        var v4 = new SqlExpressionVisitor(_sql, _parameters);
        v4.Parse(exprCoalesce);
        _sql.ToString().Should().Be("(@p = @p)");
        _sql.Clear();

        Expression<Func<Dummy, bool>> exprBetween = x => OtherClass.Between(1, 1, 10) == true;
        var v5 = new SqlExpressionVisitor(_sql, _parameters);
        v5.Parse(exprBetween);
        _sql.ToString().Should().Be("(@p = @p)");
        _sql.Clear();

        Expression<Func<Dummy, bool>> exprIn = x => OtherClass.In(1, new[] { 1 }) == true;
        var v6 = new SqlExpressionVisitor(_sql, _parameters);
        v6.Parse(exprIn);
        _sql.ToString().Should().Be("(@p = @p)");
        _sql.Clear();
    }

    [Fact]
    public void DirectEvaluation_ForSqlMethods_Called()
    {
        Expression<Func<Dummy, bool>> exprILike = x => Sql.ILike("foo", "test") == true;
        var v1 = new SqlExpressionVisitor(_sql, _parameters);
        v1.Parse(exprILike);
        _sql.ToString().Should().Be("(@p ILIKE @p = @p)");
        _sql.Clear();

        Expression<Func<Dummy, bool>> exprAny = x => Sql.Any(1, new[] { 1 }) == true;
        var v2 = new SqlExpressionVisitor(_sql, _parameters);
        v2.Parse(exprAny);
        _sql.ToString().Should().Be("(@p = ANY(@p) = @p)");
        _sql.Clear();

        Expression<Func<Dummy, bool>> exprAll = x => Sql.All(1, new[] { 1 }) == true;
        var v3 = new SqlExpressionVisitor(_sql, _parameters);
        v3.Parse(exprAll);
        _sql.ToString().Should().Be("(@p = ALL(@p) = @p)");
        _sql.Clear();

        Expression<Func<Dummy, bool>> exprCoalesce = x => Sql.Coalesce("foo", "test") == "test";
        var v4 = new SqlExpressionVisitor(_sql, _parameters);
        v4.Parse(exprCoalesce);
        _sql.ToString().Should().Be("(COALESCE(@p, @p) = @p)");
        _sql.Clear();

        Expression<Func<Dummy, bool>> exprBetween = x => Sql.Between(1, 1, 10) == true;
        var v5 = new SqlExpressionVisitor(_sql, _parameters);
        v5.Parse(exprBetween);
        _sql.ToString().Should().Be("(@p BETWEEN @p AND @p = @p)");
        _sql.Clear();
    }

    [Fact]
    public void DirectEvaluation_ForPgSqlMethods_Called()
    {
        Expression<Func<Dummy, bool>> exprILike = x => PgSql.ILike("foo", "test") == true;
        var v1 = new SqlExpressionVisitor(_sql, _parameters);
        v1.Parse(exprILike);
        _sql.ToString().Should().Be("(@p ILIKE @p = @p)");
        _sql.Clear();

        Expression<Func<Dummy, bool>> exprAny = x => PgSql.Any(1, new[] { 1 }) == true;
        var v2 = new SqlExpressionVisitor(_sql, _parameters);
        v2.Parse(exprAny);
        _sql.ToString().Should().Be("(@p = ANY(@p) = @p)");
        _sql.Clear();

        Expression<Func<Dummy, bool>> exprAll = x => PgSql.All(1, new[] { 1 }) == true;
        var v3 = new SqlExpressionVisitor(_sql, _parameters);
        v3.Parse(exprAll);
        _sql.ToString().Should().Be("(@p = ALL(@p) = @p)");
        _sql.Clear();
    }

    [Fact]
    public void HandleIn_WithInvalidArguments_EvaluatesNormally()
    {
        var visitor = new SqlExpressionVisitor(_sql, _parameters);
        Expression<Func<Dummy, bool>> expr = x => string.Equals("foo", "test", StringComparison.Ordinal) == true; 
        visitor.Parse(expr);
        _sql.ToString().Should().Be("(@p = @p)");
    }

    [Fact]
    public void EscapeLikePattern_NullString_DoesNotThrow()
    {
        var visitor = new SqlExpressionVisitor(_sql, _parameters);
        Expression<Func<Dummy, bool>> expr1 = x => "foo".StartsWith((string)null) == true;
        visitor.Parse(expr1);
        _addedParams.Should().Contain("%");
        _addedParams.Clear();
        _sql.Clear();

        Expression<Func<Dummy, bool>> expr2 = x => "foo".EndsWith((string)null) == true;
        visitor.Parse(expr2);
        _addedParams.Should().Contain("%");
        _addedParams.Clear();
        _sql.Clear();

        Expression<Func<Dummy, bool>> expr3 = x => "foo".Contains((string)null) == true;
        visitor.Parse(expr3);
        _addedParams.Should().Contain("%%");
        _addedParams.Clear();
        _sql.Clear();
    }

    [Fact]
    public void HandleMethods_WithNullDeclaringType_ReturnsFalse()
    {
        var p1 = Substitute.For<System.Reflection.ParameterInfo>();
        p1.ParameterType.Returns(typeof(string));
        var p2 = Substitute.For<System.Reflection.ParameterInfo>();
        p2.ParameterType.Returns(typeof(string));

        var mockILike = Substitute.For<System.Reflection.MethodInfo>();
        mockILike.DeclaringType.Returns((Type?)null);
        mockILike.Attributes.Returns(System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.Static);
        mockILike.Name.Returns("ILike");
        mockILike.ReturnType.Returns(typeof(bool));
        mockILike.GetParameters().Returns(new[] { p1, p2 });

        var callILike = Expression.Call(null, mockILike, Expression.Constant("a"), Expression.Constant("b"));
        var visitor1 = new SqlExpressionVisitor(_sql, _parameters);
        visitor1.Parse(callILike);
        _sql.ToString().Should().Be("NULL");
        _sql.Clear();

        var mockAny = Substitute.For<System.Reflection.MethodInfo>();
        mockAny.DeclaringType.Returns((Type?)null);
        mockAny.Attributes.Returns(System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.Static);
        mockAny.Name.Returns("Any");
        mockAny.ReturnType.Returns(typeof(bool));
        mockAny.GetParameters().Returns(new[] { p1, p2 });

        var callAny = Expression.Call(null, mockAny, Expression.Constant("a"), Expression.Constant("b"));
        var visitor2 = new SqlExpressionVisitor(_sql, _parameters);
        visitor2.Parse(callAny);
        _sql.ToString().Should().Be("NULL");
        _sql.Clear();

        var mockAll = Substitute.For<System.Reflection.MethodInfo>();
        mockAll.DeclaringType.Returns((Type?)null);
        mockAll.Attributes.Returns(System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.Static);
        mockAll.Name.Returns("All");
        mockAll.ReturnType.Returns(typeof(bool));
        mockAll.GetParameters().Returns(new[] { p1, p2 });

        var callAll = Expression.Call(null, mockAll, Expression.Constant("a"), Expression.Constant("b"));
        var visitor3 = new SqlExpressionVisitor(_sql, _parameters);
        visitor3.Parse(callAll);
        _sql.ToString().Should().Be("NULL");
        _sql.Clear();
    }
}



