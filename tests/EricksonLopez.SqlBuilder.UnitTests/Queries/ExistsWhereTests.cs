// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests.Queries;

public class ExistsWhereTests
{
    private class Order : EricksonLopez.SqlBuilder.Annotations.ISqlEntity
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public decimal TotalAmount { get; set; }

        public string GetTableName() => "orders";
        public string[] GetColumnNames() => new[] { "id", "customer_id", "total_amount" };
        public object?[] GetValues() => new object?[] { Id, CustomerId, TotalAmount };
        public string[] GetAllColumnNames() => GetColumnNames();
        public object?[] GetAllValues() => GetValues();
        public IReadOnlyDictionary<string, string> GetPropertyMap() => new Dictionary<string, string> { { "Id", "id" }, { "CustomerId", "customer_id" }, { "TotalAmount", "total_amount" } };
        public string[] GetIndexedColumns() => System.Array.Empty<string>();
    }

    private class Customer : EricksonLopez.SqlBuilder.Annotations.ISqlEntity
    {
        public int Id { get; set; }
        public bool IsActive { get; set; }

        public string GetTableName() => "customers";
        public string[] GetColumnNames() => new[] { "id", "is_active" };
        public object?[] GetValues() => new object?[] { Id, IsActive };
        public string[] GetAllColumnNames() => GetColumnNames();
        public object?[] GetAllValues() => GetValues();
        public IReadOnlyDictionary<string, string> GetPropertyMap() => new Dictionary<string, string> { { "Id", "id" }, { "IsActive", "is_active" } };
        public string[] GetIndexedColumns() => System.Array.Empty<string>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // AST structure tests
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void WhereExists_AddsExistsWhereNode_WithIsNotFalse_AndIsOrFalse()
    {
        var subquery = Sql.From<Customer>().Where(c => c.IsActive == true);
        var query = Sql.From<Order>().WhereExists(subquery);

        var node = query.Nodes.OfType<ExistsWhereNode>().Single();
        node.IsNot.Should().BeFalse();
        node.IsOr.Should().BeFalse();
        node.Subquery.Should().BeSameAs(subquery);
    }

    [Fact]
    public void WhereNotExists_AddsExistsWhereNode_WithIsNotTrue()
    {
        var subquery = Sql.From<Customer>().Where(c => c.IsActive == true);
        var query = Sql.From<Order>().WhereNotExists(subquery);

        var node = query.Nodes.OfType<ExistsWhereNode>().Single();
        node.IsNot.Should().BeTrue();
        node.IsOr.Should().BeFalse();
    }

    [Fact]
    public void OrExists_AddsExistsWhereNode_WithIsOrTrue_AndIsNotFalse()
    {
        var subquery = Sql.From<Customer>().Where(c => c.IsActive == true);
        var query = Sql.From<Order>()
            .Where(o => o.TotalAmount > 1000)
            .OrExists(subquery);

        var node = query.Nodes.OfType<ExistsWhereNode>().Single();
        node.IsOr.Should().BeTrue();
        node.IsNot.Should().BeFalse();
    }

    [Fact]
    public void OrNotExists_AddsExistsWhereNode_WithIsOrTrue_AndIsNotTrue()
    {
        var subquery = Sql.From<Customer>().Where(c => c.IsActive == true);
        var query = Sql.From<Order>()
            .Where(o => o.TotalAmount > 1000)
            .OrNotExists(subquery);

        var node = query.Nodes.OfType<ExistsWhereNode>().Single();
        node.IsOr.Should().BeTrue();
        node.IsNot.Should().BeTrue();
    }

    [Fact]
    public void WhereExists_IsImmutable_DoesNotMutateOriginalQuery()
    {
        var subquery = Sql.From<Customer>();
        var original = Sql.From<Order>();
        var modified = original.WhereExists(subquery);

        original.Nodes.OfType<ExistsWhereNode>().Should().BeEmpty();
        modified.Nodes.OfType<ExistsWhereNode>().Should().HaveCount(1);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // SQL output tests using PostgreSQL compiler
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void WhereExists_Build_PostgreSql_EmitsExistsSql()
    {
        var compiler = new EricksonLopez.SqlBuilder.PostgreSql.PostgreSqlCompiler();
        var subquery = Sql.From<Customer>().Select("id").Where(c => c.IsActive == true);
        var query = Sql.From<Order>().WhereExists(subquery);

        var result = query.Build(compiler);

        result.Sql.Should().Contain("WHERE EXISTS (");
        result.Sql.Should().Contain("FROM \"customers\"");
    }

    [Fact]
    public void WhereNotExists_Build_PostgreSql_EmitsNotExistsSql()
    {
        var compiler = new EricksonLopez.SqlBuilder.PostgreSql.PostgreSqlCompiler();
        var subquery = Sql.From<Customer>().Select("id").Where(c => c.IsActive == true);
        var query = Sql.From<Order>().WhereNotExists(subquery);

        var result = query.Build(compiler);

        result.Sql.Should().Contain("WHERE NOT EXISTS (");
    }

    [Fact]
    public void OrExists_Build_PostgreSql_EmitsOrExistsSql()
    {
        var compiler = new EricksonLopez.SqlBuilder.PostgreSql.PostgreSqlCompiler();
        var subquery = Sql.From<Customer>().Select("id");
        var query = Sql.From<Order>()
            .Where(o => o.TotalAmount > 1000)
            .OrExists(subquery);

        var result = query.Build(compiler);

        result.Sql.Should().Contain("WHERE ");
        result.Sql.Should().Contain("OR EXISTS (");
    }

    [Fact]
    public void WhereExists_AndWhereExists_BuildsMultipleExistsWithAndOperator()
    {
        var compiler = new EricksonLopez.SqlBuilder.PostgreSql.PostgreSqlCompiler();
        var sub1 = Sql.From<Customer>().Select("id");
        var sub2 = Sql.From<Customer>().Select("id").Where(c => c.IsActive == true);

        var query = Sql.From<Order>()
            .WhereExists(sub1)
            .WhereExists(sub2); // second is AND

        var result = query.Build(compiler);

        result.Sql.Should().Contain("WHERE EXISTS (");
        result.Sql.Should().Contain("AND EXISTS (");
    }

    [Fact]
    public void ExistsWhereNode_Accept_CallsVisitorVisitMethod()
    {
        var subquery = Sql.From<Customer>();
        var node = new ExistsWhereNode(subquery, IsNot: false, IsOr: false);
        var visitor = new TrackingVisitor();

        node.Accept(visitor);

        visitor.VisitedExists.Should().BeTrue();
    }

    private class TrackingVisitor : EricksonLopez.SqlBuilder.Abstractions.SqlVisitorBase
    {
        public bool VisitedExists { get; private set; }
        public override void Visit(ExistsWhereNode node) => VisitedExists = true;
        public override void VisitUnknown(ISqlNode node) { }
    }
}




