// Copyright © Erickson Lopez. MIT License.
// ╔══════════════════════════════════════════════════════════════════════════════╗
// ║  EricksonLopez.SqlBuilder — Oracle Free 23c Playground                      ║
// ║  Demonstrates real-world SQL generation against Oracle Database Free 23c     ║
// ║                                                                              ║
// ║  Prerequisites:                                                              ║
// ║    docker compose up -d     (first boot takes ~90s)                          ║
// ║    dotnet run                                                                 ║
// ║                                                                              ║
// ║  Oracle-specific features showcased:                                         ║
// ║    • "double-quoted" identifiers                                             ║
// ║    • :p0, :p1, ... named parameters                                          ║
// ║    • FETCH FIRST n ROWS ONLY (pagination)                                    ║
// ║    • OFFSET m ROWS FETCH NEXT n ROWS ONLY                                    ║
// ║    • IDENTITY columns (12c+)                                                  ║
// ║    • MERGE INTO ... USING ... WHEN MATCHED / WHEN NOT MATCHED                ║
// ║    • RETURNING ... INTO :out_var (via ODP.NET)                               ║
// ║    • ROWNUM / ROW_NUMBER() analytics                                         ║
// ║    • Dual-table arithmetic                                                   ║
// ╚══════════════════════════════════════════════════════════════════════════════╝
// ─── ANSI Colors ─────────────────────────────────────────────────────────────
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Oracle;
using EricksonLopez.SqlBuilder.Playgrounds.Oracle;
using Oracle.ManagedDataAccess.Client;

const string R  = "\x1b[0m";
const string B  = "\x1b[1m";
const string Cy = "\x1b[36m";
const string Gn = "\x1b[32m";
const string Ye = "\x1b[33m";
const string Mg = "\x1b[35m";
const string Gr = "\x1b[90m";
const string Bl = "\x1b[34m";
const string Re = "\x1b[31m";

static void H(string t)
{
    Console.WriteLine();
    Console.WriteLine($"{Cy}{B}{"═".PadRight(70, '═')}{R}");
    Console.WriteLine($"{Cy}{B}  {t}{R}");
    Console.WriteLine($"{Cy}{B}{"═".PadRight(70, '═')}{R}");
}

static void S(string s)
{
    Console.WriteLine();
    Console.WriteLine($"{Ye}{B}▶ {s}{R}");
    Console.WriteLine($"{Gr}{"─".PadRight(60, '─')}{R}");
}

static void P(string sql, IReadOnlyDictionary<string, object?> p)
{
    Console.WriteLine($"{Gr}  SQL: {Mg}{sql.Trim()}{R}");
    if (p.Count > 0)
    {
        Console.WriteLine($"{Gr}  Params: {string.Join(", ", p.Select(x => $"{x.Key}={x.Value}"))}{R}");
    }
}

static void Row(string s) => Console.WriteLine($"  {Gn}→ {s}{R}");
static void Val(string k, object? v) => Console.WriteLine($"  {Gn}→ {k}: {B}{v}{R}");
static void Info(string s) => Console.WriteLine($"  {Bl}ℹ {s}{R}");

// Helper: build DynamicParameters from compiled result
static DynamicParameters ToParams(IReadOnlyDictionary<string, object?> parameters)
{
    var dp = new DynamicParameters();
    foreach (var p in parameters)
    {
        dp.Add(p.Key, p.Value);
    }

    return dp;
}

// ─── Oracle Date Format ───────────────────────────────────────────────────────

// Oracle sessions default NLS_DATE_FORMAT must be set for DateTime parsing
OracleConfiguration.OracleDataSources.Add("DEMO", "(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1522))(CONNECT_DATA=(SERVICE_NAME=FREEPDB1)))");

Console.OutputEncoding = System.Text.Encoding.UTF8;
DefaultTypeMap.MatchNamesWithUnderscores = true;

H("EricksonLopez.SqlBuilder — Oracle Free 23c Playground");
Console.WriteLine($"  {Gr}Engine: Oracle Free 23c | Compiler: OracleCompiler{R}");
Console.WriteLine($"  {Gr}Identifiers: \"double-quoted\" | Params: :p0, :p1, ...|{R}");
Console.WriteLine($"  {Bl}Features: FETCH FIRST, OFFSET FETCH, IDENTITY, MERGE INTO{R}");

