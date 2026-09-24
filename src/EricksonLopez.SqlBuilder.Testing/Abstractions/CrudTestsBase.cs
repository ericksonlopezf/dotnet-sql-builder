// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Testing.Domain;
using EricksonLopez.SqlBuilder.Testing.Infrastructure;
using Xunit;

namespace EricksonLopez.SqlBuilder.Testing.Abstractions;

/// <summary>
/// Provides an abstract suite of database-agnostic CRUD integration tests.
/// </summary>
/// <remarks>
/// Exercises query compilation and execution against real database fixtures across SELECT, INSERT, UPDATE, DELETE, transactions, and aggregates.
/// </remarks>
/// <typeparam name="TFixture">The database fixture type used to obtain connections and compilers.</typeparam>
public abstract class CrudTestsBase<TFixture> where TFixture : DatabaseFixture
{
    /// <summary>
    /// Gets the database fixture providing connections and compiler for these tests.
    /// </summary>
    protected readonly TFixture Fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="CrudTestsBase{TFixture}"/> class with the specified fixture.
    /// </summary>
    /// <param name="fixture">The database fixture providing database connections and compilers.</param>
    protected CrudTestsBase(TFixture fixture)
    {
        Fixture = fixture;
    }

    // ─── Helper: execute a query and return results ────────────────────────────

    private static async Task<IEnumerable<T>> QueryAsync<T>(
        IDbConnection conn, ISqlQuery query, ISqlCompiler compiler, IDbTransaction? tx = null)
    {
        var result = query.Build(compiler);
        var dp = new DynamicParameters();
        foreach (var p in result.Parameters)
        {
            dp.Add(p.Key, p.Value);
        }

        return await conn.QueryAsync<T>(result.Sql, dp, tx);
    }

    private static async Task<T?> QuerySingleOrDefaultAsync<T>(
        IDbConnection conn, ISqlQuery query, ISqlCompiler compiler, IDbTransaction? tx = null)
    {
        var result = query.Build(compiler);
        var dp = new DynamicParameters();
        foreach (var p in result.Parameters)
        {
            dp.Add(p.Key, p.Value);
        }

        return await conn.QuerySingleOrDefaultAsync<T>(result.Sql, dp, tx);
    }

    private static async Task<T> QuerySingleAsync<T>(
        IDbConnection conn, ISqlQuery query, ISqlCompiler compiler, IDbTransaction? tx = null)
    {
        var result = query.Build(compiler);
        var dp = new DynamicParameters();
        foreach (var p in result.Parameters)
        {
            dp.Add(p.Key, p.Value);
        }

        return await conn.QuerySingleAsync<T>(result.Sql, dp, tx);
    }

    private static async Task<int> ExecuteAsync(
        IDbConnection conn, ISqlQuery query, ISqlCompiler compiler, IDbTransaction? tx = null)
    {
        var result = query.Build(compiler);
        var dp = new DynamicParameters();
        foreach (var p in result.Parameters)
        {
            dp.Add(p.Key, p.Value);
        }

        return await conn.ExecuteAsync(result.Sql, dp, tx);
    }

    // ─── SELECT tests ─────────────────────────────────────────────────────────

    /// <summary>Verifies that selecting all customers returns all seeded customer records.</summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Select_AllCustomers_ShouldReturn100Records()
    {
        using var conn = Fixture.CreateConnection();
        var compiler = Fixture.CreateCompiler();

        var query = Sql.From<Customer>();
        var customers = await QueryAsync<Customer>(conn, query, compiler);

        Assert.Equal(100, customers.Count());
    }

    /// <summary>Verifies that filtering customers by active status returns only active records.</summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Select_ActiveCustomers_ShouldReturnSubset()
    {
        using var conn = Fixture.CreateConnection();
        var compiler = Fixture.CreateCompiler();

        var query = Sql.From<Customer>().Where(c => c.IsActive);
        var customers = await QueryAsync<Customer>(conn, query, compiler);

        Assert.NotEmpty(customers);
        Assert.All(customers, c => Assert.True(c.IsActive));
    }

    /// <summary>Verifies that querying a customer by primary key returns exactly one matching record.</summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Select_CustomerById_ShouldReturnExactlyOne()
    {
        using var conn = Fixture.CreateConnection();
        var compiler = Fixture.CreateCompiler();

        var expectedId = Fixture.Data.Customers.First().Id;
        var query = Sql.From<Customer>().Where(c => c.Id == expectedId);
        var customer = await QuerySingleOrDefaultAsync<Customer>(conn, query, compiler);

        Assert.NotNull(customer);
        Assert.Equal(expectedId, customer!.Id);
    }

