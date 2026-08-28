// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Immutable;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Testing.DataBuilders;
using EricksonLopez.SqlBuilder.Testing.Domain;
using NSubstitute;
using Xunit;

namespace EricksonLopez.SqlBuilder.Oracle.Tests;

public class OracleCompilerTests
{
    [Fact]
    public void Compile_WhenSelectWithLimitOffset_ShouldGenerateCorrectSyntax()
    {
        // Arrange
        var query = Sql.From<TestEntity>().Select("Id", "Name").Offset(20).Limit(10);
        var compiler = new OracleCompiler();
        
        // Act
        var result = compiler.Compile(query);
        
        // Assert
        result.Sql.Trim().Should().Be("SELECT \"ID\", \"NAME\" FROM \"TESTENTITYS\" OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY");
    }
    
    [Fact]
    public void Compile_WhenSelectWithOnlyLimit_ShouldGenerateCorrectSyntax()
    {
        // Arrange
        var query = Sql.From<TestEntity>().Select("Id").Limit(10);
        var compiler = new OracleCompiler();
        
        // Act
        var result = compiler.Compile(query);
        
        // Assert
        result.Sql.Trim().Should().Be("SELECT \"ID\" FROM \"TESTENTITYS\" FETCH NEXT 10 ROWS ONLY");
    }

    [Fact]
    public void Compile_WhenSelectWithLimitOffset_Oracle11g_ShouldGenerateRownumSubquery()
    {
        // Arrange
        var query = Sql.From<TestEntity>().Select("Id", "Name").Offset(20).Limit(10);
        var compiler = new OracleCompiler(OracleDialectVersion.Oracle11g);
        
        // Act
        var result = compiler.Compile(query);
        
        // Assert
        result.Sql.Trim().Should().Be("SELECT * FROM (SELECT a_.*, ROWNUM rnum_ FROM (SELECT \"ID\", \"NAME\" FROM \"TESTENTITYS\") a_ WHERE ROWNUM <= 30) WHERE rnum_ > 20");
    }

    [Fact]
    public void Compile_WhenSelectWithOnlyLimit_Oracle11g_ShouldGenerateRownumSubquery()
    {
        // Arrange
        var query = Sql.From<TestEntity>().Select("Id").Limit(10);
        var compiler = new OracleCompiler(OracleDialectVersion.Oracle11g);
        
        // Act
        var result = compiler.Compile(query);
        
        // Assert
        result.Sql.Trim().Should().Be("SELECT * FROM (SELECT \"ID\" FROM \"TESTENTITYS\") WHERE ROWNUM <= 10");
    }

    [Fact]
    public void Compile_WhenSelectWithOnlyOffset_Oracle11g_ShouldGenerateRownumSubquery()
    {
        // Arrange
        var query = Sql.From<TestEntity>().Select("Id").Offset(20);
        var compiler = new OracleCompiler(OracleDialectVersion.Oracle11g);
        
        // Act
        var result = compiler.Compile(query);
        
        // Assert
        result.Sql.Trim().Should().Be("SELECT * FROM (SELECT a_.*, ROWNUM rnum_ FROM (SELECT \"ID\" FROM \"TESTENTITYS\") a_) WHERE rnum_ > 20");
    }
    
    [Fact]
    public void Compile_WhenInsert_ShouldEscapeTableName()
    {
        // Arrange
        var query = Sql.Insert(ObjectMother.CreateTestEntity());
        var compiler = new OracleCompiler();
        
        // Act
        var result = compiler.Compile(query);
        
        // Assert
        result.Sql.Trim().Should().Be("INSERT INTO \"TESTENTITYS\" (\"ID\", \"NAME\", \"IS_ACTIVE\") VALUES (:p0, :p1, :p2)");
    }

    [Fact]
    public void Compile_WhenUpdate_ShouldEscapeTableName()
    {
        // Arrange
        var query = Sql.Update<TestEntity>().WhereAll();
        var compiler = new OracleCompiler();
        
        // Act
        var result = compiler.Compile(query);
        
        // Assert
        result.Sql.Trim().Should().Be("UPDATE \"TESTENTITYS\"");
    }

    [Fact]
    public void Compile_WhenDelete_ShouldEscapeTableName()
    {
        // Arrange
        var query = Sql.Delete<TestEntity>().WhereAll();
        var compiler = new OracleCompiler();
        
        // Act
        var result = compiler.Compile(query);
        
        // Assert
        result.Sql.Trim().Should().Be("DELETE FROM \"TESTENTITYS\"");
    }

    [Fact]
    public void Compile_WhenWhere_ShouldCompileSuccessfully()
    {
        // Arrange
        var query = Sql.From<TestEntity>().Select("*").Where($"Id = {1}").Or(u => u.Name == "Admin");
        var compiler = new OracleCompiler();
        
        // Act
        var result = compiler.Compile(query);
        
        // Assert
        result.Sql.Trim().Should().Be("SELECT * FROM \"TESTENTITYS\" WHERE Id = :p0 OR (name = :p1)");
    }

    [Fact]
    public void Compile_OnConflictNode_ThrowsNotSupportedException()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[] 
        { 
            new InsertNode("users", System.Array.Empty<string>()),
            new OnConflictNode(System.Array.Empty<string>(), "DO NOTHING") 
        }.ToImmutableList());
        var compiler = new OracleCompiler();
        
        System.Action act = () => compiler.Compile((ISqlQuery)query);
        
        act.Should().Throw<System.NotSupportedException>()
           .WithMessage("Oracle does not support ON CONFLICT syntax. Use Sql.Raw() with a MERGE INTO statement instead.");
    }

    [Fact]
    public void Compile_ReturningNode_WithoutColumns_ThrowsNotSupportedException()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[] 
        { 
            new InsertNode("users", System.Array.Empty<string>()),
            new ReturningNode(System.Array.Empty<string>()) 
        }.ToImmutableList());
        var compiler = new OracleCompiler();
        
        System.Action act = () => compiler.Compile((ISqlQuery)query);
        
        act.Should().Throw<System.NotSupportedException>()
           .WithMessage("*Oracle RETURNING clause requires explicit column names*");
    }
}