// Connection string using EZ Connect Plus
const string cs = "User Id=demo;Password=Demo@SqlB1!;Data Source=localhost:1522/FREEPDB1;";
var compiler = new OracleCompiler();

Console.Write($"\n  {Ye}Connecting to Oracle Free 23c...{R}");
var sw = Stopwatch.StartNew();
await using var conn = new OracleConnection(cs);

// Retry for container startup (~90s first boot)
for (int i = 0; i < 30; i++)
{
    try
    {
        await conn.OpenAsync();
        break;
    }
    catch (Exception ex)
    {
        if (i == 29)
        {
            Console.WriteLine($"\n  {Re}✗ Could not connect after 90s: {ex.Message}{R}");
            Console.WriteLine($"  {Ye}Tip: docker compose up -d && wait 90s for Oracle to initialize{R}");
            return;
        }
        Console.Write(".");
        await Task.Delay(3000);
    }
}
sw.Stop();
Console.WriteLine($" {Gn}✓ Connected in {sw.ElapsedMilliseconds}ms{R}");

// Set NLS session params for date formatting compatibility
await conn.ExecuteAsync("ALTER SESSION SET NLS_DATE_FORMAT = 'YYYY-MM-DD HH24:MI:SS'");
await conn.ExecuteAsync("ALTER SESSION SET NLS_TIMESTAMP_FORMAT = 'YYYY-MM-DD HH24:MI:SS.FF3'");
await conn.ExecuteAsync("ALTER SESSION SET NLS_NUMERIC_CHARACTERS = '.,'");

// Verify schema
var tableCount = await conn.QuerySingleAsync<int>(
    "SELECT COUNT(*) FROM user_tables WHERE table_name IN ('customers','products','orders','order_items','categories')");
if (tableCount < 5)
{
    Console.WriteLine($"  {Re}✗ Schema not ready — run: docker compose down -v && docker compose up -d{R}");
    return;
}
Console.WriteLine($"  {Gn}✓ Schema verified — {tableCount} tables found{R}");

// ═══════════════════════════════════════════════════════════════════════════════
// DEMO 1: Basic SELECT with "double-quoted" identifiers
// ═══════════════════════════════════════════════════════════════════════════════

S("1. Basic SELECT — Active customers");
Info("Oracle uses \"double-quoted\" identifiers — case-sensitive");

var q1 = Sql.From<Customer>()
    .Where($"\"is_active\" = {1}")
    .OrderBy(c => c.Name);

var r1 = q1.Build(compiler);
P(r1.Sql, r1.Parameters);

var customers = (await conn.QueryAsync<Customer>(r1.Sql, ToParams(r1.Parameters))).ToList();
customers.Take(5).ToList().ForEach(c => Row($"#{c.Id,-4} {c.Name,-28} {c.Email}"));
Val("Total active", customers.Count);

// ═══════════════════════════════════════════════════════════════════════════════
// DEMO 2: FETCH FIRST N ROWS ONLY (Oracle 12c+ pagination)
// ═══════════════════════════════════════════════════════════════════════════════

S("2. FETCH FIRST — Top 5 most expensive products");
Info("Oracle uses FETCH FIRST n ROWS ONLY instead of LIMIT n");

var q2 = Sql.From<Product>()
    .Where($"\"is_active\" = {1}")
    .OrderByDescending(p => p.Price)
    .Limit(5);

var r2 = q2.Build(compiler);
P(r2.Sql, r2.Parameters);

var products = (await conn.QueryAsync<Product>(r2.Sql, ToParams(r2.Parameters))).ToList();
products.ForEach(p => Row($"  {p.Sku,-25}  {p.Name,-25}  ${p.Price:N2}"));

// ═══════════════════════════════════════════════════════════════════════════════
// DEMO 3: OFFSET m ROWS FETCH NEXT n ROWS ONLY (pagination)
// ═══════════════════════════════════════════════════════════════════════════════

S("3. OFFSET FETCH — Products page 2 (skip 3, take 4)");
Info("Oracle: OFFSET m ROWS FETCH NEXT n ROWS ONLY");

var q3 = Sql.From<Product>()
    .OrderBy(p => p.Price)
    .Offset(3)
    .Limit(4);

var r3 = q3.Build(compiler);
P(r3.Sql, r3.Parameters);

