// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Testing;
using EricksonLopez.SqlBuilder.Testing.DataBuilders;
using EricksonLopez.SqlBuilder.Testing.Domain;
using Xunit;

namespace EricksonLopez.SqlBuilder.Oracle.Tests;

public class OracleCompilerTestsExtended
{
    private class FakeQuery : IAstQuery
    {
        public string? Tag => null;
        private readonly ISqlNode[] _nodes;
        public FakeQuery(params ISqlNode[] nodes) => _nodes = nodes;
        public IReadOnlyList<ISqlNode> Nodes => _nodes;
        public SqlResult Build(ISqlCompiler compiler) => compiler.Compile(this);
        public void CompileTo(ISqlCompiler compiler, ISqlVisitor visitor)
        {
            throw new NotImplementedException();
        }
    }

    private class TestableOracleCompiler : OracleCompiler, IDisposable
    {
        public CompilationContext Context { get; }
        public SqlVisitorBase Visitor { get; }

        public TestableOracleCompiler()
        {
            Context = new CompilationContext(CreateParameterManager());
            Visitor = CreateVisitor(Context);
        }
        
        public void CallCompileUpdate(List<ISqlNode> nodes) => this.CompileUpdate(nodes, Visitor, Context);
        public void CallCompileDelete(List<ISqlNode> nodes) => this.CompileDelete(nodes, Visitor, Context);
        public void CallCompileInsert(List<ISqlNode> nodes) => this.CompileInsert(nodes, Visitor, Context);
        public void CallCompileLimitOffset(LimitOffsetNode? node) => this.CompileLimitOffset(node, Visitor, Context);
        
        public string GetSql() => Context.Sql.ToString();
        public IParameterManager GetParameters() => Context.Parameters;

        public void Dispose()
        {
        }
    }

    [Fact]
    public void Visit_OnConflictNode_Throws()
    {
        var compiler = new OracleCompiler();
        Action act = () => compiler.Compile(new FakeQuery(new InsertNode("users", Array.Empty<string>()), new OnConflictNode(new[] { "id" })));
        act.Should().Throw<NotSupportedException>()
            .WithMessage("Oracle does not support ON CONFLICT syntax. Use Sql.Raw() with a MERGE INTO statement instead.");
    }

    [Fact]
    public void ParameterManager_Boolean_False_Returns_0()
    {
        var mgr = new OracleParameterManager();
        var key = mgr.Add(false);
        mgr.GetParameters()[key.Replace(":", "")].Should().Be(0);
    }

    [Fact]
    public void ParameterManager_Boolean_True_Returns_1()
    {
        var mgr = new OracleParameterManager();
        var key = mgr.Add(true);
        mgr.GetParameters()[key.Replace(":", "")].Should().Be(1);
    }

    [Fact]
    public void ParameterManager_AddNamed_Processes_Value()
    {
        var mgr = new OracleParameterManager();
        mgr.AddNamed("myBool", true);
        mgr.GetParameters()["myBool"].Should().Be(1);
    }

    [Fact]
    public void Compile_Insert_NoInsertNode()
    {
        using var compiler = new TestableOracleCompiler();
        compiler.CallCompileInsert(new List<ISqlNode> { new ValuesNode(new[] { new object[] { 1 }, new object[] { 2 } }) });
        compiler.GetSql().Should().Be("");
    }

    [Fact]
    public void Compile_LimitOffset_Empty()
    {
        using var compiler = new TestableOracleCompiler();
        compiler.CallCompileLimitOffset(null);
        compiler.GetSql().Should().Be("");
    }

    [Fact]
    public Task Compile_WhenSelectWithTop_ShouldGenerateTopSyntax()
    {
        var query = Sql.From<User>().Select("Id", "FirstName").Limit(10);
        return query.VerifyQueryAsync(new OracleCompiler());
    }
    
    [Fact]
    public Task Compile_WhenSelectWithOffset_ShouldGenerateOffsetFetch()
    {
        var query = Sql.From<User>().Select("Id", "FirstName").Offset(20).Limit(10);
        return query.VerifyQueryAsync(new OracleCompiler());
    }

