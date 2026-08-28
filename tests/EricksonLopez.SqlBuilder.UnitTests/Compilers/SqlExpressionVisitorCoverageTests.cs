// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

public static class CustomExtensions
{
    public static bool Contains(IEnumerable<int> list, int a, int b) => false;
    public static bool StartsWith(this EricksonLopez.SqlBuilder.UnitTests.Dummy_Ext dummy, string a) => false;
    public static bool EndsWith(this EricksonLopez.SqlBuilder.UnitTests.Dummy_Ext dummy, string a) => false;
}

public class Dummy_Ext
{
    public int Id { get; set; }
        public bool Contains(int a, int b) => false;
    public string Name { get; set; } = string.Empty;
}

public class SqlExpressionVisitorCoverageTests
{
    private class Dummy : EricksonLopez.SqlBuilder.Annotations.ISqlEntity
    {
        public int Id { get; set; }
        public bool Contains(int a, int b) => false;
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int? NullableId { get; set; }

        public string GetTableName() => "dummy";
        public string[] GetColumnNames() => new[] { "id", "name", "is_active", "nullable_id" };
        public object?[] GetValues() => new object?[] { Id, Name, IsActive, NullableId };
        public string[] GetAllColumnNames() => GetColumnNames();
        public object?[] GetAllValues() => GetValues();
        public IReadOnlyDictionary<string, string> GetPropertyMap() => new Dictionary<string, string>
        {
            { "Id", "id" }, { "Name", "name" }, { "IsActive", "is_active" }, { "NullableId", "nullable_id" }
        };
        public string[] GetIndexedColumns() => System.Array.Empty<string>();
    }



    [Fact]
    public void Parse_StaticMethodWithTwoArgsNotContains_EvaluatesLocally()
    {
#pragma warning disable CA1309 // Use ordinal string comparison
#pragma warning disable CA2251 // Use 'string.Equals'
        Expression<Func<Dummy, bool>> expr = d => string.Compare("a", "b") == 0;
#pragma warning restore CA2251
#pragma warning restore CA1309
        var methodCall = ((BinaryExpression)expr.Body).Left as MethodCallExpression;
        var sb = new StringBuilder();
        var parameters = new EricksonLopez.SqlBuilder.ParameterManager();
        var parser = new EricksonLopez.SqlBuilder.SqlExpressionVisitor(sb, parameters, null);
        
        parser.Parse(methodCall!);
        
        // Normally, string.Compare("a", "b") is evaluated locally and outputs a parameter with value 0.
        // If the logical mutant on Contains is active, it treats it as Enumerable.Contains, 
        // iterates the string "a" as characters, and outputs "IN (@p0)" which would fail this assertion.
        var sql = sb.ToString();
        sql.Should().NotContain("IN (");
        parameters.GetParameters().Values.Should().Contain(-1); // string.Compare("a", "b") == -1
    }
    private (string Sql, ParameterManager Pm) Parse(Expression<Func<Dummy, bool>> expr)
    {
        var sb = new StringBuilder();
        var pm = new ParameterManager();
        var parser = new SqlExpressionVisitor(sb, pm, null);
        parser.Parse(expr.Body);
        return (sb.ToString(), pm);
    }

    [Fact]
    public void Parse_UnaryConvert_ParsesCorrectly()
    {
        var (sql, pm) = Parse(x => (long)x.Id == 1L);
        sql.Should().Be("(id = @p0)");
    }

    [Fact]
    public void Parse_UnaryNot_ParsesCorrectly()
    {
        var (sql, pm) = Parse(x => !x.IsActive);
        sql.Should().Be("NOT (is_active)");
    }

    [Fact]
    public void Parse_UnaryNegate_ParsesCorrectly()
    {
        var (sql, pm) = Parse(x => -x.Id == -1);
        sql.Should().Be("(-(id) = @p0)");
    }

    [Fact]
    public void Parse_StaticProperty_EvaluatesAsValue()
    {
        var (sql, pm) = Parse(x => x.Name == string.Empty);
        sql.Should().Be("(name = @p0)");
        pm.GetParameters()["p0"].Should().Be(string.Empty);
    }

    [Fact]
    public void Parse_Any_ParsesCorrectly()
    {
        var (sql, pm) = Parse(x => Sql.Any(x.Id, new[] { 1, 2 }));
        sql.Should().Be("id = ANY(@p0)");

        
    }

    [Fact]
    public void Parse_All_ParsesCorrectly()
    {
        var (sql, pm) = Parse(x => Sql.All(x.Id, new[] { 1, 2 }));
        sql.Should().Be("id = ALL(@p0)");

        
    }