(await conn.QueryAsync<Product>(r3.Sql, ToParams(r3.Parameters))).ToList()
    .ForEach(p => Row($"  {p.Sku,-25}  ${p.Price:N2}"));

// ═══════════════════════════════════════════════════════════════════════════════
// DEMO 4: WHERE with FormattableString :param binding
// ═══════════════════════════════════════════════════════════════════════════════

S("4. WHERE FormattableString — Orders by status with :p0 params");
Info("Oracle params: :p0, :p1, ... (NOT @p0 or $1)");

var q4 = Sql.From<Order>()
    .Where($"\"is_deleted\" = {0}")
    .Where($"\"status\" = {"delivered"}")
    .OrderByDescending(o => o.TotalAmount);

var r4 = q4.Build(compiler);
P(r4.Sql, r4.Parameters);

var orders = (await conn.QueryAsync<Order>(r4.Sql, ToParams(r4.Parameters))).ToList();
orders.ForEach(o => Row($"  #{o.Id,-4}  {o.Status,-12}  ${o.TotalAmount:N2}  ({o.Currency})"));

// ═══════════════════════════════════════════════════════════════════════════════
// DEMO 5: INNER JOIN
// ═══════════════════════════════════════════════════════════════════════════════

S("5. INNER JOIN — Orders with customer names");

var q5 = Sql.From<Order>()
    .InnerJoin("customers", "c", "c.\"id\" = orders.\"customer_id\"")
    .Select("orders.\"id\"", "orders.\"status\"", "orders.\"total_amount\"", "c.\"name\"")
    .Where($"orders.\"is_deleted\" = {0}")
    .OrderByDescending(o => o.TotalAmount)
    .Limit(6);

var r5 = q5.Build(compiler);
P(r5.Sql, r5.Parameters);

(await conn.QueryAsync<dynamic>(r5.Sql, ToParams(r5.Parameters))).ToList()
    .ForEach(row => Row($"#{row.id,-4}  {row.status,-12}  ${row.total_amount,10:N2}  {row.name}"));

// ═══════════════════════════════════════════════════════════════════════════════
// DEMO 6: GROUP BY + aggregate (SUM, COUNT)
// ═══════════════════════════════════════════════════════════════════════════════

S("6. GROUP BY + aggregate — Revenue by status");

var q6 = Sql.From<Order>()
    .Select("\"status\"")
    .RawSelect($"COUNT(*) AS order_count, SUM(\"total_amount\") AS revenue")
    .Where($"\"is_deleted\" = {0}")
    .GroupBy("\"status\"")
    .OrderBy(o => o.Status);

var r6 = q6.Build(compiler);
P(r6.Sql, r6.Parameters);

// Oracle returns column names in UPPERCASE for unquoted aliases
(await conn.QueryAsync<dynamic>(r6.Sql, ToParams(r6.Parameters))).ToList().ForEach(row =>
{
    var d = (IDictionary<string, object>)row;
    var status    = d.TryGetValue("status",     out var s) ? s : d.TryGetValue("STATUS",     out s) ? s : "?";
    var cnt       = d.TryGetValue("order_count",out var c) ? c : d.TryGetValue("ORDER_COUNT",out c) ? c : 0;
    var rev       = d.TryGetValue("revenue",    out var v) ? v : d.TryGetValue("REVENUE",    out v) ? v : 0;
    Row($"{status,-12}  {cnt} orders  ${Convert.ToDecimal(rev, System.Globalization.CultureInfo.InvariantCulture):N2}");
});

// ═══════════════════════════════════════════════════════════════════════════════
// DEMO 7: ROW_NUMBER() analytic — Top N per group
// ═══════════════════════════════════════════════════════════════════════════════

S("7. Analytic function — Top 2 products per category by price");
Info("Oracle analytics: ROW_NUMBER() OVER (PARTITION BY ... ORDER BY ...)");

// Raw SQL to demonstrate Oracle analytic window functions alongside SqlBuilder
var analyticSql = """
    SELECT "sku", "name", "price", "category_id",
           ROW_NUMBER() OVER (PARTITION BY "category_id" ORDER BY "price" DESC) AS rn
    FROM (
        SELECT "id", "sku", "name", "price", "category_id"
        FROM "products"
        WHERE "is_active" = 1
    ) ranked
    ORDER BY "category_id", rn
    FETCH FIRST 10 ROWS ONLY
""";

