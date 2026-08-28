// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests.Queries;

/// <summary>
/// Unit tests for the BETWEEN SQL expression built via Sql.Between extension method.
/// </summary>
public class BetweenTests
{
    private class Product : EricksonLopez.SqlBuilder.Annotations.ISqlEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }

        public string GetTableName() => "products";
        public string[] GetColumnNames() => new[] { "id", "name", "price", "stock_quantity" };
        public object?[] GetValues() => new object?[] { Id, Name, Price, StockQuantity };
        public string[] GetAllColumnNames() => GetColumnNames();
        public object?[] GetAllValues() => GetValues();
        public IReadOnlyDictionary<string, string> GetPropertyMap() => new Dictionary<string, string>
        {
            { "Id", "id" }, { "Name", "name" }, { "Price", "price" }, { "StockQuantity", "stock_quantity" }
        };
        public string[] GetIndexedColumns() => System.Array.Empty<string>();
    }

    private class User : EricksonLopez.SqlBuilder.Annotations.ISqlEntity
    {
        public int Id { get; set; }
        public int Age { get; set; }
        public decimal Salary { get; set; }
        public string Name { get; set; } = string.Empty;

        public string GetTableName() => "users";
        public string[] GetColumnNames() => new[] { "id", "age", "salary", "name" };
        public object?[] GetValues() => new object?[] { Id, Age, Salary, Name };
        public string[] GetAllColumnNames() => GetColumnNames();
        public object?[] GetAllValues() => GetValues();
        public IReadOnlyDictionary<string, string> GetPropertyMap() => new Dictionary<string, string>
        {
            { "Id", "id" }, { "Age", "age" }, { "Salary", "salary" }, { "Name", "name" }
        };
        public string[] GetIndexedColumns() => System.Array.Empty<string>();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BETWEEN on integer column
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Between_Int_EmitsBetweenSql()
    {
        var compiler = new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler();
        var query = Sql.From<User>().Where(u => u.Age.Between(18, 65));

        var result = query.Build(compiler);

        result.Sql.Should().Contain("BETWEEN");
        result.Sql.Should().Contain("AND");
        result.Parameters.Should().HaveCount(2);
    }

    [Fact]
    public void Between_Int_CorrectParameterValues()
    {
        var compiler = new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler();
        var query = Sql.From<User>().Where(u => u.Age.Between(18, 65));

        var result = query.Build(compiler);

        // Parameters should contain the two boundary values
        var values = result.Parameters.Values;
        values.Should().Contain(18);
        values.Should().Contain(65);
    }

    [Fact]
    public void Between_Decimal_EmitsBetweenSql()
    {
        var compiler = new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler();
        var query = Sql.From<Product>().Where(p => p.Price.Between(10.0m, 999.99m));

        var result = query.Build(compiler);

        result.Sql.Should().Contain("BETWEEN");
        result.Sql.Should().Contain("AND");
    }

    [Fact]
    public void Between_WithCapturedVariables_CorrectParameters()
    {
        int minAge = 21;
        int maxAge = 60;

        var compiler = new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler();
        var query = Sql.From<User>().Where(u => u.Age.Between(minAge, maxAge));

        var result = query.Build(compiler);

        result.Sql.Should().Contain("BETWEEN");
        var values = result.Parameters.Values;
        values.Should().Contain(21);
        values.Should().Contain(60);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Combination: BETWEEN + other WHERE predicates
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Between_CombinedWithAnd_EmitsCorrectSql()
    {
        var compiler = new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler();
        var query = Sql.From<User>()
            .Where(u => u.Age.Between(18, 65))
            .And(u => u.Name == "Alice");

        var result = query.Build(compiler);

        result.Sql.Should().Contain("BETWEEN");
        result.Sql.Should().Contain("AND");
        result.Sql.Should().Contain("Alice".Length > 0 ? "name" : "");
        result.Parameters.Should().HaveCount(3); // from, to, name
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PostgreSQL dialect
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Between_PostgreSql_EmitsBetweenSql()
    {
        var compiler = new EricksonLopez.SqlBuilder.PostgreSql.PostgreSqlCompiler();
        var query = Sql.From<Product>().Where(p => p.StockQuantity.Between(5, 100));

        var result = query.Build(compiler);

        result.Sql.Should().Contain("BETWEEN");
        result.Sql.Should().Contain("AND");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SQL structure verification
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Between_Int_SqlContainsColumnName()
    {
        var compiler = new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler();
        var query = Sql.From<User>().Where(u => u.Age.Between(18, 65));

        var result = query.Build(compiler);

        // SQLite doesn't quote column names the same way — confirm 'age' appears
        result.Sql.ToLower().Should().Contain("age");
    }

    [Fact]
    public void Between_ReturnsSelectQuery_AllowsFurtherChaining()
    {
        // Fluent API should remain chainable after Between
        var compiler = new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler();
        var query = Sql.From<User>()
            .Where(u => u.Age.Between(18, 65))
            .Select("id", "age")
            .Limit(10);

        var result = query.Build(compiler);

        result.Sql.Should().Contain("BETWEEN");
        result.Sql.Should().Contain("LIMIT");
    }
}




