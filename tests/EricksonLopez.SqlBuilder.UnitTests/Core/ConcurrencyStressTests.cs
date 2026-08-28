// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.MySql;
using EricksonLopez.SqlBuilder.Oracle;
using EricksonLopez.SqlBuilder.PostgreSql;
using EricksonLopez.SqlBuilder.Sqlite;
using EricksonLopez.SqlBuilder.SqlServer;
using EricksonLopez.SqlBuilder.Testing;
using EricksonLopez.SqlBuilder.Testing.Domain;
using System.Threading.Tasks;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests.Core;

[Trait("Category", "Stress")]
[Collection("SqlEntityCache")]
public class ConcurrencyStressTests
{
    [Fact]
    public void SqlEntityCache_GetMetadata_ConcurrentAccess_IsThreadSafe()
    {
        // Arrange
        const int iterations = 200;
        var exceptions = new ConcurrentBag<Exception>();

        // Act
        Parallel.For(0, iterations, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 4 }, i =>
        {
            try
            {
                SqlEntityCache<User>.TableName.Should().Be("users");
                SqlEntityCache<User>.PropertyMap.Should().NotBeEmpty();
                SqlEntityCache<User>.ColumnNames.Should().NotBeEmpty();

                SqlEntityCache<Product>.TableName.Should().Be("products");
                SqlEntityCache<Product>.PropertyMap.Should().NotBeEmpty();
                SqlEntityCache<Product>.ColumnNames.Should().NotBeEmpty();

                SqlEntityCache<DummyEntity>.TableName.Should().Be("dummy_entity");
                SqlEntityCache<DummyEntity>.PropertyMap.Should().NotBeEmpty();
                SqlEntityCache<DummyEntity>.ColumnNames.Should().NotBeEmpty();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        // Assert
        exceptions.Should().BeEmpty();
    }

    [Fact]
    public void QueryCompilation_ConcurrentMultiDialect_IsThreadSafe()
    {
        // Arrange
        const int iterations = 200;
        var compilers = new ISqlCompiler[]
        {
            new SqlServerCompiler(),
            new PostgreSqlCompiler(),
            new MySqlCompiler(),
            new SqliteCompiler(),
            new OracleCompiler()
        };

        var exceptions = new ConcurrentBag<Exception>();
        var results = new ConcurrentBag<string>();

        // Act
        Parallel.For(0, iterations, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 4 }, i =>
        {
            try
            {
                var compiler = compilers[i % compilers.Length];
                var query = Sql.From<User>()
                    .Where(u => u.IsActive && u.Email.Contains("@test.com"))
                    .OrderBy(u => u.CreatedAt)
                    .Limit(10)
                    .Offset(20);

                var compiled = compiler.Compile(query);
                compiled.Sql.Should().NotBeNullOrWhiteSpace();
                compiled.Parameters.Should().NotBeEmpty();
                results.Add(compiled.Sql);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        // Assert
        exceptions.Should().BeEmpty();
        results.Count.Should().Be(iterations);
    }

    [Fact]
    public void QueryBuilding_ImmutabilityUnderContention_DoesNotMutateBaseQuery()
    {
        // Arrange
        const int iterations = 100;
        var baseQuery = Sql.From<User>().Where(u => u.IsActive);
        var initialNodeCount = baseQuery.Nodes.Length;
        var compiler = new SqlServerCompiler();
        var baseCompiled = compiler.Compile(baseQuery);

        var exceptions = new ConcurrentBag<Exception>();

        // Act
        Parallel.For(0, iterations, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 4 }, i =>
        {
            try
            {
                var branchedQuery = baseQuery
                    .Where(u => u.Id > i)
                    .OrderBy(u => u.Username);

                var compiledBranch = compiler.Compile(branchedQuery);
                compiledBranch.Sql.Should().NotBeNullOrWhiteSpace();
                compiledBranch.Parameters.Should().NotBeEmpty();
                branchedQuery.Nodes.Length.Should().BeGreaterThan(initialNodeCount);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        // Assert
        exceptions.Should().BeEmpty();
        baseQuery.Nodes.Length.Should().Be(initialNodeCount);
        compiler.Compile(baseQuery).Sql.Should().Be(baseCompiled.Sql);
    }

    [Fact]
    public void CompilationContext_StringBuilderPool_ConcurrentBorrowAndReturn_DoesNotLeakOrCorrupt()
    {
        // Arrange
        const int iterations = 300;
        var exceptions = new ConcurrentBag<Exception>();

        // Act
        Parallel.For(0, iterations, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 4 }, i =>
        {
            try
            {
                var expectedPayload = $"SELECT col_{i} FROM table_{i} WHERE id = {i}";
                using var context = new CompilationContext(new ParameterManager());
                context.Sql.Append(expectedPayload);
                context.Sql.ToString().Should().Be(expectedPayload);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        // Assert
        exceptions.Should().BeEmpty();
    }
}