(await conn.QueryAsync<dynamic>(analyticSql)).ToList().ForEach(row =>
{
    var d = (IDictionary<string, object>)row;
    var sku  = d.TryGetValue("sku",  out var sk) ? sk : d["SKU"];
    var name = d.TryGetValue("name", out var nm) ? nm : d["NAME"];
    var price= d.TryGetValue("price",out var pr) ? pr : d["PRICE"];
    var rn   = d.TryGetValue("rn",   out var r)  ? r  : d["RN"];
    Row($"  Rank #{rn}  {sku,-25}  ${Convert.ToDecimal(price, System.Globalization.CultureInfo.InvariantCulture):N2}  {name}");
});

// ═══════════════════════════════════════════════════════════════════════════════
// DEMO 8: INSERT (IDENTITY column — no RETURNING in Oracle SqlBuilder INSERT)
// ═══════════════════════════════════════════════════════════════════════════════

S("8. INSERT — New customer (IDENTITY column auto-assigned)");
Info("Oracle IDENTITY (12c+): GENERATED ALWAYS AS IDENTITY");
Info("Get new ID via: SELECT ... FROM customers WHERE email = :email");

var demoEmail = $"oracle_{DateTime.UtcNow:HHmmss}@playground.dev";

var insertQ = new InsertQuery<Customer>()
    .Into("customers")
    .Values("Oracle Demo Corp", demoEmail, "+1-999-0001", 1);

var rInsert = insertQ.Build(compiler);
P(rInsert.Sql, rInsert.Parameters);

await conn.ExecuteAsync(rInsert.Sql, ToParams(rInsert.Parameters));

// Fetch the inserted record by email (Oracle doesn't support RETURNING in INSERT directly via ODP.NET without PLSQL)
var insertedCust = await conn.QuerySingleAsync<Customer>(
    "SELECT \"id\", \"name\", \"email\", \"is_active\", \"created_at\" FROM \"customers\" WHERE \"email\" = :email",
    new { email = demoEmail });

Row($"Inserted: #{insertedCust.Id}  {insertedCust.Name}  {insertedCust.Email}");
Row($"Created:  {insertedCust.CreatedAt:yyyy-MM-dd HH:mm:ss}");

// ═══════════════════════════════════════════════════════════════════════════════
// DEMO 9: MERGE INTO (Oracle UPSERT — the definitive Oracle upsert syntax)
// ═══════════════════════════════════════════════════════════════════════════════

S("9. MERGE INTO — Oracle-native UPSERT");
Info("Oracle MERGE INTO is the canonical way to do upsert — no ON CONFLICT");

var mergeSql = """
    MERGE INTO "customers" target
    USING (SELECT :email AS "email", :name AS "name" FROM DUAL) source
    ON (target."email" = source."email")
    WHEN MATCHED THEN
        UPDATE SET target."name" = source."name"
    WHEN NOT MATCHED THEN
        INSERT ("name", "email", "is_active")
        VALUES (source."name", source."email", 1)
""";

var mergeRows = await conn.ExecuteAsync(mergeSql, new { email = demoEmail, name = "Oracle Demo Corp (Merged)" });
Row($"{mergeRows} row(s) affected by MERGE (WHEN MATCHED → UPDATE)");

// Second run — insert new email → WHEN NOT MATCHED
var demoEmail2 = $"oracle_new_{DateTime.UtcNow:HHmmss}@playground.dev";
mergeRows = await conn.ExecuteAsync(mergeSql, new { email = demoEmail2, name = "Oracle Demo Corp 2" });
Row($"{mergeRows} row(s) affected by MERGE (WHEN NOT MATCHED → INSERT)");

// ═══════════════════════════════════════════════════════════════════════════════
// DEMO 10: UPDATE — SqlBuilder generated UPDATE
// ═══════════════════════════════════════════════════════════════════════════════

S("10. UPDATE — SqlBuilder-generated UPDATE statement");

var q10 = Sql.Update<Customer>()
    .Set(c => c.Name, "Oracle Demo Corp (Updated)")
    .Where($"\"email\" = {demoEmail}");

var r10 = q10.Build(compiler);
P(r10.Sql, r10.Parameters);

