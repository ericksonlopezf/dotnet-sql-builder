// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Annotations;
using EricksonLopez.SqlBuilder.Dapper;
using EricksonLopez.SqlBuilder.Filters;
using EricksonLopez.SqlBuilder.PostgreSql;
using EricksonLopez.SqlBuilder.Sqlite;
using Microsoft.Data.Sqlite;

namespace EricksonLopez.SqlBuilder.Samples.Level04_AdvancedIntegration;

// ─── Entities ───────────────────────────────────────────────────────────────

[SqlEntity("accounts")]
public partial class Account
{
    public int Id { get; set; }
    public decimal Balance { get; set; }
    public bool IsActive { get; set; }
}

[SqlEntity("orders")]
public partial class Order
{
    [DatabaseGenerated] public int Id { get; set; }
    public int AccountId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

[SqlEntity("archived_orders")]
public partial class ArchivedOrder
{
    [DatabaseGenerated] public int Id { get; set; }
    public int AccountId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// ─── Specification Pattern: ISqlFilter<T> ───────────────────────────────────

/// <summary>
/// Reusable specification filter: accounts with IsActive == true.
/// </summary>
public class ActiveAccountsFilter : ISqlFilter<Account>
{
    public SelectQuery<Account> Apply(SelectQuery<Account> query)
        => query.Where(a => a.IsActive == true);
}

/// <summary>
/// Reusable specification filter: accounts with Balance >= minBalance.
/// </summary>
public class MinimumBalanceFilter : ISqlFilter<Account>
{
    private readonly decimal _minBalance;
    public MinimumBalanceFilter(decimal minBalance) => _minBalance = minBalance;