    [Fact]
    public void Parse_ILike_ParsesCorrectly()
    {
        var (sql, pm) = Parse(x => Sql.ILike(x.Name, "test%"));
        sql.Should().Be("name ILIKE @p0");
        pm.GetParameters()["p0"].Should().Be("test%");

    }

    [Fact]
    public void Parse_Contains_ParsesCorrectly()
    {
        var (sql, pm) = Parse(x => x.Name.Contains("Erick"));
        sql.Should().Be("name LIKE @p0 ESCAPE '\\'");
        pm.GetParameters()["p0"].Should().Be("%Erick%");
    }

    [Fact]
    public void Parse_StartsWith_ParsesCorrectly()
    {
        var (sql, pm) = Parse(x => x.Name.StartsWith("Erick"));
        sql.Should().Be("name LIKE @p0 ESCAPE '\\'");
        pm.GetParameters()["p0"].Should().Be("Erick%");
    }

    [Fact]
    public void Parse_EndsWith_ParsesCorrectly()
    {
        var (sql, pm) = Parse(x => x.Name.EndsWith("Erick"));
        sql.Should().Be("name LIKE @p0 ESCAPE '\\'");
        pm.GetParameters()["p0"].Should().Be("%Erick");
    }

    private string? GetNullString() => null; [Fact] public void Parse_MemberExpressionNull_GeneratesNULL() { var sb = new StringBuilder(); var pm = new ParameterManager(); var parser = new SqlExpressionVisitor(sb, pm, null); string? localNull = null; Expression<Func<string?>> expr = () => localNull; parser.Parse(expr.Body); sb.ToString().Should().Be("NULL"); } [Fact] public void Parse_MethodCallExpressionNull_GeneratesNULL() { var sb = new StringBuilder(); var pm = new ParameterManager(); var parser = new SqlExpressionVisitor(sb, pm, null); Expression<Func<string?>> expr = () => GetNullString(); parser.Parse(expr.Body); sb.ToString().Should().Be("NULL"); } [Fact] public void Evaluate_ConvertChecked_IsEvaluated() { var sb = new StringBuilder(); var pm = new ParameterManager(); var parser = new SqlExpressionVisitor(sb, pm, null); var convertExpr = Expression.ConvertChecked(Expression.Constant("test"), typeof(string)); var param = Expression.Parameter(typeof(Dummy), "x"); var method = typeof(Sql).GetMethod("ILike")!; var body = Expression.Call(method, Expression.Property(param, "Name"), convertExpr); var lambda = Expression.Lambda<Func<Dummy, bool>>(body, param); parser.Parse(lambda.Body); sb.ToString().Should().Be("name ILIKE @p0"); pm.GetParameters()["p0"].Should().Be("test"); } [Fact] public void Parse_IsNotNulL_UsesIsNotNull() { int? val = null; var (sql, pm) = Parse(x => x.NullableId != val); sql.Should().Be("(nullable_id IS NOT NULL)"); } [Fact] public void Parse_IsNullConstantOrExpression_RightIsNull_UsesIsNull()
    {
        int? val = null;
        var (sql, pm) = Parse(x => x.NullableId == val);
        sql.Should().Be("(nullable_id IS NULL)");
    }

    [Fact]
    public void Parse_IsNullConstantOrExpression_LeftIsNull_UsesIsNull()
    {
        int? val = null;
        var (sql, pm) = Parse(x => val == x.NullableId);
        sql.Should().Be("(nullable_id IS NULL)");
    }

    [Fact]
    public void Parse_UnaryConvert_Evaluated()
    {
        int val = 1;
        var (sql, pm) = Parse(x => x.Id == (long)val);
        sql.Should().Be("(id = @p0)");
    }

    [Fact]
    public void Parse_ConvertChecked_IsNull_UsesIsNull()
    {
        int? val = null;
        var (sql, pm) = Parse(x => x.NullableId == checked((long?)val));
        sql.Should().Be("(nullable_id IS NULL)");
    }

    [Fact]
    public void Parse_UnsupportedExpressionInEvaluate_Throws()
    {
        // To hit Evaluate's NotSupportedException, we need to bypass IsStaticEvaluation or use a MethodCall that evaluates its arguments.
        // E.g. Sql.ILike(x.Name, not_supported_expr)
        Func<int> f = () => 1;
        Expression<Func<Dummy, bool>> expr = x => Sql.ILike(x.Name, f().ToString(System.Globalization.CultureInfo.InvariantCulture));
        Action act = () => Parse(expr);
        act.Should().Throw<NotSupportedException>().WithMessage("*cannot be evaluated in NativeAOT context*Supported evaluation patterns:*MethodCallExpression, UnaryExpression (Convert/ConvertChecked), and NewArrayExpression.*Avoid using closures over complex types*");
    }

