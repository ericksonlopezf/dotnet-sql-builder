// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.PostgreSql;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

public class AdversarialSecurityAuditTests
{
    private readonly ISqlCompiler _compiler = new PostgreSqlCompiler();

    public class User : EricksonLopez.SqlBuilder.Annotations.ISqlEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

        public string GetTableName() => "users";
        public string[] GetColumnNames() => new[] { "id", "name", "role" };
        public object?[] GetValues() => new object?[] { Id, Name, Role };
        public string[] GetAllColumnNames() => GetColumnNames();
        public object?[] GetAllValues() => GetValues();
        public IReadOnlyDictionary<string, string> GetPropertyMap() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Id", "id" },
            { "Name", "name" },
            { "Role", "role" }
        };
        public string[] GetIndexedColumns() => new[] { "id" };
    }

    [Fact]
    public void OrderByDynamic_WithMaliciousAliasPrefix_ThrowsArgumentException_PreventingSqlInjection()
    {
        // Vector: Table/alias prefix injection
        // Exploit payload: "u; DROP TABLE users;--.Name"
        var query = new SelectQuery<User>();

        Action act = () => query.OrderByDynamic("u; DROP TABLE users;--.Name");

        // Vulnerability SQL-SEC-001 demonstration:
        // Prior to fix, prefix was unvalidated and resulted in ORDER BY u; DROP TABLE users;--.name
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("u' OR '1'='1.Name")]
    [InlineData("u\" OR \"1\"=\"1.Name")]
    [InlineData("u/*comment*/.Name")]
    [InlineData("u\0nullbyte.Name")]
    [InlineData("u;SELECT 1.Name")]
    [InlineData("1;DROP DATABASE.Name")]
    [InlineData("u--comment.Name")]
    public void OrderByDynamic_WithAdversarialPrefixes_ThrowsArgumentException(string maliciousSortBy)
    {
        var query = new SelectQuery<User>();

        Action act = () => query.OrderByDynamic(maliciousSortBy);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void OrderByDynamic_WithValidAliasAndProperty_Succeeds()
    {
        var query = new SelectQuery<User>();

        var result = query.OrderByDynamic("u.Name").Build(_compiler);

        result.Sql.Should().Contain("ORDER BY u.");
    }

    [Fact]
    public void WhereColumns_WithInjectionInColumnOrOperator_ThrowsArgumentException()
    {
        var query = new SelectQuery<User>();

        Action actCol = () => query.WhereColumns("id; DROP TABLE users;--", "=", "id");
        Action actOp = () => query.WhereColumns("id", "= 1; DROP TABLE users;--", "id");

        actCol.Should().Throw<ArgumentException>();
        actOp.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SeekPagination_GeneratesValidWhereAndOrderByClauses()
    {
        var query = new SelectQuery<User>();

        var result = query.Seek(u => u.Role, "admin", ascending: true, limit: 10).Build(_compiler);

        result.Sql.Should().Contain("WHERE role > @p0 ORDER BY \"role\" LIMIT 10");
    }

    [Fact]
    public void ParameterManager_NoCollisionOnSameNameDifferentValues_ThrowsInvalidOperationException()
    {
        var pm = new ParameterManager();
        pm.AddNamed("userId", 42);

        Action act = () => pm.AddNamed("userId", 999);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*collision*");
    }

    [Fact]
    public void ParameterManager_IdempotentOnSameNameSameValue()
    {
        var pm = new ParameterManager();
        var p1 = pm.AddNamed("userId", 42);
        var p2 = pm.AddNamed("userId", 42);

        p1.Should().Be("@userId");
        p2.Should().Be("@userId");
        pm.GetParameters().Count.Should().Be(1);
    }
}
