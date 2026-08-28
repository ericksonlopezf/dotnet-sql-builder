// Copyright © Erickson Lopez. MIT License.
// ╔══════════════════════════════════════════════════════════════════════════════╗
// ║  EricksonLopez.SqlBuilder — PostgreSQL Playground                           ║
// ║  Demonstrates real-world SQL generation against PostgreSQL 16               ║
// ║                                                                              ║
// ║  Prerequisites:                                                              ║
// ║    docker compose up -d     (in this directory)                              ║
// ║    dotnet run                                                                 ║
// ╚══════════════════════════════════════════════════════════════════════════════╝
// ─── ANSI Colors ─────────────────────────────────────────────────────────────
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Dapper;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Annotations;
using EricksonLopez.SqlBuilder.Dapper;
using EricksonLopez.SqlBuilder.Playgrounds.PostgreSql;
using EricksonLopez.SqlBuilder.PostgreSql;
using Npgsql;

const string Reset  = "\x1b[0m";
const string Bold   = "\x1b[1m";
const string Cyan   = "\x1b[36m";
const string Green  = "\x1b[32m";
const string Yellow = "\x1b[33m";
const string Magenta= "\x1b[35m";
const string Gray   = "\x1b[90m";
const string Red    = "\x1b[31m";

static void PrintHeader(string title)
{
    Console.WriteLine();
    Console.WriteLine($"{Cyan}{Bold}{'═'.ToString().PadRight(70, '═')}{Reset}");
    Console.WriteLine($"{Cyan}{Bold}  {title}{Reset}");
    Console.WriteLine($"{Cyan}{Bold}{'═'.ToString().PadRight(70, '═')}{Reset}");
}

static void PrintSection(string section)
{
    Console.WriteLine();
    Console.WriteLine($"{Yellow}{Bold}▶ {section}{Reset}");
    Console.WriteLine($"{Gray}{"─".PadRight(60, '─')}{Reset}");
}

static void PrintSql(string sql, IReadOnlyDictionary<string, object?> parameters)
{
    Console.WriteLine($"{Gray}  SQL: {Magenta}{sql.Trim()}{Reset}");
    if (parameters.Count > 0)
    {
        var paramStr = string.Join(", ", parameters.Select(p => $"{p.Key}={p.Value}"));
        Console.WriteLine($"{Gray}  Params: {paramStr}{Reset}");
    }
}

static void PrintResult<T>(IEnumerable<T> items, Func<T, string> format, int maxRows = 5)
{
    var list = items.Take(maxRows).ToList();
    foreach (var item in list)
    {
        Console.WriteLine($"  {Green}→ {format(item)}{Reset}");
    }

    if (list.Count == 0)
    {
        Console.WriteLine($"  {Gray}(no results){Reset}");
    }
}

// ─── Main ─────────────────────────────────────────────────────────────────────

Console.OutputEncoding = System.Text.Encoding.UTF8;

PrintHeader("EricksonLopez.SqlBuilder — PostgreSQL 16 Playground");
Console.WriteLine($"  {Gray}Engine: PostgreSQL | Dialect: ANSI SQL + PG extensions{Reset}");
Console.WriteLine($"  {Gray}Compiler: PostgreSqlCompiler (\"identifier\" quoting, $1 params){Reset}");

const string connectionString =
    "Host=localhost;Port=5433;Database=sqlbuilder_demo;Username=demo;Password=Demo@SqlB1!;";

var compiler = new PostgreSqlCompiler();
DefaultTypeMap.MatchNamesWithUnderscores = true;

// Wait for DB to be ready
Console.WriteLine();
Console.Write($"  {Yellow}Connecting to PostgreSQL...{Reset}");
var sw = Stopwatch.StartNew();

await using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync();
sw.Stop();
Console.WriteLine($" {Green}✓ Connected in {sw.ElapsedMilliseconds}ms{Reset}");

// ═══════════════════════════════════════════════════════════════════════════════
// DEMO 1: Basic SELECT
// ═══════════════════════════════════════════════════════════════════════════════

PrintSection("1. Basic SELECT — All active customers");

var q1 = Sql.From<Customer>()
    .Where(c => c.IsActive)
    .OrderBy(c => c.Name);

var compiled1 = q1.Build(compiler);
PrintSql(compiled1.Sql, compiled1.Parameters);

var dp1 = new DynamicParameters();
foreach (var p in compiled1.Parameters)
{
    dp1.Add(p.Key, p.Value);
}

