// Copyright © Erickson Lopez. MIT License.
using System;
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

namespace EricksonLopez.SqlBuilder.SqlServer.Tests;

public class SqlServerCompilerTests
{
    [Fact]
    public Task Compile_WhenSelectWithTop_ShouldGenerateTopSyntax()
    {
        // Arrange
        var query = Sql.From<User>().Select("Id", "FirstName").Limit(10);
        
        // Act & Assert
        return query.VerifyQueryAsync(new SqlServerCompiler());
    }
    
    [Fact]
    public Task Compile_WhenSelectWithOffset_ShouldGenerateOffsetFetch()
    {
        // Arrange
        var query = Sql.From<User>().Select("Id").Offset(20).Limit(10);
        
        // Act & Assert
        return query.VerifyQueryAsync(new SqlServerCompiler());
    }
    
    [Fact]
    public Task Compile_WhenInsert_ShouldEscapeTableName()
    {
        // Arrange
        var query = Sql.Insert(ObjectMother.CreateUser());
        
        // Act & Assert
        return query.VerifyQueryAsync(new SqlServerCompiler());
    }

    [Fact]
    public Task Compile_WhenUpdate_ShouldEscapeTableName()
    {
        // Arrange
        var query = Sql.Update<User>().WhereAll();
        
        // Act & Assert
        return query.VerifyQueryAsync(new SqlServerCompiler());
    }

    [Fact]
    public Task Compile_WhenDelete_ShouldEscapeTableName()
    {
        // Arrange
        var query = Sql.Delete<User>().WhereAll();
        
        // Act & Assert
        return query.VerifyQueryAsync(new SqlServerCompiler());
    }

    [Fact]
    public Task Compile_WhenWhere_ShouldCompileSuccessfully()
    {
        // Arrange
        var query = Sql.From<User>().Select("*").Where($"Id = {1}").Or(u => u.FirstName == "Admin");
        
        // Act & Assert
        return query.VerifyQueryAsync(new SqlServerCompiler());
    }
    
    [Fact]
    public Task Compile_WhenDistinct_ShouldIncludeDistinctKeyword()
    {
        // Arrange
        var query = Sql.From<User>().Select("FirstName").Distinct();
        
        // Act & Assert
        return query.VerifyQueryAsync(new SqlServerCompiler());
    }

    [Fact]
    public void Compile_OrderBy_NullsFirst_EmulatesCaseWhen()
    {
        var compiler = new SqlServerCompiler();
        var query = Sql.From<User>().OrderBy(u => u.CreatedAt, EricksonLopez.SqlBuilder.Abstractions.Nodes.NullsPosition.First);
        var result = compiler.Compile(query);
        result.Sql.Should().Be("SELECT * FROM [users] ORDER BY CASE WHEN [created_at] IS NULL THEN 0 ELSE 1 END, [created_at]");
    }

    [Fact]
    public void Compile_OrderByDescending_NullsLast_EmulatesCaseWhen()
    {
        var compiler = new SqlServerCompiler();
        var query = Sql.From<User>().OrderByDescending(u => u.CreatedAt, EricksonLopez.SqlBuilder.Abstractions.Nodes.NullsPosition.Last);
        var result = compiler.Compile(query);
        result.Sql.Should().Be("SELECT * FROM [users] ORDER BY CASE WHEN [created_at] IS NULL THEN 1 ELSE 0 END, [created_at] DESC");
    }

    [Fact]
    public void Compile_OrderBy_ReferenceTypeProperty_NullsFirst_EmulatesCaseWhen()
    {
        var compiler = new SqlServerCompiler();
        var query = Sql.From<User>().OrderBy(u => u.Email, EricksonLopez.SqlBuilder.Abstractions.Nodes.NullsPosition.First);
        var result = compiler.Compile(query);
        result.Sql.Should().Be("SELECT * FROM [users] ORDER BY CASE WHEN [email] IS NULL THEN 0 ELSE 1 END, [email]");
    }

    [Fact]
    public void Compile_OrderByDescending_ReferenceTypeProperty_NullsLast_EmulatesCaseWhen()
    {
        var compiler = new SqlServerCompiler();
        var query = Sql.From<User>().OrderByDescending(u => u.Email, EricksonLopez.SqlBuilder.Abstractions.Nodes.NullsPosition.Last);
        var result = compiler.Compile(query);
        result.Sql.Should().Be("SELECT * FROM [users] ORDER BY CASE WHEN [email] IS NULL THEN 1 ELSE 0 END, [email] DESC");
    }

    [Fact]
    public void Compile_Update_MultipleConcurrencyTokensWithoutWhere_AppendsWhereThenAnd()
    {
        var compiler = new SqlServerCompiler();
        var context = new CompilationContext(new ParameterManager("@", 2100));
        var visitor = compiler.CreateVisitor(context);

        var nodes = new ISqlNode[]
        {
            new UpdateNode("users"),
            new SetNode("name", "Bob"),
            new ConcurrencyTokenNode("version", 1),
            new ConcurrencyTokenNode("status", "Active")
        };

        compiler.CompileUpdate(nodes, visitor, context);
        context.Sql.ToString().Should().Be("UPDATE [users] SET [name] = @p0, [version] = [version] + 1, [status] = [status] + 1 WHERE [version] = @p1 AND [status] = @p2 ");
    }

    [Fact]
    public void Compile_WindowFunction_WithFilter_ThrowsNotSupportedException()
    {
        var compiler = new SqlServerCompiler();
        var context = new CompilationContext(new ParameterManager());
        var visitor = compiler.CreateVisitor(context);

        var node = new WindowFunctionNode(
            "SUM", "Amount", null, null,
            Array.Empty<string>(), Array.Empty<string>(), Array.Empty<bool>(),
            "sum_val", FilterRaw: "Status = 'Active'");

        var act = () => visitor.Visit(node);

        act.Should().Throw<NotSupportedException>()
            .WithMessage("*SQL Server does not support the FILTER (WHERE ...) clause on window functions*");
    }
}