    public SelectQuery<Account> Apply(SelectQuery<Account> query)
        => query.Where(a => a.Balance >= _minBalance);
}

// ─── Main Sample ─────────────────────────────────────────────────────────────

public static class AdvancedIntegrationSample
{
    public static async Task RunAsync()
    {
        Console.WriteLine("\n=== LEVEL 4: ADVANCED INTEGRATION ===");

        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        DapperExtensions.RegisterCompiler<SqliteConnection>(() => new SqliteCompiler());

        await connection.ExecuteAsync(@"
            CREATE TABLE accounts (id INTEGER PRIMARY KEY, balance DECIMAL NOT NULL, is_active BOOLEAN NOT NULL);
            CREATE TABLE orders (id INTEGER PRIMARY KEY AUTOINCREMENT, account_id INTEGER NOT NULL, amount DECIMAL NOT NULL, status TEXT NOT NULL, created_at DATETIME NOT NULL);
            CREATE TABLE archived_orders (id INTEGER PRIMARY KEY AUTOINCREMENT, account_id INTEGER NOT NULL, amount DECIMAL NOT NULL, status TEXT NOT NULL, created_at DATETIME NOT NULL);
            INSERT INTO accounts VALUES (1, 1000, 1), (2, 50, 1), (3, 5000, 0);
            INSERT INTO orders (account_id, amount, status, created_at) VALUES
                (1, 200, 'completed', '2024-01-01'),
                (1, 150, 'completed', '2024-01-02'),
                (2, 75, 'pending', '2024-01-03');
        ");

        // ────────────────────────────────────────────────────────────────────
        // 1. Transaction Boundaries
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 1. Transactions (Funds transfer)");
        using (var transaction = connection.BeginTransaction())
        {
            try
            {
                var withdraw = Sql.Update<Account>()
                    .Set(a => a.Balance, 900m) // Account 1: 1000 - 100
                    .Where(a => a.Id == 1);

                var deposit = Sql.Update<Account>()
                    .Set(a => a.Balance, 150m) // Account 2: 50 + 100
                    .Where(a => a.Id == 2);

                await connection.ExecuteAsync(withdraw, transaction);
                await connection.ExecuteAsync(deposit, transaction);

                transaction.Commit();
                Console.WriteLine("    Transfer committed successfully.");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"    Transaction error: {ex.Message}");
            }
        }

        // ────────────────────────────────────────────────────────────────────
        // 2. Specification Pattern (ISqlFilter<T>)
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 2. Specification Pattern with ISqlFilter<T>");

        var filteredQuery = Sql.From<Account>()
            .ApplyFilters(
                new ActiveAccountsFilter(),
                new MinimumBalanceFilter(100m)
            );

        var validAccounts = await connection.QueryAsync<Account>(filteredQuery);
        foreach (var acc in validAccounts)
        {
            Console.WriteLine($"    Valid account: Id={acc.Id}, Balance={acc.Balance}");
        }

        // ────────────────────────────────────────────────────────────────────
        // 3. CASE Expression (SelectCase builder)
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 3. CASE Expression with SelectCase builder");

        // The SelectCase builder generates CASE WHEN ... THEN ... ELSE ... END AS alias
        var caseQuery = Sql.From<Order>()
            .SelectCase(c => c
                .When("status = {0}", "completed").Then("'DONE'")
                .When("status = {0}", "pending").Then("'WAITING'")
                .Else("'UNKNOWN'")
                .As("status_label"));

        var caseResult = caseQuery.Build(new SqliteCompiler());
        Console.WriteLine($"    Generated CASE SQL:\n    {caseResult.Sql}");

        // ────────────────────────────────────────────────────────────────────
        // 4. Window Functions (Window.Rank, Window.RowNumber, Window.Sum)
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 4. Window Functions (ROW_NUMBER, RANK, SUM)");

        var windowQuery = Sql.From<Order>()
            .Select(
                Window.RowNumber<Order>()
                      .OrderBy(o => o.CreatedAt)
                      .As("row_num"),
                Window.Sum<Order, decimal>(o => o.Amount)
                      .PartitionBy(o => o.AccountId)
                      .OrderBy(o => o.CreatedAt)
                      .As("running_total"));

        var windowSql = windowQuery.Build(new SqliteCompiler());
        Console.WriteLine($"    Window SQL:\n    {windowSql.Sql}");

        // ────────────────────────────────────────────────────────────────────
        // 5. Subquery Join (JoinSubquery / LeftJoinSubquery)
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 5. Subquery Join");

        var orderSubquery = Sql.From<Order>()
            .Select("account_id", "SUM(amount) as total_amount")
            .GroupBy("account_id");

        var subqueryJoinQuery = Sql.From<Account>()
            .Select("accounts.id", "accounts.balance", "o.total_amount")
            .JoinSubquery(orderSubquery, "o", "accounts.id = o.account_id");

        var subqueryJoinSql = subqueryJoinQuery.Build(new SqliteCompiler());
        Console.WriteLine($"    Subquery Join SQL:\n    {subqueryJoinSql.Sql}");

        // ────────────────────────────────────────────────────────────────────
        // 6. INSERT INTO ... SELECT (InsertFrom)
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 6. INSERT INTO ... SELECT (Sql.InsertFrom<T>)");

        // Archives completed orders into the archived_orders table
        var completedOrders = Sql.From<Order>()
            .Select("account_id", "amount", "status", "created_at")
            .Where(o => o.Status == "completed");

        var archiveQuery = Sql.InsertFrom<ArchivedOrder>(completedOrders, 
            "account_id", "amount", "status", "created_at");

        var archiveSql = archiveQuery.Build(new SqliteCompiler());
        Console.WriteLine($"    INSERT INTO...SELECT SQL:\n    {archiveSql.Sql}");

        // Execute it
        await connection.ExecuteAsync(archiveQuery);
        var archivedCount = await connection.QueryAsync<int>(Sql.Raw($"SELECT COUNT(*) FROM archived_orders"));
        Console.WriteLine($"    Archived orders: {archivedCount.First()}");

        // ────────────────────────────────────────────────────────────────────
        // 7. WhereExists / WhereNotExists (Subquery Exists)
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 7. WHERE EXISTS / WHERE NOT EXISTS");

        var hasOrdersSubquery = Sql.From<Order>()
            .Where($"account_id = accounts.id");

        var accountsWithOrders = Sql.From<Account>()
            .WhereExists(hasOrdersSubquery);

        var existsSql = accountsWithOrders.Build(new SqliteCompiler());
        Console.WriteLine($"    EXISTS SQL:\n    {existsSql.Sql}");

        var results = await connection.QueryAsync<Account>(accountsWithOrders);
        Console.WriteLine($"    Accounts with orders: {results.Count()}");

        // ────────────────────────────────────────────────────────────────────
        // 8. Union / Union All / Intersect / Except
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 8. Set Operations (UNION ALL)");

        var completed = Sql.From<Order>().Where(o => o.Status == "completed").Select("id", "amount");
        var pending = Sql.From<Order>().Where(o => o.Status == "pending").Select("id", "amount");

        var unionQuery = completed.UnionAll(pending);
        var unionSql = unionQuery.Build(new SqliteCompiler());
        Console.WriteLine($"    UNION ALL SQL:\n    {unionSql.Sql}");

        // ────────────────────────────────────────────────────────────────────
        // 9. Sql.Raw — Parameterized raw SQL
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 9. Sql.Raw — Parameterized raw SQL");
        int minBalance = 100;
        var rawQuery = Sql.Raw($"SELECT * FROM accounts WHERE balance >= {minBalance}");
        var rawAccounts = await connection.QueryAsync<Account>(rawQuery);
        Console.WriteLine($"    Accounts with balance >= {minBalance}: {rawAccounts.Count()}");

        // ────────────────────────────────────────────────────────────────────
        // 10. Distinct
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 10. SELECT DISTINCT");
        var distinctQuery = Sql.From<Order>()
            .Select("status")
            .Distinct();
        var distinctSql = distinctQuery.Build(new SqliteCompiler());
        Console.WriteLine($"    DISTINCT SQL: {distinctSql.Sql}");

        // ──────────────────────────────────────────────────────────────────
        // 11. Additional Join Types — InnerJoin, LeftJoin, RightJoin, FullJoin, CrossJoin
        // ──────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 11. Join Type Variants — InnerJoin / LeftJoin / RightJoin / FullJoin / CrossJoin");

        // InnerJoin (explicit alias method, functionally identical to Join)
        var innerJoinSql = Sql.From<Account>()
            .InnerJoin("orders", "o", "accounts.id = o.account_id")
            .Select("accounts.id", "o.amount")
            .Build(new SqliteCompiler());
        Console.WriteLine($"    INNER JOIN SQL: {innerJoinSql.Sql}");

        // LeftJoin
        var leftJoinSql2 = Sql.From<Account>()
            .LeftJoin("orders", "o", "accounts.id = o.account_id")
            .Select("accounts.id", "o.amount")
            .Build(new SqliteCompiler());
        Console.WriteLine($"    LEFT JOIN SQL: {leftJoinSql2.Sql}");

        // NOTE: RightJoin and FullJoin generate valid SQL; SQLite does not support them natively,
        // but SQL Server and PostgreSQL compilers do.
        var rightJoinSql = Sql.From<Account>()
            .RightJoin("orders", "o", "accounts.id = o.account_id")
            .Build(new SqliteCompiler());
        Console.WriteLine($"    RIGHT JOIN SQL (generated): {rightJoinSql.Sql}");

        var fullJoinSql = Sql.From<Account>()
            .FullJoin("orders", "o", "accounts.id = o.account_id")
            .Build(new SqliteCompiler());
        Console.WriteLine($"    FULL OUTER JOIN SQL (generated): {fullJoinSql.Sql}");

        // CrossJoin (no ON condition)
        var crossJoinSql = Sql.From<Account>()
            .CrossJoin("orders", "o")
            .Select("accounts.id")
            .Limit(3)
            .Build(new SqliteCompiler());
        Console.WriteLine($"    CROSS JOIN SQL: {crossJoinSql.Sql}");

        // ──────────────────────────────────────────────────────────────────
        // 12. Typed Join<TOther> — Type-safe join using LINQ expression predicate
        // ──────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 12. Typed Join<TOther> — Expression-based join condition");

        var typedJoinSql = Sql.From<Account>()
            .Join<Order>((account, order) => account.Id == order.AccountId)
            .Select("accounts.id", "accounts.balance")
            .Build(new SqliteCompiler());
        Console.WriteLine($"    Typed JOIN SQL: {typedJoinSql.Sql}");

        // ──────────────────────────────────────────────────────────────────
        // 13. UNION / INTERSECT / INTERSECT ALL / EXCEPT / EXCEPT ALL
        // ──────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 13. Set Operations — Union / Intersect / IntersectAll / Except / ExceptAll");

        var qCompleted = Sql.From<Order>().Select("account_id").Where(o => o.Status == "completed");
        var qHighValue  = Sql.From<Order>().Select("account_id").Where(o => o.Amount > 100m);

        // UNION (dedup)
        var unionDedupSql = qCompleted.Union(qHighValue).Build(new SqliteCompiler());
        Console.WriteLine($"    UNION SQL: {unionDedupSql.Sql}");

        // INTERSECT — rows present in both result sets
        var intersectSql = qCompleted.Intersect(qHighValue).Build(new SqliteCompiler());
        Console.WriteLine($"    INTERSECT SQL: {intersectSql.Sql}");

        // INTERSECT ALL — as above but preserving duplicates
        var intersectAllSql = qCompleted.IntersectAll(qHighValue).Build(new SqliteCompiler());
        Console.WriteLine($"    INTERSECT ALL SQL: {intersectAllSql.Sql}");

        // EXCEPT — rows in qCompleted not in qHighValue
        var exceptSql = qCompleted.Except(qHighValue).Build(new SqliteCompiler());
        Console.WriteLine($"    EXCEPT SQL: {exceptSql.Sql}");

        // EXCEPT ALL — as above but preserving duplicate counts
        var exceptAllSql = qCompleted.ExceptAll(qHighValue).Build(new SqliteCompiler());
        Console.WriteLine($"    EXCEPT ALL SQL: {exceptAllSql.Sql}");

        // ──────────────────────────────────────────────────────────────────
        // 14. OrExists / OrNotExists — OR variants of EXISTS predicate
        // ──────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 14. OrExists / OrNotExists — OR-EXISTS predicates");

        var completedOrdersSub = Sql.From<Order>().Where($"account_id = accounts.id AND status = 'completed'");
        var pendingOrdersSub   = Sql.From<Order>().Where($"account_id = accounts.id AND status = 'pending'");

        var orExistsSql = Sql.From<Account>()
            .WhereExists(completedOrdersSub)      // AND EXISTS (...)
            .OrExists(pendingOrdersSub)           // OR  EXISTS (...)
            .Build(new SqliteCompiler());
        Console.WriteLine($"    OrExists SQL: {orExistsSql.Sql}");

        var orNotExistsSql = Sql.From<Account>()
            .WhereNotExists(completedOrdersSub)   // AND NOT EXISTS (...)
            .OrNotExists(pendingOrdersSub)        // OR  NOT EXISTS (...)
            .Build(new SqliteCompiler());
        Console.WriteLine($"    OrNotExists SQL: {orNotExistsSql.Sql}");

        // ──────────────────────────────────────────────────────────────────
        // 15. From(tableName, alias) / From(subquery, alias) / Alias()
        // ──────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 15. From(tableName, alias) / From(subquery, alias) / Alias()");

        // From(tableName, alias) — override default entity table with an alias
        var aliasedFromSql = Sql.From<Account>()
            .From("accounts", "a")
            .Select("a.id", "a.balance")
            .Where(acc => acc.IsActive == true)
            .Build(new SqliteCompiler());
        Console.WriteLine($"    From(tableName, alias) SQL: {aliasedFromSql.Sql}");

        // From(subquery, alias) — use a subquery as the main data source (derived table)
        var orderTotalsSubquery = Sql.From<Order>()
            .Select("account_id", "SUM(amount) as total")
            .GroupBy("account_id");

        var derivedTableSql = Sql.From<Account>()
            .From(orderTotalsSubquery, "order_totals")
            .Select("account_id", "total")
            .Build(new SqliteCompiler());
        Console.WriteLine($"    From(subquery, alias) SQL: {derivedTableSql.Sql}");

        // Alias() — assigns an alias to the query itself when embedded as subquery
        var namedQuery = Sql.From<Account>()
            .Select("id", "balance")
            .Alias("rich_accounts");
        var namedSql = namedQuery.Build(new SqliteCompiler());
        Console.WriteLine($"    Alias() SQL: {namedSql.Sql}");

        // ──────────────────────────────────────────────────────────────────
        // 16. RawSelect — Raw SQL expression in SELECT clause
        // ──────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 16. RawSelect — Raw SQL expression in SELECT clause");

        decimal threshold2 = 100m;
        var rawSelectSql = Sql.From<Order>()
            .Select("id", "amount", "status")
            .RawSelect($"CASE WHEN amount > {threshold2} THEN 'large' ELSE 'small' END AS size_label")
            .Build(new SqliteCompiler());
        Console.WriteLine($"    RawSelect SQL: {rawSelectSql.Sql}");

        // ──────────────────────────────────────────────────────────────────
        // 17. RawJoin — Arbitrary SQL JOIN expression
        // ──────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 17. RawJoin — Raw JOIN with SQL expression");

        decimal minAmt = 50m;
        var rawJoinSql = Sql.From<Account>()
            .Select("accounts.id", "o.amount")
            .RawJoin($"LEFT JOIN orders o ON accounts.id = o.account_id AND o.amount > {minAmt}")
            .Build(new SqliteCompiler());
        Console.WriteLine($"    RawJoin SQL: {rawJoinSql.Sql}");

        // ──────────────────────────────────────────────────────────────────
        // 18. Select(ISqlQuery, alias) — Scalar subquery in SELECT clause
        // ──────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 18. Select(subquery, alias) — Scalar subquery en SELECT");

        var orderCountSub = Sql.From<Order>()
            .Where($"account_id = accounts.id")
            .AsCount("cnt");

        var scalarSelectSql = Sql.From<Account>()
            .Select("id", "balance")
            .Select(orderCountSub, "order_count")
            .Build(new SqliteCompiler());
        Console.WriteLine($"    Scalar subquery SELECT SQL: {scalarSelectSql.Sql}");

        // ──────────────────────────────────────────────────────────────────
        // 19. Window function builder — Window.Rank / DenseRank / RowNumber
        //     / Lag / Lead / Sum / Avg / Count / Min / Max
        //     / FirstValue / LastValue / NthValue / CumeDist / PercentRank
        //     / StdDev / Variance
        //
        //  Usage: Sql.From<T>().Select(Window.Rank<T>().PartitionBy(...).OrderByDescending(...).As("alias"))
        // ──────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 19. Window Function Builder (Window static factory)");

        // RANK() OVER (PARTITION BY account_id ORDER BY amount DESC)
        var rankQuery = Sql.From<Order>()
            .Select("id", "account_id", "amount")
            .Select(
                Window.Rank<Order>()
                      .PartitionBy(o => o.AccountId)
                      .OrderByDescending(o => o.Amount)
                      .As("rnk"))
            .Build(new SqliteCompiler());
        Console.WriteLine($"    RANK() SQL: {rankQuery.Sql}");

        // DENSE_RANK()
        var denseRankQuery = Sql.From<Order>()
            .Select(
                Window.DenseRank<Order>()
                      .PartitionBy(o => o.AccountId)
                      .OrderByDescending(o => o.Amount)
                      .As("dense_rnk"))
            .Build(new SqliteCompiler());
        Console.WriteLine($"    DENSE_RANK() SQL: {denseRankQuery.Sql}");

        // ROW_NUMBER() with FILTER (WHERE ...) — supported by PostgreSQL and SQLite 3.25+.
        // SQL Server does NOT support FILTER on window functions; use PostgreSqlCompiler or SqliteCompiler.
        var rowNumQuery = Sql.From<Order>()
            .Select(
                Window.RowNumber<Order>()
                      .OrderBy(o => o.CreatedAt)
                      .Filter(o => o.Status == "completed")
                      .As("row_num"))
            .Build(new PostgreSqlCompiler());
        Console.WriteLine($"    ROW_NUMBER() + FILTER SQL (PostgreSQL): {rowNumQuery.Sql}");

        // LAG(amount, 1) — previous row value
        var lagQuery = Sql.From<Order>()
            .Select(
                Window.Lag<Order, decimal>(o => o.Amount, offset: 1, defaultValue: 0m)
                      .PartitionBy(o => o.AccountId)
                      .OrderBy(o => o.CreatedAt)
                      .As("prev_amount"))
            .Build(new SqliteCompiler());
        Console.WriteLine($"    LAG() SQL: {lagQuery.Sql}");

        // LEAD(amount, 1) — next row value
        var leadQuery = Sql.From<Order>()
            .Select(
                Window.Lead<Order, decimal>(o => o.Amount, offset: 1)
                      .PartitionBy(o => o.AccountId)
                      .OrderBy(o => o.CreatedAt)
                      .As("next_amount"))
            .Build(new SqliteCompiler());
        Console.WriteLine($"    LEAD() SQL: {leadQuery.Sql}");

        // SUM OVER PARTITION — running total
        var sumOverQuery = Sql.From<Order>()
            .Select(
                Window.Sum<Order, decimal>(o => o.Amount)
                      .PartitionBy(o => o.AccountId)
                      .OrderBy(o => o.CreatedAt)
                      .As("running_total"))
            .Build(new SqliteCompiler());
        Console.WriteLine($"    SUM() OVER SQL: {sumOverQuery.Sql}");

        // AVG OVER PARTITION
        var avgOverQuery = Sql.From<Order>()
            .Select(
                Window.Avg<Order, decimal>(o => o.Amount)
                      .PartitionBy(o => o.AccountId)
                      .As("running_avg"))
            .Build(new SqliteCompiler());
        Console.WriteLine($"    AVG() OVER SQL: {avgOverQuery.Sql}");

        // COUNT(*) OVER
        var countOverQuery = Sql.From<Order>()
            .Select(
                Window.Count<Order>()
                      .PartitionBy(o => o.AccountId)
                      .As("partition_count"))
            .Build(new SqliteCompiler());
        Console.WriteLine($"    COUNT(*) OVER SQL: {countOverQuery.Sql}");

        // NTILE(4) — quartile buckets
        var ntileQuery = Sql.From<Order>()
            .Select(
                Window.Ntile<Order>(4)
                      .OrderByDescending(o => o.Amount)
                      .As("quartile"))
            .Build(new SqliteCompiler());
        Console.WriteLine($"    NTILE(4) SQL: {ntileQuery.Sql}");

        // FIRST_VALUE / LAST_VALUE
        var firstValQuery = Sql.From<Order>()
            .Select(
                Window.FirstValue<Order, decimal>(o => o.Amount)
                      .PartitionBy(o => o.AccountId)
                      .OrderBy(o => o.CreatedAt)
                      .As("first_amount"),
                Window.LastValue<Order, decimal>(o => o.Amount)
                      .PartitionBy(o => o.AccountId)
                      .OrderBy(o => o.CreatedAt)
                      .As("last_amount"))
            .Build(new SqliteCompiler());
        Console.WriteLine($"    FIRST_VALUE/LAST_VALUE SQL: {firstValQuery.Sql}");

        // NTH_VALUE(amount, 2)
        var nthValQuery = Sql.From<Order>()
            .Select(
                Window.NthValue<Order, decimal>(o => o.Amount, n: 2)
                      .PartitionBy(o => o.AccountId)
                      .OrderBy(o => o.CreatedAt)
                      .As("second_amount"))
            .Build(new SqliteCompiler());
        Console.WriteLine($"    NTH_VALUE(2) SQL: {nthValQuery.Sql}");

        // CUME_DIST / PERCENT_RANK
        var cumeDistQuery = Sql.From<Order>()
            .Select(
                Window.CumeDist<Order>().OrderBy(o => o.Amount).As("cume_dist"),
                Window.PercentRank<Order>().OrderBy(o => o.Amount).As("pct_rank"))
            .Build(new SqliteCompiler());
        Console.WriteLine($"    CUME_DIST/PERCENT_RANK SQL: {cumeDistQuery.Sql}");

        // STDDEV_SAMP / VAR_SAMP — statistical aggregations
        var stdDevQuery = Sql.From<Order>()
            .Select(
                Window.StdDev<Order, decimal>(o => o.Amount)
                      .PartitionBy(o => o.AccountId)
                      .As("std_dev"),
                Window.Variance<Order, decimal>(o => o.Amount)
                      .PartitionBy(o => o.AccountId)
                      .As("variance"))
            .Build(new SqliteCompiler());
        Console.WriteLine($"    STDDEV/VARIANCE SQL: {stdDevQuery.Sql}");

        // ──────────────────────────────────────────────────────────────────
        // 20. LateralJoin / LateralLeftJoin — Correlated subquery joins
        //     Supported: PostgreSQL, MySQL 8.0+
        //     Not supported: SQLite (generates SQL for documentation purposes only)
        // ──────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 20. LateralJoin / LateralLeftJoin — Correlated subquery joins");

        // LateralJoin(IAstQuery, alias) — inline subquery evaluated per outer row
        var latSubquery = Sql.From<Order>()
            .Where($"account_id = accounts.id AND status = 'completed'")
            .OrderByDescending(o => o.Amount)
            .Limit(1);

        var lateralJoinSql = Sql.From<Account>()
            .Select("accounts.id", "accounts.balance")
            .LateralJoin(latSubquery, "top_order")
            .Build(new EricksonLopez.SqlBuilder.SqlServer.SqlServerCompiler());
        Console.WriteLine($"    LateralJoin SQL: {lateralJoinSql.Sql}");

        // LateralJoin<TSub>(Func<SelectQuery<TSub>, IAstQuery>, alias) — factory overload
        var lateralFactorySql = Sql.From<Account>()
            .Select("accounts.id")
            .LateralJoin<Order>(q => q
                .Where($"account_id = accounts.id")
                .AsCount("cnt"),
                alias: "order_stats")
            .Build(new EricksonLopez.SqlBuilder.SqlServer.SqlServerCompiler());
        Console.WriteLine($"    LateralJoin<TSub>(factory) SQL: {lateralFactorySql.Sql}");

        // LateralLeftJoin — preserves rows with no matching lateral subquery result
        var latLeftSql = Sql.From<Account>()
            .Select("accounts.id")
            .LateralLeftJoin(latSubquery, "top_order")
            .Build(new EricksonLopez.SqlBuilder.SqlServer.SqlServerCompiler());
        Console.WriteLine($"    LateralLeftJoin SQL: {latLeftSql.Sql}");

        // ──────────────────────────────────────────────────────────────────
        // 21. JoinSubquery / LeftJoinSubquery — Non-lateral derived table join
        // ──────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 21. JoinSubquery / LeftJoinSubquery — Derived table join");

        var orderTotals = Sql.From<Order>()
            .Select("account_id", "SUM(amount) as total")
            .GroupBy("account_id");

        // JoinSubquery(IAstQuery, alias, on) — INNER JOIN (SELECT ...) AS alias ON ...
        var joinSubquerySql = Sql.From<Account>()
            .Select("accounts.id", "ot.total")
            .JoinSubquery(orderTotals, "ot", "accounts.id = ot.account_id")
            .Build(new SqliteCompiler());
        Console.WriteLine($"    JoinSubquery SQL: {joinSubquerySql.Sql}");

        // LeftJoinSubquery(IAstQuery, alias, on) — LEFT JOIN (SELECT ...) AS alias ON ...
        var leftJoinSubquerySql = Sql.From<Account>()
            .Select("accounts.id", "ot.total")
            .LeftJoinSubquery(orderTotals, "ot", "accounts.id = ot.account_id")
            .Build(new SqliteCompiler());
        Console.WriteLine($"    LeftJoinSubquery SQL: {leftJoinSubquerySql.Sql}");

        // ──────────────────────────────────────────────────────────────────
        // 22. CrossApply / OuterApply — SQL Server-specific APPLY operations
        //     Equivalent to CROSS JOIN LATERAL / LEFT JOIN LATERAL in PostgreSQL
        //     Not supported by SQLite — SQL generated for documentation only
        // ──────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 22. CrossApply / OuterApply — SQL Server APPLY");

        var applySubquery = Sql.From<Order>()
            .Where($"account_id = accounts.id")
            .OrderByDescending(o => o.Amount)
            .Limit(3);

        // CrossApply — only rows from outer query that have matching rows in correlated sub
        var crossApplySql = Sql.From<Account>()
            .Select("accounts.id")
            .CrossApply(applySubquery, "top_orders")
            .Build(new EricksonLopez.SqlBuilder.SqlServer.SqlServerCompiler());
        Console.WriteLine($"    CROSS APPLY SQL: {crossApplySql.Sql}");

        // OuterApply — preserves all outer rows (like LEFT JOIN LATERAL)
        var outerApplySql = Sql.From<Account>()
            .Select("accounts.id")
            .OuterApply(applySubquery, "top_orders")
            .Build(new EricksonLopez.SqlBuilder.SqlServer.SqlServerCompiler());
        Console.WriteLine($"    OUTER APPLY SQL: {outerApplySql.Sql}");

        // CrossApply<TSub>(Func<>, alias) — factory overload
        var crossApplyFactorySql = Sql.From<Account>()
            .CrossApply<Order>(q => q.Where($"account_id = accounts.id").Limit(1), "latest")
            .Build(new EricksonLopez.SqlBuilder.SqlServer.SqlServerCompiler());
        Console.WriteLine($"    CROSS APPLY (factory) SQL: {crossApplyFactorySql.Sql}");

        // ──────────────────────────────────────────────────────────────────
        // 23. CTE(name, query, MaterializationHint) — Optimizer hint for CTEs
        //     Supported: PostgreSQL 12+. Hint is ignored by other dialects.
        // ──────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 23. CTE with MaterializationHint");

        using var mhNamespace = new System.Threading.CancellationTokenSource(); // just to prove namespace
        // MaterializationHint.Materialized — force CTE to be materialized as a temp table
        var cteMaterialized = Sql.From<Order>()
            .CTE("expensive_sub",
                Sql.From<Order>().Where(o => o.Amount > 500m),
                EricksonLopez.SqlBuilder.Abstractions.Nodes.MaterializationHint.Materialized)
            .Select("id", "amount")
            .Build(new EricksonLopez.SqlBuilder.SqlServer.SqlServerCompiler());
        Console.WriteLine($"    CTE MATERIALIZED SQL: {cteMaterialized.Sql}");

        // MaterializationHint.NotMaterialized — force CTE to be inlined
        var cteNotMaterialized = Sql.From<Order>()
            .CTE("cheap_sub",
                Sql.From<Order>().Where(o => o.Status == "pending"),
                EricksonLopez.SqlBuilder.Abstractions.Nodes.MaterializationHint.NotMaterialized)
            .Select("id", "status")
            .Build(new EricksonLopez.SqlBuilder.SqlServer.SqlServerCompiler());
        Console.WriteLine($"    CTE NOT MATERIALIZED SQL: {cteNotMaterialized.Sql}");

        // ──────────────────────────────────────────────────────────────────
        // 24. ThenBy / ThenByDescending — Multi-key sorting
        //     Used after an initial OrderBy to add secondary / tertiary sort keys
        // ──────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 24. ThenBy / ThenByDescending — Multi-key ORDER BY");

        var thenBySql = Sql.From<Order>()
            .OrderBy(o => o.AccountId)          // primary ascending
            .ThenByDescending(o => o.Amount)    // secondary descending
            .ThenBy(o => o.CreatedAt)           // tertiary ascending
            .Build(new SqliteCompiler());
        Console.WriteLine($"    ThenBy SQL: {thenBySql.Sql}");

        // With NullsPosition on secondary key
        var thenByNullsSql = Sql.From<Order>()
            .OrderBy(o => o.AccountId)
            .ThenBy(o => o.Amount, EricksonLopez.SqlBuilder.Abstractions.Nodes.NullsPosition.Last)
            .Build(new SqliteCompiler());
        Console.WriteLine($"    ThenBy NULLS LAST SQL: {thenByNullsSql.Sql}");

        // ──────────────────────────────────────────────────────────────────
        // 25. OrHaving — OR condition in HAVING clause
        // ──────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 25. OrHaving — OR condition in HAVING");

        var orHavingSql = Sql.From<Order>()
            .Select("account_id")
            .AsSum("amount", "total")
            .GroupBy("account_id")
            .Having(o => o.Amount > 100m)           // AND HAVING amount > 100
            .OrHaving(o => o.AccountId == 5)        // OR HAVING account_id = 5
            .Build(new SqliteCompiler());
        Console.WriteLine($"    OrHaving (typed) SQL: {orHavingSql.Sql}");

        decimal highAmt = 1000m;
        var orHavingRawSql = Sql.From<Order>()
            .Select("account_id")
            .AsSum("amount", "total")
            .GroupBy("account_id")
            .Having($"SUM(amount) > {highAmt}")
            .OrHaving($"COUNT(*) > {10}")
            .Build(new SqliteCompiler());
        Console.WriteLine($"    OrHaving (raw) SQL: {orHavingRawSql.Sql}");

        // ──────────────────────────────────────────────────────────────────
        // 26. WhereDay — Day component filter in WHERE
        // ──────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 26. WhereDay — Filter by DAY component of date");
        var whereDaySql = Sql.From<Order>()
            .WhereDay("created_at", "=", 15)
            .Build(new SqliteCompiler());
        Console.WriteLine($"    WhereDay SQL: {whereDaySql.Sql}");

        // ──────────────────────────────────────────────────────────────────
        // 27. OrderBy(FormattableString) / OrderByDescending(FormattableString) — Raw ORDER BY
        // ──────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 27. OrderBy(FormattableString) — Raw ORDER BY expression");

        // Dynamic expression with parameter injection (safe — no SQL injection)
        string colName = "amount";
        var rawOrderBySql = Sql.From<Order>()
            .OrderBy($"{colName} DESC NULLS LAST")
            .Build(new SqliteCompiler());
        Console.WriteLine($"    OrderBy(FormattableString) SQL: {rawOrderBySql.Sql}");

        var rawOrderByDescSql = Sql.From<Order>()
            .OrderByDescending($"{colName}")
            .Build(new SqliteCompiler());
        Console.WriteLine($"    OrderByDescending(FormattableString) SQL: {rawOrderByDescSql.Sql}");

        // ──────────────────────────────────────────────────────────────────
        // 28. Offset(int) — Skip rows without an explicit Limit
        //     Combined with Limit for standard pagination
        // ──────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 28. Offset(int) — Raw offset without Limit");

        var offsetOnlySql = Sql.From<Order>()
            .OrderBy(o => o.Id)
            .Offset(20)        // skip first 20 rows
            .Build(new SqliteCompiler());
        Console.WriteLine($"    Offset SQL: {offsetOnlySql.Sql}");

        // ──────────────────────────────────────────────────────────────────
        // 29. Fetch(int) — [Obsolete] Alias for Limit; prefer Limit()
        // ──────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 29. Fetch(int) — [Obsolete] alias for Limit()");

        #pragma warning disable CS0618 // Fetch is [Obsolete] — documented for API completeness
        var fetchSql = Sql.From<Order>()
            .OrderBy(o => o.Id)
            .Fetch(5)    // equivalent to .Limit(5) — prefer Limit()
            .Build(new SqliteCompiler());
        #pragma warning restore CS0618
        Console.WriteLine($"    Fetch (obsolete) SQL: {fetchSql.Sql}");

        // ──────────────────────────────────────────────────────────────────
        // 30. And / Or — AND / OR conditions in WHERE (typed predicate)
        // ──────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 30. And / Or — Logical AND / OR WHERE conditions");

        var andOrSql = Sql.From<Order>()
            .Where(o => o.Status == "pending")     // WHERE status = 'pending'
            .And(o => o.Amount > 50m)              // AND amount > 50
            .Or(o => o.AccountId == 1)             // OR account_id = 1
            .Build(new SqliteCompiler());
        Console.WriteLine($"    And/Or SQL: {andOrSql.Sql}");
    }
}



