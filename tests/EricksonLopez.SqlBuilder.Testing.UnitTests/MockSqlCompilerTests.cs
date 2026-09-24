// Copyright © Erickson Lopez. MIT License.
using System;
using System.Text;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using NSubstitute;
using Xunit;

namespace EricksonLopez.SqlBuilder.Testing.UnitTests;

public class MockSqlCompilerTests
{
    [Fact]
    public void SupportsCapability_ReturnsTrueForAnyCapability()
    {
        var compiler = new MockSqlCompiler();
        Assert.True(compiler.SupportsCapability(ProviderCapability.Merge));
        Assert.True(compiler.SupportsCapability(ProviderCapability.Returning));
    }

    [Fact]
    public void Escape_ReturnsSameString()
    {
        var compiler = new MockSqlCompiler();
        Assert.Equal("test_identifier", compiler.Escape("test_identifier"));
        Assert.Equal("test_identifier", compiler.EscapeIdentifier("test_identifier"));

        var sb = new StringBuilder();
        compiler.EscapeIdentifier(sb, "span_id".AsSpan());
        Assert.Equal("span_id", sb.ToString());
    }

    [Fact]
    public void Compile_WithAstQuery_VisitsNodesAndFormatsTypeNames()
    {
        var compiler = new MockSqlCompiler();
        var query = Sql.From<TestingUser>().Where(u => u.Id == 10);

        var result = compiler.Compile(query);

        Assert.NotNull(result);
        Assert.Contains("[FromNode]", result.Sql);
        Assert.Contains("[ExpressionWhereNode]", result.Sql);
    }

    [Fact]
    public void Compile_WithExistingParameterManager_PreservesExistingParameters()
    {
        var compiler = new MockSqlCompiler();
        var pm = new ParameterManager();
        pm.AddNamed("existing_key", "existing_val");

        var rawQuery = new RawQuery("SELECT 42");
        var result = compiler.Compile(rawQuery, pm);

        Assert.Equal("SELECT 42", result.Sql);
        Assert.True(result.Parameters.ContainsKey("existing_key"));
        Assert.Equal("existing_val", result.Parameters["existing_key"]);
    }

    [Fact]
    public void Compile_WithoutExistingParameterManager_InstantiatesNewOne()
    {
        var compiler = new MockSqlCompiler();
        var rawQuery = new RawQuery("SELECT 42");

        var result = compiler.Compile(rawQuery);

        Assert.Equal("SELECT 42", result.Sql);
        Assert.Empty(result.Parameters);
    }

    [Fact]
    public void CompilationStubs_And_FactoryMethods_WorkAsExpected()
    {
        var compiler = new MockSqlCompiler();
        var visitor = Substitute.For<ISqlVisitor>();

        compiler.CompileSelect(Array.Empty<ISqlNode>(), visitor);
        compiler.CompileInsert(Array.Empty<ISqlNode>(), visitor);
        compiler.CompileUpdate(Array.Empty<ISqlNode>(), visitor);
        compiler.CompileDelete(Array.Empty<ISqlNode>(), visitor);

        var pm = compiler.CreateParameterManager();
        Assert.NotNull(pm);
        Assert.IsType<ParameterManager>(pm);
    }
}