var customers = (await connection.QueryAsync<Customer>(compiled1.Sql, dp1)).ToList();

PrintResult(customers, c => $"{c.Id,3}  {c.Name,-30}  {c.Email}");
Console.WriteLine($"  {Gray}Total: {customers.Count} active customers{Reset}");

// ═══════════════════════════════════════════════════════════════════════════════
// DEMO 2: WHERE with FormattableString parameter binding
// ═══════════════════════════════════════════════════════════════════════════════

PrintSection("2. WHERE with FormattableString — Products in price range");

var minPrice = 500m;
var maxPrice = 2000m;

var q2 = Sql.From<Product>()
    .Where($"price BETWEEN {minPrice} AND {maxPrice}")
    .Where(p => p.IsActive)
    .OrderBy(p => p.Price);

var compiled2 = q2.Build(compiler);
PrintSql(compiled2.Sql, compiled2.Parameters);

var dp2 = new DynamicParameters();
foreach (var p in compiled2.Parameters)
{
    dp2.Add(p.Key, p.Value);
}

var products = (await connection.QueryAsync<Product>(compiled2.Sql, dp2)).ToList();

PrintResult(products, p => $"  {p.Sku,-25}  ${p.Price,10:N2}  stock={p.Stock}");
Console.WriteLine($"  {Gray}Total: {products.Count} products between ${minPrice:N2} – ${maxPrice:N2}{Reset}");

// ═══════════════════════════════════════════════════════════════════════════════
// DEMO 3: INNER JOIN — Orders with customer names
// ═══════════════════════════════════════════════════════════════════════════════

PrintSection("3. INNER JOIN — Orders with customer names");

var q3 = Sql.From<Order>()
    .InnerJoin("customers", "c", "c.id = orders.customer_id")
    .Select("orders.id", "orders.status", "orders.total_amount", "c.name")
    .Where(o => !o.IsDeleted)
    .OrderByDescending(o => o.TotalAmount)
    .Limit(8);

var compiled3 = q3.Build(compiler);
PrintSql(compiled3.Sql, compiled3.Parameters);

var dp3 = new DynamicParameters();
foreach (var p in compiled3.Parameters)
{
    dp3.Add(p.Key, p.Value);
}

var orderRows = (await connection.QueryAsync<dynamic>(compiled3.Sql, dp3)).ToList();

foreach (var row in orderRows)
{
    Console.WriteLine($"  {Green}→ #{row.id,-4} {row.status,-12} ${row.total_amount,10:N2}  {row.name}{Reset}");
}

// ═══════════════════════════════════════════════════════════════════════════════
// DEMO 4: GROUP BY + HAVING — Revenue by status
// ═══════════════════════════════════════════════════════════════════════════════

PrintSection("4. GROUP BY + HAVING — Revenue by order status (> $5000)");

var q4 = Sql.From<Order>()
    .Select("status")
    .RawSelect($"COUNT(*) AS order_count, SUM(total_amount) AS revenue")
    .Where(o => !o.IsDeleted)
    .GroupBy("status")
    .Having($"SUM(total_amount) > {5000m}")
    .OrderByDescending(o => o.TotalAmount);

var compiled4 = q4.Build(compiler);
PrintSql(compiled4.Sql, compiled4.Parameters);

var dp4 = new DynamicParameters();
foreach (var p in compiled4.Parameters)
{
    dp4.Add(p.Key, p.Value);
}

var revenueRows = (await connection.QueryAsync<dynamic>(compiled4.Sql, dp4)).ToList();

foreach (var row in revenueRows)
{
    Console.WriteLine($"  {Green}→ {row.status,-12}  {row.order_count} orders  ${row.revenue:N2} revenue{Reset}");
}

// ═══════════════════════════════════════════════════════════════════════════════
// DEMO 5: Pagination — Page 2 of products
// ═══════════════════════════════════════════════════════════════════════════════

PrintSection("5. Pagination — Products page 2 (5 per page)");

var q5 = Sql.From<Product>()
    .Where(p => p.IsActive)
    .OrderBy(p => p.Sku)
    .Limit(5)
    .Offset(5);

var compiled5 = q5.Build(compiler);
PrintSql(compiled5.Sql, compiled5.Parameters);

var dp5 = new DynamicParameters();
foreach (var p in compiled5.Parameters)
{
    dp5.Add(p.Key, p.Value);
}

