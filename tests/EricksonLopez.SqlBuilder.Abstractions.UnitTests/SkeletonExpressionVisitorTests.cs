// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Linq.Expressions;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using Xunit;

namespace EricksonLopez.SqlBuilder.Abstractions.UnitTests;

public class SkeletonExpressionVisitorTests
{
    [Fact]
    public void GetSkeleton_Constant_ReturnsQuestionMark()
    {
        var visitor = new SkeletonExpressionVisitor();
        Expression<Func<int>> expr = () => 5;
        
        var result = visitor.GetSkeleton(expr.Body);
        result.Should().Be("?");

        // Test Clear() by running it again on the same visitor
        var result2 = visitor.GetSkeleton(expr.Body);
        result2.Should().Be("?");
    }

    [Fact]
    public void GetSkeleton_Member_ReturnsMemberName()
    {
        var visitor = new SkeletonExpressionVisitor();
        var user = new TestUser { Age = 30 };
        Expression<Func<int>> expr = () => user.Age;
        
        var result = visitor.GetSkeleton(expr.Body);
        result.Should().Be("Ageuser?");
    }

    [Fact]
    public void GetSkeleton_ParameterMember_ReturnsOnlyMemberName()
    {
        var visitor = new SkeletonExpressionVisitor();
        Expression<Func<TestUser, int>> expr = u => u.Age;
        
        var result = visitor.GetSkeleton(expr.Body);
        result.Should().Be("Age");
    }

    [Fact]
    public void GetSkeleton_Binary_ReturnsFormattedString()
    {
        var visitor = new SkeletonExpressionVisitor();
        var expr = Expression.Equal(Expression.Constant(1), Expression.Constant(2));
        
        var result = visitor.GetSkeleton(expr);
        result.Should().Be("(? Equal ?)");
    }

    [Fact]
    public void GetSkeleton_NestedBinary_ReturnsParenthesizedStructure()
    {
        var visitor = new SkeletonExpressionVisitor();
        var left = Expression.Add(Expression.Constant(1), Expression.Constant(2));
        var expr = Expression.Multiply(left, Expression.Constant(3));
        
        var result = visitor.GetSkeleton(expr);
        result.Should().Be("((? Add ?) Multiply ?)");
    }

    [Fact]
    public void GetSkeleton_MethodCall_NoArguments_ReturnsEmptyParens()
    {
        var visitor = new SkeletonExpressionVisitor();
        Expression<Func<string>> expr = () => "hello".ToUpper();
        
        var result = visitor.GetSkeleton(expr.Body);
        result.Should().Be("ToUpper()");
    }

    [Fact]
    public void GetSkeleton_MethodCall_OneArgument_ReturnsSingleArg()
    {
        var visitor = new SkeletonExpressionVisitor();
        Expression<Func<string>> expr = () => "hello".Trim('h');
        
        var result = visitor.GetSkeleton(expr.Body);
        result.Should().Be("Trim(?)");
    }

    [Fact]
    public void GetSkeleton_MethodCall_TwoArguments_ReturnsCommaSeparated()
    {
        var visitor = new SkeletonExpressionVisitor();
        var strParam = Expression.Parameter(typeof(string), "str");
        var method = typeof(string).GetMethod("Replace", new[] { typeof(string), typeof(string) })!;
        var expr = Expression.Call(strParam, method, Expression.Constant("e"), Expression.Constant("a"));
        
        var result = visitor.GetSkeleton(expr);
        result.Should().Be("Replace(?,?)");
    }

    [Fact]
    public void GetSkeleton_MethodCall_ThreeArguments_ReturnsThreeCommaSeparatedArgs()
    {
        var visitor = new SkeletonExpressionVisitor();
        var strParam = Expression.Parameter(typeof(string), "str");
        var method = typeof(string).GetMethod("IndexOf", new[] { typeof(string), typeof(int), typeof(int) })!;
        var expr = Expression.Call(strParam, method, Expression.Constant("sub"), Expression.Constant(0), Expression.Constant(5));
        
        var result = visitor.GetSkeleton(expr);
        result.Should().Be("IndexOf(?,?,?)");
    }

    private class TestUser
    {
        public int Age { get; set; }
    }
}