    /// <summary>Verifies that querying a non-existent customer by identifier returns <see langword="null"/>.</summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Select_NonExistentCustomer_ShouldReturnNull()
    {
        using var conn = Fixture.CreateConnection();
        var compiler = Fixture.CreateCompiler();

        var query = Sql.From<Customer>().Where(c => c.Id == int.MaxValue);
        var customer = await QuerySingleOrDefaultAsync<Customer>(conn, query, compiler);

        Assert.Null(customer);
    }

    /// <summary>Verifies that paginating customer queries with limit and offset returns the expected page size.</summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Select_Paginated_ShouldReturnCorrectPage()
    {
        using var conn = Fixture.CreateConnection();
        var compiler = Fixture.CreateCompiler();

        const int pageSize = 10;
        const int page     = 2;

        var query = Sql.From<Customer>()
            .OrderBy(c => c.Id)
            .Limit(pageSize)
            .Offset((page - 1) * pageSize);

        var customers = (await QueryAsync<Customer>(conn, query, compiler)).ToList();

        Assert.Equal(pageSize, customers.Count);
    }

    /// <summary>Verifies that ordering customers by name descending produces sorted results.</summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Select_OrderedByNameDescending_ShouldReturnSortedResults()
    {
        using var conn = Fixture.CreateConnection();
        var compiler = Fixture.CreateCompiler();

        var query = Sql.From<Customer>()
            .OrderByDescending(c => c.Name)
            .Limit(10);

        var customers = (await QueryAsync<Customer>(conn, query, compiler)).ToList();

        Assert.NotEmpty(customers);
        for (int i = 1; i < customers.Count; i++)
        {
            Assert.True(
                string.Compare(customers[i - 1].Name, customers[i].Name, StringComparison.OrdinalIgnoreCase) >= 0,
                $"Expected '{customers[i - 1].Name}' >= '{customers[i].Name}'");
        }
    }

    /// <summary>Verifies that selecting all products returns all seeded product records.</summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Select_AllProducts_ShouldReturn500Records()
    {
        using var conn = Fixture.CreateConnection();
        var compiler = Fixture.CreateCompiler();

        var query = Sql.From<Product>();
        var products = await QueryAsync<Product>(conn, query, compiler);

        Assert.Equal(500, products.Count());
    }

    /// <summary>Verifies that filtering products above a minimum price returns only matching products.</summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Select_ProductsAboveMinPrice_ShouldFilterCorrectly()
    {
        using var conn = Fixture.CreateConnection();
        var compiler = Fixture.CreateCompiler();

        var minPrice = 100m;
        var query = Sql.From<Product>().Where($"price >= {minPrice}");
        var products = await QueryAsync<Product>(conn, query, compiler);

        Assert.NotEmpty(products);
        Assert.All(products, p => Assert.True(p.Price >= minPrice));
    }

    /// <summary>Verifies that querying non-deleted orders excludes soft-deleted records.</summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Select_NonDeletedOrders_ShouldReturnMostOrders()
    {
        using var conn = Fixture.CreateConnection();
        var compiler = Fixture.CreateCompiler();

        var query = Sql.From<Order>().Where(o => !o.IsDeleted);
        var orders = await QueryAsync<Order>(conn, query, compiler);

        Assert.True(orders.Count() >= 900,
            $"Expected ~1000 non-deleted orders, got {orders.Count()}");
    }

    /// <summary>Verifies that filtering orders by customer identifier returns the exact count of customer orders.</summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Select_OrdersByCustomer_ShouldReturnExactCount()
    {
        using var conn = Fixture.CreateConnection();
        var compiler = Fixture.CreateCompiler();

        var customer = Fixture.Data.Customers.First();
        var expectedCount = Fixture.Data.Orders.Count(o => o.CustomerId == customer.Id && !o.IsDeleted);

        var query = Sql.From<Order>()
            .Where(o => o.CustomerId == customer.Id)
            .Where(o => !o.IsDeleted);

        var orders = await QueryAsync<Order>(conn, query, compiler);

        Assert.Equal(expectedCount, orders.Count());
    }