var page2 = (await connection.QueryAsync<Product>(compiled5.Sql, dp5)).ToList();

PrintResult(page2, p => $"  {p.Sku,-25}  ${p.Price,8:N2}");

// ═══════════════════════════════════════════════════════════════════════════════
// DEMO 6: CTE — Top customers by revenue
// ═══════════════════════════════════════════════════════════════════════════════

PrintSection("6. CTE — Top customers by total revenue");

var orderRevenue = Sql.From<Order>()
    .Select("customer_id")
    .RawSelect($"SUM(total_amount) AS total_revenue, COUNT(*) AS order_count")
    .Where(o => !o.IsDeleted)
    .GroupBy("customer_id");

var q6 = Sql.From<Order>()
    .CTE("customer_revenue", orderRevenue)
    .From("customer_revenue", "cr")
    .InnerJoin("customers", "c", "c.id = cr.customer_id")
    .Select("c.name", "cr.order_count", "cr.total_revenue")
    .OrderByDescending(o => o.TotalAmount)
    .Limit(5);

var compiled6 = q6.Build(compiler);
PrintSql(compiled6.Sql, compiled6.Parameters);

var dp6 = new DynamicParameters();
foreach (var p in compiled6.Parameters)
{
    dp6.Add(p.Key, p.Value);
}

var topCustomers = (await connection.QueryAsync<dynamic>(compiled6.Sql, dp6)).ToList();

foreach (var row in topCustomers)
{
    Console.WriteLine($"  {Green}→ {row.name,-30}  {row.order_count} orders  ${row.total_revenue:N2}{Reset}");
}

// ═══════════════════════════════════════════════════════════════════════════════
// DEMO 7: INSERT with RETURNING
// ═══════════════════════════════════════════════════════════════════════════════

PrintSection("7. INSERT with RETURNING — New customer");

var newEmail = $"demo_{DateTime.UtcNow:yyyyMMddHHmmss}@playground.dev";
var insertQ = new InsertQuery<Customer>()
    .Into("customers")
    .Values("Playground Demo Corp", newEmail, "+1-555-9999", null, true)
    .Returning("id", "name", "email", "created_at");

var compiledInsert = insertQ.Build(compiler);
PrintSql(compiledInsert.Sql, compiledInsert.Parameters);

var dpInsert = new DynamicParameters();
foreach (var p in compiledInsert.Parameters)
{
    dpInsert.Add(p.Key, p.Value);
}

var inserted = await connection.QuerySingleAsync<dynamic>(compiledInsert.Sql, dpInsert);

Console.WriteLine($"  {Green}→ Inserted: #{inserted.id}  {inserted.name}  {inserted.email}{Reset}");
Console.WriteLine($"  {Green}→ Created:  {inserted.created_at:yyyy-MM-dd HH:mm:ss}{Reset}");

// ═══════════════════════════════════════════════════════════════════════════════
// DEMO 8: UPDATE
// ═══════════════════════════════════════════════════════════════════════════════

PrintSection("8. UPDATE — Rename the just-inserted customer");

var updateQ = Sql.Update<Customer>()
    .Set(c => c.Name, "Playground Demo Corp (Updated)")
    .Where(c => c.Email == newEmail);

var compiledUpdate = updateQ.Build(compiler);
PrintSql(compiledUpdate.Sql, compiledUpdate.Parameters);

var dpUpdate = new DynamicParameters();
foreach (var p in compiledUpdate.Parameters)
{
    dpUpdate.Add(p.Key, p.Value);
}

var rows = await connection.ExecuteAsync(compiledUpdate.Sql, dpUpdate);
Console.WriteLine($"  {Green}→ {rows} row(s) updated{Reset}");

// ═══════════════════════════════════════════════════════════════════════════════
// DEMO 9: UPSERT (ON CONFLICT DO UPDATE)
// ═══════════════════════════════════════════════════════════════════════════════

PrintSection("9. UPSERT — ON CONFLICT (email) DO UPDATE");

var upsertQ = new InsertQuery<Customer>()
    .Into("customers")
    .Values("Upsert Test Corp", newEmail, "+1-555-8888", null, true)
    .OnConflict("email")
    .DoUpdate(c => c.Name); // updates name on conflict

var compiledUpsert = upsertQ.Build(compiler);
PrintSql(compiledUpsert.Sql, compiledUpsert.Parameters);

