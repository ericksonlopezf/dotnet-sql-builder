// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.MySql;
using EricksonLopez.SqlBuilder.Oracle;
using EricksonLopez.SqlBuilder.PostgreSql;
using EricksonLopez.SqlBuilder.Sqlite;
using EricksonLopez.SqlBuilder.SqlServer;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests.Queries;

public class ConcurrencyTokenTests
{
    private sealed class User : EricksonLopez.SqlBuilder.Annotations.ISqlEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Version { get; set; }
        public Guid RowVersion { get; set; }
        public DateTime UpdatedAt { get; set; }

        public string GetTableName() => "users";
        public string[] GetColumnNames() => new[] { "id", "name", "version", "row_version", "updated_at" };
        public object?[] GetValues() => new object?[] { Id, Name, Version, RowVersion, UpdatedAt };
        public string[] GetAllColumnNames() => GetColumnNames();
        public object?[] GetAllValues() => GetValues();
        public IReadOnlyDictionary<string, string> GetPropertyMap() => new Dictionary<string, string>
        {
            { "Id", "id" }, { "Name", "name" }, { "Version", "version" }, { "RowVersion", "row_version" }, { "UpdatedAt", "updated_at" }
        };
        public string[] GetIndexedColumns() => Array.Empty<string>();
    }

    [Fact]
    public void WithConcurrencyToken_Int_GeneratesAutoIncrementSetAndWhereClause()
    {
        var query = Sql.Update<User>()
            .Set(u => u.Name, "Alice")
            .Where(u => u.Id == 42)
            .WithConcurrencyToken(u => u.Version, expectedValue: 7);

        var compiler = new SqlServerCompiler();
        var result = compiler.Compile(query);

        result.Sql.Should().Contain("[version] = [version] + 1");
        result.Sql.Should().Contain("AND [version] = @p");
        result.Parameters.Values.Should().Contain(7);
    }

    [Fact]
    public void WithConcurrencyToken_Guid_UsesExplicitNewValue()
    {
        var expectedGuid = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var newGuid = Guid.NewGuid();

        var query = Sql.Update<User>()
            .Set(u => u.Name, "Bob")
            .Where(u => u.Id == 1)
            .WithConcurrencyToken(u => u.RowVersion, expectedValue: expectedGuid, newValue: newGuid);

        var compiler = new SqlServerCompiler();
        var result = compiler.Compile(query);

        result.Sql.Should().NotContain("[row_version] = [row_version] + 1");
        result.Sql.Should().Contain("AND [row_version] = @p");
        result.Parameters.Values.Should().Contain(expectedGuid);
        result.Parameters.Values.Should().Contain(newGuid);
    }

    [Fact]
    public void WithConcurrencyToken_NonMemberExpression_Throws()
    {
        var act = () => Sql.Update<User>()
            .Set(u => u.Name, "Alice")
            .Where(u => u.Id == 1)
            .WithConcurrencyToken(u => u.Version + 1, expectedValue: 7);

        act.Should().Throw<ArgumentException>().WithMessage("*member expression*");
    }

    [Fact]
    public void WithConcurrencyToken_NoExistingWhere_GeneratesWhereClause()
    {
        var query = Sql.Update<User>()
            .Set(u => u.Name, "Alice")
            .WithConcurrencyToken(u => u.Version, expectedValue: 3);

        var compiler = new SqlServerCompiler();
        var result = compiler.Compile(query);

        result.Sql.Should().Contain("WHERE");
        result.Sql.Should().Contain("[version] = @p");
    }

    [Fact]
    public void WithConcurrencyToken_AllDialects_CompileWithoutException()
    {
        var compilers = new ISqlCompiler[]
        {
            new SqlServerCompiler(),
            new PostgreSqlCompiler(),
            new MySqlCompiler(),
            new SqliteCompiler(),
            new OracleCompiler()
        };

        foreach (var compiler in compilers)
        {
            var query = Sql.Update<User>()
                .Set(u => u.Name, "Test")
                .Where(u => u.Id == 1)
                .WithConcurrencyToken(u => u.Version, 3);
            var act = () => query.Build(compiler);
            act.Should().NotThrow($"Compiler {compiler.GetType().Name} should handle ConcurrencyToken");
        }
    }

    [Fact]
    public void DbConcurrencyException_Constructor_SetsProperties()
    {
        var ex = new DbConcurrencyException("User", 0);

        ex.EntityTypeName.Should().Be("User");
        ex.RowsAffected.Should().Be(0);
        ex.Message.Should().Contain("User");
        ex.Message.Should().Contain("conflict");
    }

    [Fact]
    public void DbConcurrencyException_WithInnerException_Chains()
    {
        var inner = new Exception("inner");
        var ex = new DbConcurrencyException("Order", 0, inner);

        ex.InnerException.Should().BeSameAs(inner);
        ex.EntityTypeName.Should().Be("Order");
    }

    [Fact]
    public void ConcurrencyTokenNode_Accept_CallsVisitorVisit()
    {
        var node = new ConcurrencyTokenNode("version", 1, null, true);
        var visitor = new TrackingVisitor();
        node.Accept(visitor);
        visitor.ConcurrencyTokenVisited.Should().BeTrue();
    }

    private class TrackingVisitor : SqlVisitorBase
    {
        public bool ConcurrencyTokenVisited { get; private set; }
        public override void Visit(ConcurrencyTokenNode node) => ConcurrencyTokenVisited = true;
    }
}
