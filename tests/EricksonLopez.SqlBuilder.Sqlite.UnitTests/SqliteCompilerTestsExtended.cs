// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Testing;
using EricksonLopez.SqlBuilder.Testing.DataBuilders;
using EricksonLopez.SqlBuilder.Testing.Domain;
using Xunit;

namespace EricksonLopez.SqlBuilder.Sqlite.Tests;

public class SqliteCompilerTestsExtended
{
    [Fact]
    public Task Compile_WhenSelectWithTop_ShouldGenerateTopSyntax()
    {
        // Arrange
        var query = Sql.From<User>().Select("Id", "Name").Limit(10);
        
        // Act & Assert
        return query.VerifyQueryAsync(new SqliteCompiler());
    }
    
    [Fact]
    public Task Compile_WhenSelectWithOffset_ShouldGenerateOffsetFetch()
    {
        // Arrange
        var query = Sql.From<User>().Select("Id").Offset(20).Limit(10);
        
        // Act & Assert
        return query.VerifyQueryAsync(new SqliteCompiler());
    }
    
    [Fact]
    public Task Compile_WhenInsert_ShouldEscapeTableName()
    {
        // Arrange
        var query = Sql.Insert(ObjectMother.CreateUser());
        
        // Act & Assert
        return query.VerifyQueryAsync(new SqliteCompiler());
    }

    [Fact]
    public Task Compile_WhenUpdate_ShouldEscapeTableName()
    {
        // Arrange
        var query = Sql.Update<User>().WhereAll();
        
        // Act & Assert
        return query.VerifyQueryAsync(new SqliteCompiler());
    }

    [Fact]
    public Task Compile_WhenDelete_ShouldEscapeTableName()
    {
        // Arrange
        var query = Sql.Delete<User>().WhereAll();
        
        // Act & Assert
        return query.VerifyQueryAsync(new SqliteCompiler());
    }

    [Fact]
    public Task Compile_WhenWhere_ShouldCompileSuccessfully()
    {
        // Arrange
        var query = Sql.From<User>().Select("*").Where($"Id = {1}").Or(u => u.Id == 2);
        
        // Act & Assert
        return query.VerifyQueryAsync(new SqliteCompiler());
    }
    
    [Fact]
    public Task Compile_WhenDistinct_ShouldIncludeDistinctKeyword()
    {
        // Arrange
        var query = Sql.From<User>().Select("Name").Distinct();
        
        // Act & Assert
        return query.VerifyQueryAsync(new SqliteCompiler());
    }

    [Fact]
    public void RenderInsert_WithMultipleColumns_ShouldGenerateCorrectSql()
    {
        var compiler = new SqliteCompiler();
        var entity = new ThreeColumnEntity { Id = "1", Name = "Erick", Status = "A" };
        var mask = new[] { true, false, true }.AsSpan();
        
        var result = compiler.RenderInsert(entity, mask);
        
        result.Sql.Should().Be("INSERT INTO \"TestEntity\" (\"Id\", \"Status\") VALUES (@p0, @p1) RETURNING *");
        result.Parameters.Should().ContainKey("p0");
        result.Parameters.Should().ContainKey("p1");
    }

    [Fact]
    public void RenderUpdate_WithMultipleColumns_ShouldGenerateCorrectSql()
    {
        var compiler = new SqliteCompiler();
        var entity = new ThreeColumnEntity { Id = "1", Name = "Erick", Status = "A" };
        var setMask = new[] { false, true, true }.AsSpan();
        var whereMask = new[] { true, true, false }.AsSpan();
        
        var result = compiler.RenderUpdate(entity, setMask, whereMask);
        
        result.Sql.Should().Be("UPDATE \"TestEntity\" SET \"Name\" = @p0, \"Status\" = @p1 WHERE \"Id\" = @p2 AND \"Name\" = @p3 RETURNING *");
        result.Parameters.Should().ContainKey("p0");
        result.Parameters.Should().ContainKey("p1");
        result.Parameters.Should().ContainKey("p2");
        result.Parameters.Should().ContainKey("p3");
    }

    [Fact]
    public void Compile_OnConflictDoNothing_ShouldGenerateCorrectSql()
    {
        var compiler = new SqliteCompiler();
        var query = Sql.Insert(ObjectMother.CreateUser()).OnConflict("Id", "Username").DoNothing();
        
        var result = compiler.Compile(query);
        
        result.Sql.Trim().Should().EndWith("ON CONFLICT (\"Id\", \"Username\") DO NOTHING");
    }

    [Fact]
    public void Compile_OnConflictDoUpdate_ShouldGenerateCorrectSql()
    {
        var compiler = new SqliteCompiler();
        var query = Sql.Insert(ObjectMother.CreateUser()).OnConflict("Id").DoUpdate((System.FormattableString)$"\"Username\" = EXCLUDED.\"Username\"");
        
        var result = compiler.Compile(query);
        
        result.Sql.Trim().Should().EndWith("ON CONFLICT (\"Id\") DO UPDATE SET \"Username\" = EXCLUDED.\"Username\"");
    }

    [Fact]
    public void Compile_OnConflictDoUpdateWithSetClause_ShouldGenerateCorrectSql()
    {
        var compiler = new SqliteCompiler();
        var query = Sql.Insert(ObjectMother.CreateUser()).OnConflict("Id").DoUpdate(x => new { x.Username });
        
        var result = compiler.Compile(query);
        
        result.Sql.Trim().Should().EndWith("ON CONFLICT (\"Id\") DO UPDATE SET \"username\" = EXCLUDED.\"username\"");
    }

    [Fact]
    public void Compile_ReturningStar_ShouldGenerateCorrectSql()
    {
        var compiler = new SqliteCompiler();
        var query = Sql.Update<User>().WhereAll();
        var updateQuery = (EricksonLopez.SqlBuilder.UpdateQuery<User>)query;
        var finalQuery = updateQuery.AddNode(new EricksonLopez.SqlBuilder.Abstractions.Nodes.ReturningNode(System.Array.Empty<string>()));
        
        var result = compiler.Compile(finalQuery);
        
        result.Sql.Trim().Should().EndWith("RETURNING *");
    }

    [Fact]
    public void Compile_ReturningColumns_ShouldGenerateCorrectSql()
    {
        var compiler = new SqliteCompiler();
        var query = Sql.Update<User>().WhereAll();
        var updateQuery = (EricksonLopez.SqlBuilder.UpdateQuery<User>)query;
        var finalQuery = updateQuery.AddNode(new EricksonLopez.SqlBuilder.Abstractions.Nodes.ReturningNode(new[] { "Id", "Username" }));
        
        var result = compiler.Compile(finalQuery);
        
        result.Sql.Trim().Should().EndWith("RETURNING \"Id\", \"Username\"");
    }
}