var dpUpsert = new DynamicParameters();
foreach (var p in compiledUpsert.Parameters)
{
    dpUpsert.Add(p.Key, p.Value);
}

var upsertRows = await connection.ExecuteAsync(compiledUpsert.Sql, dpUpsert);
Console.WriteLine($"  {Green}→ {upsertRows} row(s) affected (upsert){Reset}");

// ═══════════════════════════════════════════════════════════════════════════════
// DEMO 10: DELETE
// ═══════════════════════════════════════════════════════════════════════════════

PrintSection("10. DELETE — Remove the demo customer");

var deleteQ = Sql.Delete<Customer>().Where($"email = {newEmail}");
var compiledDelete = deleteQ.Build(compiler);
PrintSql(compiledDelete.Sql, compiledDelete.Parameters);

var dpDelete = new DynamicParameters();
foreach (var p in compiledDelete.Parameters)
{
    dpDelete.Add(p.Key, p.Value);
}

var deleted = await connection.ExecuteAsync(compiledDelete.Sql, dpDelete);
Console.WriteLine($"  {Green}→ {deleted} row(s) deleted{Reset}");

// ═══════════════════════════════════════════════════════════════════════════════
// DEMO 11: SUBQUERY — Products never ordered
// ═══════════════════════════════════════════════════════════════════════════════

PrintSection("11. Subquery — Products never included in any order");

var q11 = Sql.From<Product>()
    .Where($"id NOT IN (SELECT DISTINCT product_id FROM order_items)")
    .Where(p => p.IsActive)
    .OrderBy(p => p.Name)
    .Limit(5);

var compiled11 = q11.Build(compiler);
PrintSql(compiled11.Sql, compiled11.Parameters);

var dp11 = new DynamicParameters();
foreach (var p in compiled11.Parameters)
{
    dp11.Add(p.Key, p.Value);
}

var neverOrdered = (await connection.QueryAsync<Product>(compiled11.Sql, dp11)).ToList();

PrintResult(neverOrdered, p => $"  {p.Sku,-25}  {p.Name}");

// ═══════════════════════════════════════════════════════════════════════════════
// DEMO 12: Transaction — Atomic order creation
// ═══════════════════════════════════════════════════════════════════════════════

PrintSection("12. Transaction — Atomic order + items insert");

await using var tx = await connection.BeginTransactionAsync();
try
{
    var orderInsert = new InsertQuery<Order>()
        .Into("orders")
        .Values(1, "pending", null, 3748.00m, 0m, 0m, "USD")
        .Returning("id");

    var compiledOrder = orderInsert.Build(compiler);
    var dpOrder = new DynamicParameters();
    foreach (var p in compiledOrder.Parameters)
    {
        dpOrder.Add(p.Key, p.Value);
    }

    var newOrderId = await connection.QuerySingleAsync<int>(compiledOrder.Sql, dpOrder, tx);

    Console.WriteLine($"  {Green}→ Created order #{newOrderId}{Reset}");

    // Add two items
    var itemInsert = new InsertQuery<OrderItem>()
        .Into("order_items")
        .Values(newOrderId, 2, 1, 2199.00m, 0m, 2199.00m);

    var compiledItem = itemInsert.Build(compiler);
    var dpItem = new DynamicParameters();
    foreach (var p in compiledItem.Parameters)
    {
        dpItem.Add(p.Key, p.Value);
    }

    await connection.ExecuteAsync(compiledItem.Sql, dpItem, tx);

    await tx.CommitAsync();
    Console.WriteLine($"  {Green}→ Transaction committed — order + items persisted{Reset}");
}
catch (Exception ex)
{
    await tx.RollbackAsync();
    Console.WriteLine($"  {Red}→ Transaction rolled back: {ex.Message}{Reset}");
}

// ═══════════════════════════════════════════════════════════════════════════════
// DONE
// ═══════════════════════════════════════════════════════════════════════════════

Console.WriteLine();
Console.WriteLine($"{Cyan}{Bold}{'═'.ToString().PadRight(70, '═')}{Reset}");
Console.WriteLine($"{Green}{Bold}  ✓ All 12 demos completed successfully!{Reset}");
Console.WriteLine($"  {Gray}EricksonLopez.SqlBuilder — PostgreSQL 16 playground{Reset}");
Console.WriteLine($"{Cyan}{Bold}{'═'.ToString().PadRight(70, '═')}{Reset}");
Console.WriteLine();

// ─── Entities (must be at end of top-level program file) ──────────────────────