    [Fact]
    public void Parse_UnsupportedMethodOnString_Throws()
    {
        // Kills the mutant where `EndsWith` condition is mutated to `|| methodCall.Object != null`
        // x.Name.ToLower() has Object != null and type string.
        Action act = () => Parse(x => x.Name.ToLower() == "a");
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Parse_UnsupportedUnaryExpression_Throws()
    {
        // TypeAs is a UnaryExpression but not Convert/Not/Negate.
        Action act = () => Parse(x => (x.Name as string) == null);
        act.Should().Throw<NotSupportedException>().WithMessage("Expression of type TypeAs is not supported.");
    }

    [Fact]
    public void Parse_UnsupportedMethodOnIEnumerable_Throws()
    {
        // Kills the mutant where `Contains` condition is mutated to `|| typeof(IEnumerable).IsAssignableFrom...`
        // List<int>.Remove(...) has declaring type List<int> which is IEnumerable, but method is not Contains.
        Action act = () => Parse(x => new List<int>().Remove(x.Id));
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Parse_UnaryNegateChecked_ParsesCorrectly()
    {
        var (sql, pm) = Parse(x => checked(-x.Id) == -1);
        sql.Should().Be("(-(id) = @p0)");
    }

    [Fact]
    public void Parse_ConstantIsNull_OutputsNull()
    {
        var sb = new StringBuilder();
        var pm = new ParameterManager();
        var parser = new SqlExpressionVisitor(sb, pm, null);
        parser.Parse(Expression.Constant(null));
        sb.ToString().Should().Be("NULL");
    }

    [Fact]
    public void Parse_BitwiseOperators_ParsesCorrectly()
    {
        var (sql1, _) = Parse(x => (x.Id & 1) == 1);
        sql1.Should().Be("((id & @p0) = @p1)");
        
        var (sql2, _) = Parse(x => (x.Id | 1) == 1);
        sql2.Should().Be("((id | @p0) = @p1)");
        
        var (sql3, _) = Parse(x => (x.Id ^ 1) == 1);
        sql3.Should().Be("((id ^ @p0) = @p1)");
    }

    [Fact]
    public void Evaluate_Convert_IsEvaluated()
    {
        var sb = new StringBuilder();
        var pm = new ParameterManager();
        var parser = new SqlExpressionVisitor(sb, pm, null);
        
        var convertExpr = Expression.Convert(Expression.Constant("test"), typeof(string));
        var param = Expression.Parameter(typeof(Dummy), "x");
        var method = typeof(Sql).GetMethod("ILike")!;
        var body = Expression.Call(method, Expression.Property(param, "Name"), convertExpr);
        var lambda = Expression.Lambda<Func<Dummy, bool>>(body, param);
        
        parser.Parse(lambda.Body);
        
        sb.ToString().Should().Be("name ILIKE @p0");
        pm.GetParameters()["p0"].Should().Be("test");
    }

    [Fact]
    public void Parse_NegateChecked_ParsesCorrectly()
    {
        var sb = new StringBuilder();
        var pm = new ParameterManager();
        var parser = new SqlExpressionVisitor(sb, pm, null);
        
        var param = Expression.Parameter(typeof(Dummy), "x");
        var negateChecked = Expression.NegateChecked(Expression.Property(param, "Id"));
        var body = Expression.Equal(negateChecked, Expression.Constant(-1));
        var lambda = Expression.Lambda<Func<Dummy, bool>>(body, param);
        
        parser.Parse(lambda.Body);
        
        sb.ToString().Should().Be("(-(id) = @p0)");
    }

    [Fact]
    public void Evaluate_NewArrayExpression_IsEvaluated()
    {
        var sb = new StringBuilder();
        var pm = new ParameterManager();
        var parser = new SqlExpressionVisitor(sb, pm, null);

        var arrayExpr = Expression.NewArrayInit(typeof(int), Expression.Constant(1), Expression.Constant(2));
        var param = Expression.Parameter(typeof(Dummy), "x");
        
        var method = typeof(Sql).GetMethod("Any")!.MakeGenericMethod(typeof(int));
        var body = Expression.Call(method, Expression.Property(param, "Id"), arrayExpr);
        var lambda = Expression.Lambda<Func<Dummy, bool>>(body, param);

        parser.Parse(lambda.Body);

        sb.ToString().Should().Be("id = ANY(@p0)");
        pm.GetParameters()["p0"].Should().BeEquivalentTo(new[] { 1, 2 });
    }

    [Fact]
    public void Evaluate_MemberExpressionOnNull_Throws()
    {
        Helper h = null!;
        // Assuming GetValue on null h throws Exception
        var expr = (Expression<Func<Dummy, bool>>)(x => h.Value == "a");
        var act = () => Parse(expr);
        act.Should().Throw<Exception>();
    }

    private class Helper { public string GetStr() => null!; public string Value => null!; public string Prop => null!; }

    [Fact]
    public void Evaluate_MethodCallExpressionOnNull_Throws()
    {
        Helper h = null!;
        // It evaluates h.GetStr() which throws Exception during reflection.
        var expr = (Expression<Func<Dummy, bool>>)(x => h.GetStr() == "a");
        var act = () => Parse(expr);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Evaluate_PropertyOnNull_Throws()
    {
        Helper h = null!;
        var expr = (Expression<Func<Dummy, bool>>)(x => h.Prop == "a");
        var act = () => Parse(expr);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Parse_UnsupportedMethodOnSql_Evaluates()
    {
        // Kills the mutant where `All` condition is mutated to `|| typeof(Sql)`
        // Sql.From<Dummy>() is in typeof(Sql), but is not "All".
        // It has 0 arguments. If mutant enters `All`, it throws ArgumentOutOfRangeException.
        // Original code falls through and evaluates it as a Constant/MethodCall.
        Action act = () => Parse(x => Sql.From<Dummy>() == null);
        act.Should().NotThrow();
    }

    [Fact]
    public void Parse_UnsupportedContains_ThrowsNotSupportedException()
    {
        Action act = () => ParseExt(x => CustomExtensions.Contains(new[] { 1 }, x.Id, 2));
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Parse_UnsupportedStartsWith_ThrowsNotSupportedException()
    {
        Action act = () => ParseExt(x => x.StartsWith("test"));
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Parse_UnsupportedEndsWith_ThrowsNotSupportedException()
    {
        Action act = () => ParseExt(x => x.EndsWith("test"));
        act.Should().Throw<NotSupportedException>();
    }

    private (string Sql, ParameterManager Pm) ParseExt(Expression<Func<Dummy_Ext, bool>> expr)
    {
        var sb = new StringBuilder();
        var pm = new ParameterManager();
        var parser = new SqlExpressionVisitor(sb, pm, null);
        parser.Parse(expr.Body);
        return (sb.ToString(), pm);
    }

    [Fact]
    public void Parse_ContainsEscapesSpecialCharacters_ParsesCorrectly()
    {
        var (sql, pm) = Parse(x => x.Name.Contains(@"a\b%c_d"));
        sql.Should().Be("name LIKE @p0 ESCAPE '\\'");
        pm.GetParameters()["p0"].Should().Be(@"%a\\b\%c\_d%");
    }

    [Fact]
    public void Parse_ConvertNull_GeneratesNull()
    {
        var sb = new StringBuilder();
        var pm = new ParameterManager();
        var parser = new SqlExpressionVisitor(sb, pm, null);
        
        var convertExpr = Expression.Convert(Expression.Constant(null, typeof(int?)), typeof(int?));
        var param = Expression.Parameter(typeof(Dummy), "x");
        var body = Expression.Equal(Expression.Property(param, "NullableId"), convertExpr);
        var lambda = Expression.Lambda<Func<Dummy, bool>>(body, param);
        
        parser.Parse(lambda.Body);
        sb.ToString().Should().Be("(nullable_id IS NULL)");
    }

    [Fact]
    public void Parse_InstanceContainsTwoArgs_EvaluatesLocally()
    {
        var dummy = new Dummy();
        var (sql, pm) = Parse(x => dummy.Contains(1, 2));
        sql.Should().Be("@p0");
    }

    [Fact]
    public void Parse_SqlOuter_With_UnaryExpression_ParsesCorrectly()
    {
        var (sql, pm) = Parse(x => x.Id == Sql.Outer<Dummy, int>(y => (int)y.NullableId!));
        sql.Should().Be("(id = \"nullable_id\")");
    }

    [Fact]
    public void Parse_SqlOuter_With_NonMemberExpression_ThrowsNotSupportedException()
    {
        Action act = () => Parse(x => x.Id == Sql.Outer<Dummy, int>(y => 42));
        act.Should().Throw<NotSupportedException>()
            .WithMessage("*Sql.Outer requires a member expression selector.*");
    }
}