    /// <summary>Verifies that selecting order items returns at least one thousand seeded records.</summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Select_OrderItems_ShouldHaveAtLeast1000Records()
    {
        using var conn = Fixture.CreateConnection();
        var compiler = Fixture.CreateCompiler();

        var query = Sql.From<OrderItem>();
        var items = await QueryAsync<OrderItem>(conn, query, compiler);

        Assert.True(items.Count() >= 1000,
            $"Expected 1000+ order items, got {items.Count()}");
    }

    /// <summary>Verifies that selecting specific columns returns partial entity projections.</summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Select_SpecificColumns_ShouldReturnPartialData()
    {
        using var conn = Fixture.CreateConnection();
        var compiler = Fixture.CreateCompiler();

        var query = Sql.From<Customer>().Select("id", "name").Limit(5);
        var result = await QueryAsync<dynamic>(conn, query, compiler);

        Assert.Equal(5, result.Count());
    }

    /// <summary>Verifies that selecting distinct column values returns deduplicated results.</summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Select_Distinct_ShouldReturnUniqueStatuses()
    {
        using var conn = Fixture.CreateConnection();
        var compiler = Fixture.CreateCompiler();

        var query = Sql.From<Order>().Select("status").Distinct();
        var statuses = (await QueryAsync<string>(conn, query, compiler)).ToList();

        Assert.True(statuses.Count <= 5);
        Assert.Equal(statuses.Count, statuses.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    /// <summary>Verifies that calculating a COUNT aggregate returns at least one hundred customer rows.</summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Select_CountAggregate_ShouldReturnAtLeast100()
    {
        using var conn = Fixture.CreateConnection();
        var compiler = Fixture.CreateCompiler();

        // Use raw SQL count to avoid engine-specific column aliasing issues
        var sql = Sql.From<Customer>().RawSelect($"COUNT(*) AS total_count");
        var compiled = sql.Build(compiler);
        var dp = new DynamicParameters();
        foreach (var p in compiled.Parameters)
        {
            dp.Add(p.Key, p.Value);
        }

        var row = await conn.QuerySingleAsync<dynamic>(compiled.Sql, dp);
        // Oracle uses uppercase column names — handle both
        long count = TryGetLong(row, "total_count", "TOTAL_COUNT");
        Assert.True(count >= 100, $"Expected >= 100 customers, got {count}");
    }

    /// <summary>Verifies that inner joining orders with customers produces joined result rows.</summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Select_InnerJoin_OrdersWithCustomers_ShouldReturnJoinedRows()
    {
        using var conn = Fixture.CreateConnection();
        var compiler = Fixture.CreateCompiler();

        var query = Sql.From<Order>()
            .InnerJoin("customers", "c", "c.id = orders.customer_id")
            .Select("orders.id", "orders.status")
            .Where(o => !o.IsDeleted)
            .Limit(10);

        var result = await QueryAsync<dynamic>(conn, query, compiler);

        Assert.Equal(10, result.Count());
    }

    // ─── INSERT tests ─────────────────────────────────────────────────────────

    /// <summary>Verifies that inserting a new customer persists the record into the database.</summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Insert_NewCustomer_ShouldPersistToDatabase()
    {
        using var conn = Fixture.CreateConnection();
        var compiler = Fixture.CreateCompiler();

        var uniqueEmail = $"test_{Guid.NewGuid():N}@integration.test";

        var insertQuery = new InsertQuery<Customer>()
            .Into("customers")
            .Values(uniqueEmail + " Corp", uniqueEmail, null, null, 1);

        await ExecuteAsync(conn, insertQuery, compiler);

        var selectQuery = Sql.From<Customer>().Where($"email = {uniqueEmail}");
        var inserted = await QuerySingleOrDefaultAsync<Customer>(conn, selectQuery, compiler);

        Assert.NotNull(inserted);
        Assert.Equal(uniqueEmail, inserted!.Email);
    }

    /// <summary>Verifies that batch inserting multiple customers persists all inserted records.</summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Insert_MultipleCustomers_ShouldAllPersist()
    {
        using var conn = Fixture.CreateConnection();
        var compiler = Fixture.CreateCompiler();

        var suffix = Guid.NewGuid().ToString("N")[..8];

        for (int i = 1; i <= 5; i++)
        {
            var email = $"batch{i}_{suffix}@test.com";
            var insertQuery = new InsertQuery<Customer>()
                .Into("customers")
                .Values($"Batch {i}", email, null, null, 1);
            await ExecuteAsync(conn, insertQuery, compiler);
        }

        // Verify all 5 were inserted using LIKE search
        var selectQuery = Sql.From<Customer>()
            .Where($"email LIKE {"batch%_" + suffix + "@test.com"}");
        var inserted = await QueryAsync<Customer>(conn, selectQuery, compiler);

        Assert.Equal(5, inserted.Count());
    }

    // ─── UPDATE tests ─────────────────────────────────────────────────────────

    /// <summary>Verifies that updating a customer name persists the modified value in the database.</summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Update_CustomerName_ShouldPersistChange()
    {
        using var conn = Fixture.CreateConnection();
        var compiler = Fixture.CreateCompiler();

        var uniqueEmail = $"update_{Guid.NewGuid():N}@test.com";
        await ExecuteAsync(conn,
            new InsertQuery<Customer>().Into("customers").Values("Original Name", uniqueEmail, null, null, 1),
            compiler);

        var selectQuery = Sql.From<Customer>().Where($"email = {uniqueEmail}");
        var inserted = await QuerySingleAsync<Customer>(conn, selectQuery, compiler);

        const string newName = "Updated Corp Name";
        await ExecuteAsync(conn,
            Sql.Update<Customer>().Set(c => c.Name, newName).Where(c => c.Id == inserted.Id),
            compiler);

        var updated = await QuerySingleAsync<Customer>(conn, selectQuery, compiler);
        Assert.Equal(newName, updated.Name);
    }

    /// <summary>Verifies that deactivating a customer updates the active flag to false.</summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Update_DeactivateCustomer_ShouldSetIsActiveFalse()
    {
        using var conn = Fixture.CreateConnection();
        var compiler = Fixture.CreateCompiler();

        var uniqueEmail = $"deact_{Guid.NewGuid():N}@test.com";
        await ExecuteAsync(conn,
            new InsertQuery<Customer>().Into("customers").Values("Deactivation Test", uniqueEmail, null, null, 1),
            compiler);

        var selectQuery = Sql.From<Customer>().Where($"email = {uniqueEmail}");
        var inserted = await QuerySingleAsync<Customer>(conn, selectQuery, compiler);

        await ExecuteAsync(conn,
            Sql.Update<Customer>().Set(c => c.IsActive, false).Where(c => c.Id == inserted.Id),
            compiler);

        var updated = await QuerySingleAsync<Customer>(conn, selectQuery, compiler);
        Assert.False(updated.IsActive);
    }

    // ─── DELETE tests ─────────────────────────────────────────────────────────

    /// <summary>Verifies that deleting a customer removes the record from the database.</summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Delete_Customer_ShouldRemoveFromDatabase()
    {
        using var conn = Fixture.CreateConnection();
        var compiler = Fixture.CreateCompiler();

        var uniqueEmail = $"del_{Guid.NewGuid():N}@test.com";
        await ExecuteAsync(conn,
            new InsertQuery<Customer>().Into("customers").Values("To Delete", uniqueEmail, null, null, 1),
            compiler);

        var selectQuery = Sql.From<Customer>().Where($"email = {uniqueEmail}");
        var inserted = await QuerySingleAsync<Customer>(conn, selectQuery, compiler);

        await ExecuteAsync(conn,
            Sql.Delete<Customer>().Where(c => c.Id == inserted.Id),
            compiler);

        var afterDelete = await QuerySingleOrDefaultAsync<Customer>(conn, selectQuery, compiler);
        Assert.Null(afterDelete);
    }

    /// <summary>Verifies that soft deleting an order sets the deletion flag.</summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SoftDelete_Order_ShouldSetIsDeletedFlag()
    {
        using var conn = Fixture.CreateConnection();
        var compiler = Fixture.CreateCompiler();

        var order = Fixture.Data.Orders.First(o => !o.IsDeleted);

        await ExecuteAsync(conn,
            Sql.Update<Order>().Set(o => o.IsDeleted, true).Where(o => o.Id == order.Id),
            compiler);

        var updated = await QuerySingleAsync<Order>(conn,
            Sql.From<Order>().Where(o => o.Id == order.Id), compiler);
        Assert.True(updated.IsDeleted);

        // Restore
        await ExecuteAsync(conn,
            Sql.Update<Order>().Set(o => o.IsDeleted, false).Where(o => o.Id == order.Id),
            compiler);
    }

    // ─── TRANSACTION tests ────────────────────────────────────────────────────

    /// <summary>Verifies that committed database transactions persist inserted changes.</summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    /// <exception cref="InvalidOperationException">The connection provided by the fixture is not a <see cref="System.Data.Common.DbConnection"/></exception>
    [Fact]
    public async Task Transaction_Commit_ShouldPersistChanges()
    {
        using var conn = Fixture.CreateConnection() as System.Data.Common.DbConnection
            ?? throw new InvalidOperationException("Connection must be DbConnection");

        if (conn.State == ConnectionState.Closed)
        {
            await conn.OpenAsync();
        }

        var compiler = Fixture.CreateCompiler();

        var uniqueEmail = $"tx_commit_{Guid.NewGuid():N}@test.com";

        await using (var tx = await conn.BeginTransactionAsync())
        {
            await ExecuteAsync(conn,
                new InsertQuery<Customer>().Into("customers")
                    .Values("Tx Commit Test", uniqueEmail, null, null, 1),
                compiler, tx);
            await tx.CommitAsync();
        }

        using var conn2 = Fixture.CreateConnection();
        var found = await QuerySingleOrDefaultAsync<Customer>(conn2,
            Sql.From<Customer>().Where($"email = {uniqueEmail}"), compiler);

        Assert.NotNull(found);
    }

    /// <summary>Verifies that rolled back database transactions discard pending changes.</summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    /// <exception cref="InvalidOperationException">The connection provided by the fixture is not a <see cref="System.Data.Common.DbConnection"/></exception>
    [Fact]
    public async Task Transaction_Rollback_ShouldDiscardChanges()
    {
        using var conn = Fixture.CreateConnection() as System.Data.Common.DbConnection
            ?? throw new InvalidOperationException("Connection must be DbConnection");

        if (conn.State == ConnectionState.Closed)
        {
            await conn.OpenAsync();
        }

        var compiler = Fixture.CreateCompiler();

        var uniqueEmail = $"tx_rollback_{Guid.NewGuid():N}@test.com";

        await using (var tx = await conn.BeginTransactionAsync())
        {
            await ExecuteAsync(conn,
                new InsertQuery<Customer>().Into("customers")
                    .Values("Tx Rollback Test", uniqueEmail, null, null, 1),
                compiler, tx);
            await tx.RollbackAsync();
        }

        using var conn2 = Fixture.CreateConnection();
        var notFound = await QuerySingleOrDefaultAsync<Customer>(conn2,
            Sql.From<Customer>().Where($"email = {uniqueEmail}"), compiler);

        Assert.Null(notFound);
    }

    // ─── EDGE CASES ───────────────────────────────────────────────────────────

    /// <summary>Verifies that querying with a non-matching predicate returns an empty collection.</summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Select_EmptyResult_ShouldReturnEmptyList()
    {
        using var conn = Fixture.CreateConnection();
        var compiler = Fixture.CreateCompiler();

        var query = Sql.From<Customer>().Where($"email = {"__non_existent__@test.dev"}");
        var result = await QueryAsync<Customer>(conn, query, compiler);

        Assert.Empty(result);
    }

    /// <summary>Verifies that querying nullable columns handles null values without errors.</summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Select_NullablePhone_ShouldHandleNullsWithoutException()
    {
        using var conn = Fixture.CreateConnection();
        var compiler = Fixture.CreateCompiler();

        var query = Sql.From<Customer>()
            .Where($"phone IS NULL")
            .Limit(5);

        var customers = await QueryAsync<Customer>(conn, query, compiler);

        Assert.All(customers, c => Assert.Null(c.Phone));
    }

    /// <summary>Verifies that grouping rows by status computes correct aggregate counts per group.</summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Select_GroupByStatus_ShouldReturnGroupCounts()
    {
        using var conn = Fixture.CreateConnection();
        var compiler = Fixture.CreateCompiler();

        var query = Sql.From<Order>()
            .Select("status")
            .RawSelect($"COUNT(*) AS order_count")
            .GroupBy("status")
            .Where(o => !o.IsDeleted);

        var compiled = query.Build(compiler);
        var dp = new DynamicParameters();
        foreach (var p in compiled.Parameters)
        {
            dp.Add(p.Key, p.Value);
        }

        var groups = (await conn.QueryAsync<dynamic>(compiled.Sql, dp)).ToList();

        Assert.NotEmpty(groups);
        foreach (var g in groups)
        {
            long cnt = TryGetLong(g, "order_count", "ORDER_COUNT");
            Assert.True(cnt > 0);
        }
    }

    /// <summary>Verifies that calculating the MAX price aggregate produces a positive value.</summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Select_MaxProductPrice_ShouldBePositive()
    {
        using var conn = Fixture.CreateConnection();
        var compiler = Fixture.CreateCompiler();

        var query = Sql.From<Product>().RawSelect($"MAX(price) AS max_price");
        var compiled = query.Build(compiler);
        var dp = new DynamicParameters();
        foreach (var p in compiled.Parameters)
        {
            dp.Add(p.Key, p.Value);
        }

        var row = await conn.QuerySingleAsync<dynamic>(compiled.Sql, dp);
        decimal maxPrice = TryGetDecimal(row, "max_price", "MAX_PRICE");

        Assert.True(maxPrice > 0, $"MAX(price) = {maxPrice}");
    }

    /// <summary>Verifies that calculating the SUM of order total amounts produces a positive total.</summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Select_SumOrderRevenue_ShouldBePositive()
    {
        using var conn = Fixture.CreateConnection();
        var compiler = Fixture.CreateCompiler();

        var query = Sql.From<Order>()
            .RawSelect($"SUM(total_amount) AS total_revenue")
            .Where(o => !o.IsDeleted);

        var compiled = query.Build(compiler);
        var dp = new DynamicParameters();
        foreach (var p in compiled.Parameters)
        {
            dp.Add(p.Key, p.Value);
        }

        var row = await conn.QuerySingleAsync<dynamic>(compiled.Sql, dp);
        decimal revenue = TryGetDecimal(row, "total_revenue", "TOTAL_REVENUE");

        Assert.True(revenue > 0, $"SUM(total_amount) = {revenue}");
    }

    /// <summary>Verifies that queries using common table expressions execute successfully.</summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Select_WithCTE_ActiveProducts_ShouldWork()
    {
        using var conn = Fixture.CreateConnection();
        var compiler = Fixture.CreateCompiler();

        var activeProducts = Sql.From<Product>().Where(p => p.IsActive);
        var query = Sql.From<Product>()
            .CTE("active_products", activeProducts)
            .RawSelect($"COUNT(*) AS cnt");

        var compiled = query.Build(compiler);
        var dp = new DynamicParameters();
        foreach (var p in compiled.Parameters)
        {
            dp.Add(p.Key, p.Value);
        }

        var row = await conn.QuerySingleAsync<dynamic>(compiled.Sql, dp);
        long cnt = TryGetLong(row, "cnt", "CNT");

        Assert.True(cnt > 0);
    }

    // ─── PERFORMANCE ─────────────────────────────────────────────────────────

    /// <summary>Verifies that retrieving one thousand order records completes within acceptable time limits.</summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Select_1000OrdersWithinTimeout_ShouldComplete()
    {
        using var conn = Fixture.CreateConnection();
        var compiler = Fixture.CreateCompiler();

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var query = Sql.From<Order>().Limit(1000);
        var orders = await QueryAsync<Order>(conn, query, compiler);
        sw.Stop();

        Assert.NotEmpty(orders);
        Assert.True(sw.ElapsedMilliseconds < 5000,
            $"Query took {sw.ElapsedMilliseconds}ms — expected < 5000ms");
    }

    // ─── Dynamic column name helpers (Oracle uses UPPERCASE) ──────────────────

    private static long TryGetLong(dynamic row, params string[] keys)
    {
        var dict = (IDictionary<string, object>)row;
        foreach (var key in keys)
        {
            if (dict.TryGetValue(key, out var val) && val != null)
            {
                return Convert.ToInt64(val, System.Globalization.CultureInfo.InvariantCulture);
            }
        }
        return 0L;
    }

    private static decimal TryGetDecimal(dynamic row, params string[] keys)
    {
        var dict = (IDictionary<string, object>)row;
        foreach (var key in keys)
        {
            if (dict.TryGetValue(key, out var val) && val != null)
            {
                return Convert.ToDecimal(val, System.Globalization.CultureInfo.InvariantCulture);
            }
        }
        return 0m;
    }
}