    [Fact]
    public void Compile_LimitOffset_Direct()
    {
        using var compiler = new TestableOracleCompiler();
        compiler.CallCompileLimitOffset(new LimitOffsetNode(10, 20));
        compiler.GetSql().Should().Be("OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY ");
    }

    [Fact]
    public void Compile_LimitOffset_Null_Direct()
    {
        using var compiler = new TestableOracleCompiler();
        compiler.CallCompileLimitOffset(new LimitOffsetNode(null, null));
        compiler.GetSql().Should().Be("");
    }
    
    [Fact]
    public Task Compile_WhenInsert_ShouldEscapeTableName()
    {
        var query = Sql.Insert(ObjectMother.CreateUser());
        return query.VerifyQueryAsync(new OracleCompiler());
    }

    [Fact]
    public Task Compile_WhenUpdate_ShouldEscapeTableName()
    {
        var query = Sql.Update<User>().WhereAll();
        return query.VerifyQueryAsync(new OracleCompiler());
    }

    [Fact]
    public Task Compile_WhenDelete_ShouldEscapeTableName()
    {
        var query = Sql.Delete<User>().WhereAll();
        return query.VerifyQueryAsync(new OracleCompiler());
    }

    [Fact]
    public Task Compile_WhenWhere_ShouldCompileSuccessfully()
    {
        var query = Sql.From<User>().Select("*").Where($"Id = {1}").Or(u => u.FirstName == "Admin");
        return query.VerifyQueryAsync(new OracleCompiler());
    }
    
    [Fact]
    public Task Compile_WhenDistinct_ShouldIncludeDistinctKeyword()
    {
        var query = Sql.From<User>().Select("FirstName").Distinct();
        return query.VerifyQueryAsync(new OracleCompiler());
    }

    [Fact]
    public void Compile_Delete_With_Where_And_Returning()
    {
        using var compiler = new TestableOracleCompiler();
        compiler.CallCompileDelete(new List<ISqlNode> {
            new DeleteNode("table"),
            new RawWhereNode("status = 1"),
            new ReturningNode(new[] { "id", "name" })
        });

        compiler.GetSql().Should().Be("DELETE FROM \"TABLE\" WHERE status = 1 RETURNING \"ID\", \"NAME\" INTO :out_id, :out_name ");
    }

    [Fact]
    public void Compile_Delete_NoDeleteNode()
    {
        using var compiler = new TestableOracleCompiler();
        compiler.CallCompileDelete(new List<ISqlNode>());
        compiler.GetSql().Should().Be("");
    }

    [Fact]
    public void Compile_Update_With_Set_Multiple_Where_And_Returning()
    {
        using var compiler = new TestableOracleCompiler();
        compiler.CallCompileUpdate(new List<ISqlNode> {
            new UpdateNode("table"),
            new SetNode("status", 1),
            new SetNode("name", "test"),
            new RawWhereNode("id = 1"),
            new ReturningNode(new[] { "id", "name" })
        });

        compiler.GetSql().Should().Be("UPDATE \"TABLE\" SET \"STATUS\" = :p0, \"NAME\" = :p1 WHERE id = 1 RETURNING \"ID\", \"NAME\" INTO :out_id, :out_name ");
        compiler.GetParameters().GetParameters().Values.Should().Contain(1);
        compiler.GetParameters().GetParameters().Values.Should().Contain("test");
    }

    [Fact]
    public void Compile_Update_No_Set()
    {
        using var compiler = new TestableOracleCompiler();
        compiler.CallCompileUpdate(new List<ISqlNode> {
            new UpdateNode("table"),
            new RawWhereNode("id = 1")
        });

        compiler.GetSql().Should().Be("UPDATE \"TABLE\" WHERE id = 1 ");
    }

    [Fact]
    public void Compile_Update_NoUpdateNode()
    {
        using var compiler = new TestableOracleCompiler();
        compiler.CallCompileUpdate(new List<ISqlNode>());
        compiler.GetSql().Should().Be("");
    }

