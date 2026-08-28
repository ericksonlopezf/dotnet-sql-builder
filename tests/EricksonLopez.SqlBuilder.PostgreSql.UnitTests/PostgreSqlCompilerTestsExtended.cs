// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Testing;
using EricksonLopez.SqlBuilder.Testing.DataBuilders;
using EricksonLopez.SqlBuilder.Testing.Domain;
using NSubstitute;
using Xunit;

namespace EricksonLopez.SqlBuilder.PostgreSql.Tests;

public class PostgreSqlCompilerTestsExtended
{
    [Fact]
    public Task Compile_WhenSelectWithTop_ShouldGenerateTopSyntax()
    {
        // Arrange
        var query = Sql.From<User>().Select("Id", "Name").Limit(10);
        
        // Act & Assert
        return query.VerifyQueryAsync(new PostgreSqlCompiler());
    }
    
    [Fact]
    public Task Compile_WhenSelectWithOffset_ShouldGenerateOffsetFetch()
    {
        // Arrange
        var query = Sql.From<User>().Select("Id").Offset(20).Limit(10);
        
        // Act & Assert
        return query.VerifyQueryAsync(new PostgreSqlCompiler());
    }
    
    [Fact]
    public Task Compile_WhenInsert_ShouldEscapeTableName()
    {
        // Arrange
        var query = Sql.Insert(ObjectMother.CreateUser());
        
        // Act & Assert
        return query.VerifyQueryAsync(new PostgreSqlCompiler());
    }

    [Fact]
    public Task Compile_WhenUpdate_ShouldEscapeTableName()
    {
        // Arrange
        var query = Sql.Update<User>().WhereAll();
        
        // Act & Assert
        return query.VerifyQueryAsync(new PostgreSqlCompiler());
    }

    [Fact]
    public Task Compile_WhenDelete_ShouldEscapeTableName()
    {
        // Arrange
        var query = Sql.Delete<User>().WhereAll();
        
        // Act & Assert
        return query.VerifyQueryAsync(new PostgreSqlCompiler());
    }

    [Fact]
    public Task Compile_WhenWhere_ShouldCompileSuccessfully()
    {
        // Arrange
        var query = Sql.From<User>().Select("*").Where($"Id = {1}").Or(u => u.FirstName == "Admin");
        
        // Act & Assert
        return query.VerifyQueryAsync(new PostgreSqlCompiler());
    }
    
    [Fact]
    public Task Compile_WhenDistinct_ShouldIncludeDistinctKeyword()
    {
        // Arrange
        var query = Sql.From<User>().Select("Name").Distinct();
        
        // Act & Assert
        return query.VerifyQueryAsync(new PostgreSqlCompiler());
    }
}






