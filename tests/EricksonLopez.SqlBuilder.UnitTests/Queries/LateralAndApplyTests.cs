// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.PostgreSql;
using EricksonLopez.SqlBuilder.SqlServer;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests.Queries;

public class LateralAndApplyTests
{
    private sealed class Customer : EricksonLopez.SqlBuilder.Annotations.ISqlEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";

        public string GetTableName() => "customers";
        public string[] GetColumnNames() => new[] { "id", "name" };
        public object?[] GetValues() => new object?[] { Id, Name };
        public string[] GetAllColumnNames() => GetColumnNames();
        public object?[] GetAllValues() => GetValues();
        public IReadOnlyDictionary<string, string> GetPropertyMap() => new Dictionary<string, string> { { "Id", "id" }, { "Name", "name" } };
        public string[] GetIndexedColumns() => Array.Empty<string>();
    }

    private sealed class Order : EricksonLopez.SqlBuilder.Annotations.ISqlEntity
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; }

        public string GetTableName() => "orders";
        public string[] GetColumnNames() => new[] { "id", "customer_id", "amount", "created_at" };
        public object?[] GetValues() => new object?[] { Id, CustomerId, Amount, CreatedAt };
        public string[] GetAllColumnNames() => GetColumnNames();
        public object?[] GetAllValues() => GetValues();
        public IReadOnlyDictionary<string, string> GetPropertyMap() => new Dictionary<string, string> { { "Id", "id" }, { "CustomerId", "customer_id" }, { "Amount", "amount" }, { "CreatedAt", "created_at" } };
        public string[] GetIndexedColumns() => Array.Empty<string>();
    }

    [Fact]
    public void LateralJoin_WithSubqueryFactory_CompilesPostgreSql()
    {
        var query = Sql.From<Customer>()
            .Where(c => c.Id == 42)
            .LateralJoin<Order>(
                sub => sub.Where(o => o.Amount > 100m).OrderByDescending(o => o.CreatedAt).Limit(3),
                "recent_orders",
                "TRUE"
            );

        var compiler = new PostgreSqlCompiler();
        var result = compiler.Compile(query);

        result.Sql.Should().Contain("INNER JOIN LATERAL (SELECT * FROM \"orders\" WHERE (amount > @p0) ORDER BY \"created_at\" DESC LIMIT 3) AS \"recent_orders\" ON TRUE");
        result.Sql.Should().Contain("WHERE (id = @p1)");
        result.Parameters.Should().ContainKey("p0").WhoseValue.Should().Be(100m);
        result.Parameters.Should().ContainKey("p1").WhoseValue.Should().Be(42);
    }

    [Fact]
    public void LateralLeftJoin_WithSubqueryFactory_CompilesPostgreSql()
    {
        var query = Sql.From<Customer>()
            .LateralLeftJoin<Order>(
                sub => sub.Where(o => o.Amount > 500m),
                "high_value_orders",
                (c, o) => c.Id == o.CustomerId
            );

        var compiler = new PostgreSqlCompiler();
        var result = compiler.Compile(query);

        result.Sql.Should().Contain("LEFT JOIN LATERAL (SELECT * FROM \"orders\" WHERE (amount > @p0)) AS \"high_value_orders\" ON (id = customer_id)");
        result.Parameters.Should().ContainKey("p0").WhoseValue.Should().Be(500m);
    }

    [Fact]
    public void CrossApply_WithSubqueryFactory_CompilesSqlServer()
    {
        var query = Sql.From<Customer>()
            .Where(c => c.Id == 10)
            .CrossApply<Order>(
                sub => sub.Where(o => o.Amount > 200m),
                "o"
            );

        var compiler = new SqlServerCompiler();
        var result = compiler.Compile(query);

        result.Sql.Should().Contain("CROSS APPLY (SELECT * FROM [orders] WHERE (amount > @p0)) AS [o]");
        result.Sql.Should().Contain("WHERE (id = @p1)");
        result.Parameters.Should().ContainKey("p0").WhoseValue.Should().Be(200m);
        result.Parameters.Should().ContainKey("p1").WhoseValue.Should().Be(10);
    }

    [Fact]
    public void OuterApply_WithSubqueryFactory_CompilesSqlServer()
    {
        var query = Sql.From<Customer>()
            .OuterApply<Order>(
                sub => sub.Where(o => o.Amount > 300m),
                "o"
            );

        var compiler = new SqlServerCompiler();
        var result = compiler.Compile(query);

        result.Sql.Should().Contain("OUTER APPLY (SELECT * FROM [orders] WHERE (amount > @p0)) AS [o]");
        result.Parameters.Should().ContainKey("p0").WhoseValue.Should().Be(300m);
    }

    [Fact]
    public void CrossApply_CompilesToCrossJoinLateral_InPostgreSql()
    {
        var query = Sql.From<Customer>()
            .CrossApply<Order>(
                sub => sub.Where(o => o.Amount > 50m),
                "o"
            );

        var compiler = new PostgreSqlCompiler();
        var result = compiler.Compile(query);

        result.Sql.Should().Contain("CROSS JOIN LATERAL (SELECT * FROM \"orders\" WHERE (amount > @p0)) AS \"o\"");
        result.Parameters.Should().ContainKey("p0").WhoseValue.Should().Be(50m);
    }

    [Fact]
    public void OuterApply_CompilesToLeftJoinLateral_InPostgreSql()
    {
        var query = Sql.From<Customer>()
            .OuterApply<Order>(
                sub => sub.Where(o => o.Amount > 75m),
                "o"
            );

        var compiler = new PostgreSqlCompiler();
        var result = compiler.Compile(query);

        result.Sql.Should().Contain("LEFT JOIN LATERAL (SELECT * FROM \"orders\" WHERE (amount > @p0)) AS \"o\"");
        result.Parameters.Should().ContainKey("p0").WhoseValue.Should().Be(75m);
    }
}




