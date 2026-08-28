// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests.Nodes;

public class NodeAcceptTests
{
    private class DummyVisitor : SqlVisitorBase
    {
        public bool Visited { get; private set; }
        
        public override void Visit(CteNode node) => Visited = true;
        public override void Visit(DeleteNode node) => Visited = true;
        public override void Visit(FromNode node) => Visited = true;
        public override void Visit(SubqueryFromNode node) => Visited = true;
        public override void Visit(UnnestNode node) => Visited = true;
        public override void Visit(GroupByNode node) => Visited = true;
        public override void Visit(ExpressionHavingNode node) => Visited = true;
        public override void Visit(RawHavingNode node) => Visited = true;
        public override void Visit(InsertNode node) => Visited = true;
        public override void Visit(ValuesNode node) => Visited = true;
        public override void Visit(ReturningNode node) => Visited = true;
        public override void Visit(OnConflictNode node) => Visited = true;
        public override void Visit(DefaultValuesNode node) => Visited = true;
        public override void Visit(JoinNode node) => Visited = true;
        public override void Visit(RawJoinNode node) => Visited = true;
        public override void Visit(SubqueryJoinNode node) => Visited = true;
        public override void Visit(LimitOffsetNode node) => Visited = true;
        public override void Visit(ScalarSubquerySelectNode node) => Visited = true;
        public override void Visit(OrderByNode node) => Visited = true;
        public override void Visit(ThenByNode node) => Visited = true;
        public override void Visit(RawOrderByNode node) => Visited = true;
        public override void Visit(SelectNode node) => Visited = true;
        public override void Visit(ExpressionSelectNode node) => Visited = true;
        public override void Visit(QueryAliasNode node) => Visited = true;
        public override void Visit(DistinctOnNode node) => Visited = true;
        public override void Visit(RawSelectNode node) => Visited = true;
        public override void Visit(SetOperationNode node) => Visited = true;
        public override void Visit(UpdateNode node) => Visited = true;
        public override void Visit(SetNode node) => Visited = true;
        public override void Visit(ExpressionWhereNode node) => Visited = true;
        public override void Visit(RawWhereNode node) => Visited = true;
        public override void Visit(ExistsWhereNode node) => Visited = true;
        public override void Visit(ConcurrencyTokenNode node) => Visited = true;
        public override void Visit(WindowNode node) => Visited = true;
        public override void Visit(WindowPageNode node) => Visited = true;
        public override void Visit(WindowFunctionNode node) => Visited = true;
        // v0.8.0+ nodes
        public override void Visit(CaseNode node) => Visited = true;
        public override void Visit(InsertSelectNode node) => Visited = true;
        public override void Visit(CompositeCursorNode node) => Visited = true;
    }

    [Fact]
    public void AllNodes_Accept_CallsVisitor()
    {
        var nodeTypes = typeof(SelectNode).Assembly.GetTypes()
            .Where(t => typeof(ISqlNode).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .ToList();

        foreach (var type in nodeTypes)
        {
            var constructors = type.GetConstructors();
            if (constructors.Length == 0)
            {
                continue;
            }

            var constructor = constructors.OrderBy(c => c.GetParameters().Length).First();
            var parameters = constructor.GetParameters().Select(p => GetDefaultValue(p.ParameterType)).ToArray();
            
            var instance = (ISqlNode)constructor.Invoke(parameters);
            var visitor = new DummyVisitor();
            
            instance.Accept(visitor);
            
            visitor.Visited.Should().BeTrue($"Node {type.Name} did not call Visit");
        }
    }

    private object? GetDefaultValue(Type type)
    {
        if (type == typeof(string))
        {
            return "test";
        }

        if (type == typeof(object[]))
        {
            return new object?[] { 1 };
        }

        if (type == typeof(string[]))
        {
            return new string[] { "test" };
        }

        if (type == typeof(IReadOnlyList<string>))
        {
            return new string[] { "test" };
        }

        if (type == typeof(IReadOnlyList<IReadOnlyList<object?>>))
        {
            return new IReadOnlyList<object?>[] { new object?[] { 1 } };
        }

        if (type == typeof(Expression))
        {
            return Expression.Constant(1);
        }

        if (type == typeof(LambdaExpression))
        {
            return Expression.Lambda(Expression.Constant(1));
        }

        if (type == typeof(ISqlQuery))
        {
            return new RawQuery("SELECT 1");
        }

        if (type.IsValueType)
        {
            return Activator.CreateInstance(type);
        }

        return null;
    }
}




