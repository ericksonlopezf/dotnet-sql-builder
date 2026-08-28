// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Dapper;
using EricksonLopez.SqlBuilder.Testing.Domain;
using Xunit;

namespace EricksonLopez.SqlBuilder.Testing.Infrastructure;

/// <summary>
/// Provides a reusable suite of CRUD integration tests for a given database fixture.
/// </summary>
/// <remarks>
/// Subclasses bind a concrete <typeparamref name="TFixture"/> implementation, allowing
/// the same test logic to run against multiple database backends.
/// </remarks>
/// <typeparam name="TFixture">The database fixture that creates connections for the target database.</typeparam>
[Collection("DatabaseCollection")]
public abstract class CrudTestsBase<TFixture> : IClassFixture<TFixture> where TFixture : DatabaseFixture
{
    /// <summary>Gets the database fixture that provides connections and compiler for these tests.</summary>
    protected readonly TFixture Fixture;

    /// <summary>
    /// Initializes a new instance of <see cref="CrudTestsBase{TFixture}"/> with the specified fixture.
    /// </summary>
    /// <param name="fixture">The database fixture injected by xUnit.</param>
    protected CrudTestsBase(TFixture fixture)
    {
        Fixture = fixture;
    }

    [Fact]
    public async Task Insert_NewCustomer_ShouldPersistToDatabase()
    {
        using var conn = Fixture.CreateConnection();
        var email = $"test_{Guid.NewGuid()}@example.com";
        var customer = new Customer
        {
            Name = "Integration Test User",
            Email = email,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var insert = Sql.Insert(customer);
        await conn.ExecuteAsync(insert);

        var select = Sql.From<Customer>().Where(c => c.Email == email);
        var result = await conn.QueryFirstOrDefaultAotAsync(select, Customer.GetReaderParser());

        result.Should().NotBeNull();
        result!.Name.Should().Be("Integration Test User");
        result.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Update_CustomerName_ShouldPersistChange()
    {
        using var conn = Fixture.CreateConnection();
        var email = $"update_{Guid.NewGuid()}@example.com";
        var customer = new Customer
        {
            Name = "Old Name",
            Email = email,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await conn.ExecuteAsync(Sql.Insert(customer));

        var update = Sql.Update<Customer>()
            .Set(c => c.Name, "New Name")
            .Where(c => c.Email == email);
        await conn.ExecuteAsync(update);

        var select = Sql.From<Customer>().Where(c => c.Email == email);
        var result = await conn.QueryFirstOrDefaultAotAsync(select, Customer.GetReaderParser());

        result.Should().NotBeNull();
        result!.Name.Should().Be("New Name");
    }

    [Fact]
    public async Task Delete_Customer_ShouldRemoveFromDatabase()
    {
        using var conn = Fixture.CreateConnection();
        var email = $"delete_{Guid.NewGuid()}@example.com";
        var customer = new Customer
        {
            Name = "To Delete",
            Email = email,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await conn.ExecuteAsync(Sql.Insert(customer));

        var delete = Sql.Delete<Customer>().Where(c => c.Email == email);
        await conn.ExecuteAsync(delete);

        var select = Sql.From<Customer>().Where(c => c.Email == email);
        var result = await conn.QueryFirstOrDefaultAotAsync(select, Customer.GetReaderParser());

        result.Should().BeNull();
    }

    [Fact]
    public async Task Select_CountAggregate_ShouldReturnExpectedRecords()
    {
        using var conn = Fixture.CreateConnection();
        var query = Sql.From<Customer>();
        var compiler = Fixture.CreateCompiler();
        var compiled = compiler.Compile(query);
        
        var count = await global::Dapper.SqlMapper.QuerySingleAsync<int>(conn, $"SELECT COUNT(*) FROM ({compiled.Sql}) t", compiled.Parameters);
        count.Should().BeGreaterThan(0);
    }
}





