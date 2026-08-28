// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.PostgreSql;
using Npgsql;
using NpgsqlTypes;
using VerifyXunit;
using Xunit;

namespace EricksonLopez.SqlBuilder.PostgreSql.UnitTests;

public class BulkParametersTests
{
    private record Product(Guid Id, string Name, decimal Price);

    // --- BulkParameters.From ---

    [Fact]
    public void From_WithNullItems_ThrowsArgumentNullException()
    {
        IEnumerable<Product> items = null!;
        var act = () => BulkParameters.From(items);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void From_WithItems_ReturnsBuilderWithCorrectCount()
    {
        var items = new[] { new Product(Guid.NewGuid(), "A", 1m) };
        var builder = BulkParameters.From(items);
        builder.Count.Should().Be(1);
    }

    [Fact]
    public void From_WithEmptyCollection_CountIsZero()
    {
        var builder = BulkParameters.From(Array.Empty<Product>());
        builder.Count.Should().Be(0);
    }

    // --- BulkParameters<T>.Add ---

    [Fact]
    public void Add_WithNullOrWhiteSpaceParameterName_ThrowsArgumentException()
    {
        var builder = BulkParameters.From(new[] { new Product(Guid.NewGuid(), "A", 1m) });
        var act = () => builder.Add("   ", p => p.Name, NpgsqlDbType.Text);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Add_WithNullSelector_ThrowsArgumentNullException()
    {
        var builder = BulkParameters.From(new[] { new Product(Guid.NewGuid(), "A", 1m) });
        Func<Product, string> selector = null!;
        var act = () => builder.Add("Names", selector, NpgsqlDbType.Text);
        act.Should().Throw<ArgumentNullException>().WithParameterName("selector");
    }

    [Fact]
    public void Add_ReturnsSameBuilderInstance_ForChaining()
    {
        var items = new[] { new Product(Guid.NewGuid(), "A", 1m) };
        var builder = BulkParameters.From(items);
        var result = builder.Add("Names", p => p.Name, NpgsqlDbType.Text);
        result.Should().BeSameAs(builder);
    }

    // --- BulkParameters<T>.Build ---

    [Fact]
    public void Build_WithNoColumnsAdded_ThrowsInvalidOperationException()
    {
        var builder = BulkParameters.From(new[] { new Product(Guid.NewGuid(), "A", 1m) });
        var act = () => builder.Build();
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*At least one column*");
    }

    [Fact]
    public void Build_WithSingleColumn_ReturnsOneParameter()
    {
        var id = Guid.NewGuid();
        var items = new[] { new Product(id, "Widget", 9.99m) };
        var parameters = BulkParameters.From(items)
            .Add("Ids", p => p.Id, NpgsqlDbType.Uuid)
            .Build();

        parameters.Should().HaveCount(1);
        parameters[0].ParameterName.Should().Be("Ids");
        parameters[0].NpgsqlDbType.Should().Be(NpgsqlDbType.Uuid | NpgsqlDbType.Array);
        var values = (Guid[])parameters[0].Value!;
        values.Should().ContainSingle().Which.Should().Be(id);
    }

    [Fact]
    public void Build_WithMultipleColumns_ReturnsCorrectParameterCount()
    {
        var items = new[]
        {
            new Product(Guid.NewGuid(), "Alpha", 1.5m),
            new Product(Guid.NewGuid(), "Beta",  2.5m),
        };

        var parameters = BulkParameters.From(items)
            .Add("Ids",    p => p.Id,    NpgsqlDbType.Uuid)
            .Add("Names",  p => p.Name,  NpgsqlDbType.Text)
            .Add("Prices", p => p.Price, NpgsqlDbType.Numeric)
            .Build();

        parameters.Should().HaveCount(3);
        parameters[0].ParameterName.Should().Be("Ids");
        parameters[1].ParameterName.Should().Be("Names");
        parameters[2].ParameterName.Should().Be("Prices");
    }

    [Fact]
    public void Build_ValuesArrayHasCorrectLength()
    {
        var items = Enumerable.Range(1, 5)
            .Select(i => new Product(Guid.NewGuid(), $"Item {i}", i * 1.1m))
            .ToList();

        var parameters = BulkParameters.From(items)
            .Add("Names", p => p.Name, NpgsqlDbType.Text)
            .Build();

        var values = (string[])parameters[0].Value!;
        values.Should().HaveCount(5);
        values[0].Should().Be("Item 1");
        values[4].Should().Be("Item 5");
    }

    [Fact]
    public void Build_DbTypeIsArrayCombinedWithElementType()
    {
        var items = new[] { new Product(Guid.NewGuid(), "A", 1m) };
        var parameters = BulkParameters.From(items)
            .Add("Prices", p => p.Price, NpgsqlDbType.Numeric)
            .Build();

        parameters[0].NpgsqlDbType.Should().Be(NpgsqlDbType.Numeric | NpgsqlDbType.Array);
    }

    // --- BulkInsertAsync / BulkUpsertAsync guard clauses ---

    [Fact]
    public async Task BulkInsertAsync_WithNullConnection_ThrowsArgumentNullException()
    {
        System.Data.IDbConnection conn = null!;
        var act = () => conn.BulkInsertAsync("SELECT 1", Array.Empty<NpgsqlParameter>());
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task BulkInsertAsync_WithNullSql_ThrowsArgumentException()
    {
        var conn = new NpgsqlConnection();
        var act = () => conn.BulkInsertAsync(null!, Array.Empty<NpgsqlParameter>());
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task BulkInsertAsync_WithEmptyParameters_ReturnsZero()
    {
        // Non-Npgsql connection to avoid needing a real server
        var conn = NSubstitute.Substitute.For<System.Data.IDbConnection>();
        // Empty params bypasses cast check
        var result = await conn.BulkInsertAsync("SELECT 1", Array.Empty<NpgsqlParameter>());
        result.Should().Be(0);
    }

    [Fact]
    public async Task BulkInsertAsync_WithNonNpgsqlConnection_ThrowsArgumentException()
    {
        var conn = NSubstitute.Substitute.For<System.Data.IDbConnection>();
        var parameters = BulkParameters.From(new[] { new Product(Guid.NewGuid(), "A", 1m) })
            .Add("Names", p => p.Name, NpgsqlDbType.Text)
            .Build();

        var act = () => conn.BulkInsertAsync("INSERT INTO t SELECT * FROM UNNEST(@Names)", parameters);
        await act.Should().ThrowAsync<ArgumentException>()
                 .WithMessage("*NpgsqlConnection*");
    }

    [Fact]
    public async Task BulkUpsertAsync_DelegatesToBulkInsertAsync_WithEmptyParams()
    {
        var conn = NSubstitute.Substitute.For<System.Data.IDbConnection>();
        // Empty params → returns 0 without reaching cast
        var result = await conn.BulkUpsertAsync("SELECT 1", Array.Empty<NpgsqlParameter>());
        result.Should().Be(0);
    }

    [Fact]
    public async Task BulkUpsertAsync_WithNonNpgsqlConnection_ThrowsArgumentException()
    {
        var conn = NSubstitute.Substitute.For<System.Data.IDbConnection>();
        var parameters = BulkParameters.From(new[] { new Product(Guid.NewGuid(), "A", 1m) })
            .Add("Names", p => p.Name, NpgsqlDbType.Text)
            .Build();

        var act = () => conn.BulkUpsertAsync("INSERT INTO t SELECT * FROM UNNEST(@Names) ON CONFLICT DO NOTHING", parameters);
        await act.Should().ThrowAsync<ArgumentException>();
    }
}