    [Fact]
    public void Compile_Insert_With_DefaultValues()
    {
        using var compiler = new TestableOracleCompiler();
        compiler.CallCompileInsert(new List<ISqlNode> {
            new InsertNode("table", new string[0]),
            new DefaultValuesNode()
        });

        compiler.GetSql().Should().Be("INSERT INTO \"TABLE\" /* Oracle: specify explicit DEFAULT values per column via VALUES () */ ");
    }

    [Fact]
    public void Compile_Insert_Returning_Empty_Throws()
    {
        using var compiler = new TestableOracleCompiler();
        Action act = () => compiler.CallCompileInsert(new List<ISqlNode> {
            new InsertNode("table", new[] { "id" }),
            new ReturningNode(new string[0])
        });

        act.Should().Throw<NotSupportedException>()
            .WithMessage("Oracle RETURNING clause requires explicit column names. Use .Returning(\"col1\", \"col2\") instead of .Returning().");
    }

    [Fact]
    public void Compile_Insert_MultiRow_InsertAll()
    {
        var values = new ValuesNode(new[] { 
            new object[] { 1, "test1" }, 
            new object[] { 2, "test2" } 
        });

        using var compiler = new TestableOracleCompiler();
        compiler.CallCompileInsert(new List<ISqlNode> {
            new InsertNode("table", new[] { "id", "name" }),
            values
        });

        compiler.GetSql().Should().Be("BEGIN INSERT INTO \"TABLE\" (\"ID\", \"NAME\") VALUES (:p0, :p1); INSERT INTO \"TABLE\" (\"ID\", \"NAME\") VALUES (:p2, :p3); END; ");
        compiler.GetParameters().GetParameters().Values.Should().Contain(1);
        compiler.GetParameters().GetParameters().Values.Should().Contain("test1");
        compiler.GetParameters().GetParameters().Values.Should().Contain(2);
        compiler.GetParameters().GetParameters().Values.Should().Contain("test2");
    }

    [Fact]
    public void Compile_LimitOffset_NodeWithNullValues()
    {
        using var compiler = new TestableOracleCompiler();
        compiler.CallCompileLimitOffset(new LimitOffsetNode(null, null));
        compiler.GetSql().Should().Be("");
    }

    [Fact]
    public void Compile_Insert_MultiRow_InsertAll_NoColumns()
    {
        var values = new ValuesNode(new[] { 
            new object[] { 1 }, 
            new object[] { 2 } 
        });

        using var compiler = new TestableOracleCompiler();
        compiler.CallCompileInsert(new List<ISqlNode> {
            new InsertNode("table", new string[0]),
            values
        });

        compiler.GetSql().Should().Be("BEGIN INSERT INTO \"TABLE\" VALUES (:p0); INSERT INTO \"TABLE\" VALUES (:p1); END; ");
    }

    [Fact]
    public void Compile_Insert_MultiRow_NoInsertNode()
    {
        var values = new ValuesNode(new[] { 
            new object[] { 1 }, 
            new object[] { 2 } 
        });

        using var compiler = new TestableOracleCompiler();
        compiler.CallCompileInsert(new List<ISqlNode> {
            values
        });

        compiler.GetSql().Should().Be("");
    }


    [Fact]
    public void Compile_Insert_MultiRow_With_Returning_Throws()
    {
        var values = new ValuesNode(new[] { 
            new object[] { 1 }, 
            new object[] { 2 } 
        });

        using var compiler = new TestableOracleCompiler();
        Action act = () => compiler.CallCompileInsert(new List<ISqlNode> {
            new InsertNode("table", new string[0]),
            values,
            new ReturningNode(new[] { "id" })
        });

        act.Should().Throw<NotSupportedException>()
            .WithMessage("Oracle does not support RETURNING with multi-row INSERT ALL. Insert rows individually to use RETURNING.");
    }

    [Fact]
    public void Compile_Insert_OnConflict_Throws()
    {
        using var compiler = new TestableOracleCompiler();
        Action act = () => compiler.CallCompileInsert(new List<ISqlNode> {
            new InsertNode("table", new[] { "id" }),
            new OnConflictNode(new[] { "id" })
        });

        act.Should().Throw<NotSupportedException>()
            .WithMessage("Oracle does not support ON CONFLICT syntax. Use Sql.Raw() with a MERGE INTO statement instead.");
    }
}