Val("Rows updated", await conn.ExecuteAsync(r10.Sql, ToParams(r10.Parameters)));

// ═══════════════════════════════════════════════════════════════════════════════
// DEMO 11: DELETE — SqlBuilder-generated DELETE
// ═══════════════════════════════════════════════════════════════════════════════

S("11. DELETE — Remove demo customers");

// Delete both demo emails
var q11a = Sql.Delete<Customer>().Where($"\"email\" = {demoEmail}");
var q11b = Sql.Delete<Customer>().Where($"\"email\" = {demoEmail2}");

var r11a = q11a.Build(compiler);
P(r11a.Sql, r11a.Parameters);

var deleted = await conn.ExecuteAsync(r11a.Sql, ToParams(r11a.Parameters));
deleted += await conn.ExecuteAsync(q11b.Build(compiler).Sql, ToParams(q11b.Build(compiler).Parameters));
Val("Total rows deleted", deleted);

// ═══════════════════════════════════════════════════════════════════════════════
// DEMO 12: DUAL table — arithmetic and system functions
// ═══════════════════════════════════════════════════════════════════════════════

S("12. Oracle DUAL — arithmetic, SYSDATE, sequences");
Info("Oracle requires FROM DUAL for scalar expressions");

var dualRows = await conn.QueryAsync<dynamic>("""
    SELECT
        SYSDATE                          AS current_date,
        SYSTIMESTAMP                     AS current_ts,
        1 + 1                            AS two,
        ROUND(3.14159, 2)                AS pi,
        SYS_GUID()                       AS new_guid,
        ORA_HASH('SqlBuilder', 1000000)  AS hash_value
    FROM DUAL
""");

var dRow = (IDictionary<string, object>)dualRows.First();
foreach (var kv in dRow)
{
    Row($"{kv.Key,-15} = {kv.Value}");
}

// ═══════════════════════════════════════════════════════════════════════════════
// DEMO 13: CTE (WITH clause — Oracle 9i+)
// ═══════════════════════════════════════════════════════════════════════════════

S("13. CTE — Top customers by revenue (WITH clause)");

var revenueSubquery = Sql.From<Order>()
    .Select("\"customer_id\"")
    .RawSelect($"SUM(\"total_amount\") AS total_revenue, COUNT(*) AS order_count")
    .Where($"\"is_deleted\" = {0}")
    .GroupBy("\"customer_id\"");

var q13 = Sql.From<Order>()
    .CTE("customer_revenue", revenueSubquery)
    .From("customer_revenue", "cr")
    .InnerJoin("customers", "c", "c.\"id\" = cr.\"customer_id\"")
    .Select("c.\"name\"", "cr.order_count", "cr.total_revenue")
    .OrderByDescending(o => o.TotalAmount)
    .Limit(5);

var r13 = q13.Build(compiler);
P(r13.Sql, r13.Parameters);

(await conn.QueryAsync<dynamic>(r13.Sql, ToParams(r13.Parameters))).ToList().ForEach(row =>
{
    var d = (IDictionary<string, object>)row;
    var name  = d.TryGetValue("name",         out var n) ? n : d["NAME"];
    var cnt   = d.TryGetValue("order_count",  out var c) ? c : d["ORDER_COUNT"];
    var rev   = d.TryGetValue("total_revenue",out var r) ? r : d["TOTAL_REVENUE"];
    Row($"{name,-30}  {cnt} orders  ${Convert.ToDecimal(rev, System.Globalization.CultureInfo.InvariantCulture):N2}");
});

// ═══════════════════════════════════════════════════════════════════════════════
// DONE
// ═══════════════════════════════════════════════════════════════════════════════

Console.WriteLine();
Console.WriteLine($"{Cy}{B}{"═".PadRight(70, '═')}{R}");
Console.WriteLine($"{Gn}{B}  ✓ All 13 Oracle demos completed!{R}");
Console.WriteLine($"  {Gr}EricksonLopez.SqlBuilder — Oracle Free 23c playground{R}");
Console.WriteLine($"  {Bl}Oracle quirks handled: IDENTITY, DUAL, UPPERCASE columns, :param, MERGE INTO{R}");
Console.WriteLine($"{Cy}{B}{"═".PadRight(70, '═')}{R}");
Console.WriteLine();







