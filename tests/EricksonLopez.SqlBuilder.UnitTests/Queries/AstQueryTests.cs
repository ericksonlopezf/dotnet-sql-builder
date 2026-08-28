// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using System;
using System.Collections.Generic;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Testing;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests.Queries;

public class AstQueryTests
{

    private class DummyCompiler : ISqlCompiler
    {
        public bool SupportsCapability(ProviderCapability capability) => true;
        public SqlResult Compile(ISqlQuery query) => Compile(query, null);
        public SqlResult Compile(ISqlQuery query, IParameterManager? existingParameters) => new SqlResult("DUMMY", new Dictionary<string, object?>());
        public string Escape(string identifier) => identifier;
        public string EscapeIdentifier(string identifier) => identifier;
        public IParameterManager CreateParameterManager() => new ParameterManager();
        public void CompileSelect(IReadOnlyList<ISqlNode> nodes, ISqlVisitor visitor) {}
        public void CompileInsert(IReadOnlyList<ISqlNode> nodes, ISqlVisitor visitor) {}
        public void CompileUpdate(IReadOnlyList<ISqlNode> nodes, ISqlVisitor visitor) {}
        public void CompileDelete(IReadOnlyList<ISqlNode> nodes, ISqlVisitor visitor) {}
        public void CompileMerge(IReadOnlyList<ISqlNode> nodes, ISqlVisitor visitor) {}
    }

    [Fact]
    public void IAstQuery_Nodes_ReturnsNodesList()
    {
        IAstQuery select = new SelectQuery<DummyEntity>();
        IAstQuery insert = new InsertQuery<DummyEntity>();
        IAstQuery update = new UpdateQuery<DummyEntity>();
        IAstQuery delete = new DeleteQuery<DummyEntity>();

        select.Nodes.Should().NotBeNull();
        insert.Nodes.Should().NotBeNull();
        update.Nodes.Should().NotBeNull();
        delete.Nodes.Should().NotBeNull();
    }

    [Fact]
    public void Build_CallsCompilerCompile()
    {
        var compiler = new DummyCompiler();
        
        var select = new SelectQuery<DummyEntity>();
        var insert = new InsertQuery<DummyEntity>();
        var update = new UpdateQuery<DummyEntity>();
        var delete = new DeleteQuery<DummyEntity>();

        select.Build(compiler).Sql.Should().Be("DUMMY");
        insert.Build(compiler).Sql.Should().Be("DUMMY");
        update.Build(compiler).Sql.Should().Be("DUMMY");
        delete.Build(compiler).Sql.Should().Be("DUMMY");
    }
}



