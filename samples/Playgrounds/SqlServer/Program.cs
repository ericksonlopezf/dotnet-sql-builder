// Copyright © Erickson Lopez. MIT License.
// ╔══════════════════════════════════════════════════════════════════════════════╗
// ║  EricksonLopez.SqlBuilder — SQL Server 2022 Playground                      ║
// ║  Demonstrates real-world SQL generation against SQL Server 2022             ║
// ║                                                                              ║
// ║  Prerequisites:                                                              ║
// ║    docker compose up -d                                                       ║
// ║    dotnet run                                                                 ║
// ╚══════════════════════════════════════════════════════════════════════════════╝
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Annotations;
using EricksonLopez.SqlBuilder.Playgrounds.SqlServer;
using EricksonLopez.SqlBuilder.SqlServer;
using Microsoft.Data.SqlClient;

const string R = "\x1b[0m", B = "\x1b[1m", Cy = "\x1b[36m", Gn = "\x1b[32m",
             Ye = "\x1b[33m", Mg = "\x1b[35m", Gr = "\x1b[90m", Bl = "\x1b[34m";

static void H(string t)
{
    Console.WriteLine();
    Console.WriteLine($"{Cy}{B}{"═".PadRight(70,'═')}{R}");
    Console.WriteLine($"{Cy}{B}  {t}{R}");
    Console.WriteLine($"{Cy}{B}{"═".PadRight(70,'═')}{R}");
}
static void S(string s) { Console.WriteLine(); Console.WriteLine($"{Ye}{B}▶ {s}{R}"); Console.WriteLine($"{Gr}{"─".PadRight(60,'─')}{R}"); }
static void P(string sql, IReadOnlyDictionary<string,object?> p) { Console.WriteLine($"{Gr}  SQL: {Mg}{sql.Trim()}{R}"); if(p.Count>0) { Console.WriteLine($"{Gr}  Params: {string.Join(", ", p.Select(x=>$"{x.Key}={x.Value}"))}{R}"); } }
static void Row(string s) => Console.WriteLine($"  {Gn}→ {s}{R}");
static void Val(string k, object? v) => Console.WriteLine($"  {Gn}→ {k}: {B}{v}{R}");


Console.OutputEncoding = System.Text.Encoding.UTF8;
DefaultTypeMap.MatchNamesWithUnderscores = true;

H("EricksonLopez.SqlBuilder — SQL Server 2022 Playground");
Console.WriteLine($"  {Gr}Engine: SQL Server 2022 | Compiler: SqlServerCompiler{R}");
Console.WriteLine($"  {Gr}Identifiers: [bracket] | Params: @p0, @p1, ...{R}");
Console.WriteLine($"  {Bl}Features: TOP N, OFFSET FETCH, OUTPUT INSERTED, MERGE{R}");

const string cs = "Server=localhost,1434;Database=master;User Id=sa;Password=SqlBuild3r@Str0ng!;TrustServerCertificate=True;";
var compiler = new SqlServerCompiler();

Console.Write($"\n  {Ye}Connecting to SQL Server...{R}");
var sw = Stopwatch.StartNew();
await using var conn = new SqlConnection(cs);

for (int i = 0; i < 30; i++)
{
    try { await conn.OpenAsync(); break; }
    catch { if (i == 29) { throw; } await Task.Delay(3000); Console.Write("."); }
}
sw.Stop();
Console.WriteLine($" {Gn}✓ Connected in {sw.ElapsedMilliseconds}ms{R}");

// ─── Schema ───────────────────────────────────────────────────────────────────
Console.Write($"  {Ye}Creating schema...{R}");

await conn.ExecuteAsync("""
    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'categories')
    CREATE TABLE [categories] (
        [id]        INT IDENTITY(1,1) PRIMARY KEY,
        [name]      NVARCHAR(150) NOT NULL,
        [slug]      NVARCHAR(150) NOT NULL,
        [is_active] BIT NOT NULL DEFAULT 1,
        CONSTRAINT UQ_cat_slug UNIQUE ([slug])
    )
""");

await conn.ExecuteAsync("""
    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'customers')
    CREATE TABLE [customers] (
        [id]         INT IDENTITY(1,1) PRIMARY KEY,
        [name]       NVARCHAR(250) NOT NULL,
        [email]      NVARCHAR(320) NOT NULL,
        [phone]      NVARCHAR(30),
        [is_active]  BIT NOT NULL DEFAULT 1,
        [created_at] DATETIME2(3) NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT UQ_cust_email UNIQUE ([email])
    )
""");

await conn.ExecuteAsync("""
    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'products')
    CREATE TABLE [products] (
        [id]          INT IDENTITY(1,1) PRIMARY KEY,
        [category_id] INT NOT NULL,
        [name]        NVARCHAR(250) NOT NULL,
        [sku]         NVARCHAR(100) NOT NULL,
        [price]       DECIMAL(18,4) NOT NULL,
        [cost_price]  DECIMAL(18,4) NOT NULL DEFAULT 0,
        [stock]       INT NOT NULL DEFAULT 0,
        [is_active]   BIT NOT NULL DEFAULT 1,
        CONSTRAINT UQ_prod_sku UNIQUE ([sku])
    )
""");

await conn.ExecuteAsync("""
    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'orders')
    CREATE TABLE [orders] (
        [id]           INT IDENTITY(1,1) PRIMARY KEY,
        [customer_id]  INT NOT NULL,
        [status]       NVARCHAR(20) NOT NULL DEFAULT 'pending',
        [total_amount] DECIMAL(18,4) NOT NULL DEFAULT 0,
        [currency]     CHAR(3) NOT NULL DEFAULT 'USD',
        [is_deleted]   BIT NOT NULL DEFAULT 0,
        [created_at]   DATETIME2(3) NOT NULL DEFAULT GETUTCDATE()
    )
""");

Console.WriteLine($" {Gn}✓{R}");

// ─── Seed ─────────────────────────────────────────────────────────────────────
Console.Write($"  {Ye}Seeding data...{R}");

if (await conn.QuerySingleAsync<int>("SELECT COUNT(*) FROM [customers]") == 0)
{
    await conn.ExecuteAsync("""
        SET IDENTITY_INSERT [categories] ON;
        INSERT INTO [categories] ([id],[name],[slug],[is_active]) VALUES
            (1,'Electronics','electronics',1),(2,'Laptops','laptops',1),
            (3,'Smartphones','smartphones',1),(4,'Audio','audio',1),(5,'Gaming','gaming',1);
        SET IDENTITY_INSERT [categories] OFF;
    """);

    await conn.ExecuteAsync("""
        INSERT INTO [customers] ([name],[email],[phone],[is_active]) VALUES
            ('Acme Corporation','billing@acme.corp','+1-555-0100',1),
            ('Globex Industries','orders@globex.com','+1-555-0101',1),
            ('Stark Industries','tony@stark.io','+1-555-0104',1),
            ('Wayne Enterprises','bruce@wayne.biz','+1-555-0105',1),
            ('Cyberdyne Systems','support@cyberdyne.ai','+1-555-0110',0),
            ('Dunder Mifflin','michael@dundermifflin.biz','+1-555-0117',1),
            ('Pied Piper LLC','richard@piedpiper.com','+1-555-0118',1),
            ('Hooli Corp','gavin@hooli.com','+1-555-0119',1),
            ('Nakatomi Trading','billing@nakatomi.jp','+1-555-0112',1),
            ('Massive Dynamic','peter@massivedynamic.io','+1-555-0113',1)
    """);

    await conn.ExecuteAsync("""
        INSERT INTO [products] ([category_id],[name],[sku],[price],[cost_price],[stock],[is_active]) VALUES
            (2,'MacBook Pro 16 M4','LAPTOP-MBP16-M4',3499.0000,2800.0000,25,1),
            (2,'Dell XPS 15 OLED','LAPTOP-XPS15-OLED',2199.0000,1700.0000,18,1),
            (3,'iPhone 16 Pro Max','PHONE-IP16PM-256',1199.0000,900.0000,50,1),
            (3,'Samsung Galaxy S25','PHONE-SGS25U',1099.0000,820.0000,45,1),
            (4,'Sony WH-1000XM6','AUDIO-WH1000XM6',399.0000,290.0000,60,1),
            (4,'Apple AirPods Pro 3','AUDIO-APP3',249.0000,180.0000,80,1),
            (5,'PlayStation 5 Slim','GAMING-PS5S',449.0000,380.0000,30,1),
            (5,'Nintendo Switch OLED','GAMING-NSWITCH',349.0000,270.0000,35,1),
            (1,'Anker 200W Charger','ACC-ANKER200W',89.9900,55.0000,100,1),
            (2,'ThinkPad X1 Carbon','LAPTOP-TP-X1-G12',1899.0000,1500.0000,30,1)
    """);

    await conn.ExecuteAsync("""
        INSERT INTO [orders] ([customer_id],[status],[total_amount],[currency],[is_deleted]) VALUES
            (1,'delivered',4698.0000,'USD',0),(1,'shipped',3748.0000,'USD',0),
            (2,'confirmed',1248.0000,'USD',0),(2,'pending',449.0000,'USD',0),
            (3,'delivered',5998.0000,'USD',0),(4,'shipped',2198.0000,'USD',0),
            (5,'cancelled',0.0000,'USD',0),(6,'confirmed',1399.0000,'USD',0),
            (7,'delivered',3698.0000,'USD',0),(8,'pending',898.0000,'USD',0)
    """);
}

Console.WriteLine($" {Gn}✓ 10 customers, 10 products, 10 orders{R}");

// ═══ DEMOS ══════════════════════════════════════════════════════════════════════

// 1. SELECT + WHERE
S("1. SELECT — Active customers");
var q1 = Sql.From<Customer>().Where(c => c.IsActive).OrderBy(c => c.Name);
var r1 = q1.Build(compiler); P(r1.Sql, r1.Parameters);
var dp1 = new DynamicParameters(); foreach(var p in r1.Parameters)
{
    dp1.Add(p.Key, p.Value);
}

(await conn.QueryAsync<Customer>(r1.Sql, dp1)).Take(5).ToList().ForEach(c => Row($"#{c.Id,-3} {c.Name,-25} {c.Email}"));

// 2. TOP N (SQL Server specific)
S("2. TOP N — Top 5 most expensive products");
var q2 = Sql.From<Product>().Where(p => p.IsActive).OrderByDescending(p => p.Price).Limit(5);
var r2 = q2.Build(compiler); P(r2.Sql, r2.Parameters);
var dp2 = new DynamicParameters(); foreach(var p in r2.Parameters)
{
    dp2.Add(p.Key, p.Value);
}

(await conn.QueryAsync<Product>(r2.Sql, dp2)).ToList().ForEach(p => Row($"  [{p.Sku}]  ${p.Price:N2}"));

// 3. OFFSET FETCH (SQL Server 2012+)
S("3. OFFSET FETCH — Orders page 2 (skip 3, take 4)");
var q3 = Sql.From<Order>().OrderByDescending(o => o.TotalAmount).Offset(3).Limit(4);
var r3 = q3.Build(compiler); P(r3.Sql, r3.Parameters);
var dp3 = new DynamicParameters(); foreach(var p in r3.Parameters)
{
    dp3.Add(p.Key, p.Value);
}

(await conn.QueryAsync<Order>(r3.Sql, dp3)).ToList().ForEach(o => Row($"#{o.Id,-4} {o.Status,-12} ${o.TotalAmount:N2}"));

// 4. GROUP BY + aggregate
S("4. GROUP BY — Revenue by status");
var q4 = Sql.From<Order>().Select("status").RawSelect($"COUNT(*) AS cnt, SUM(total_amount) AS revenue")
    .Where(o => !o.IsDeleted).GroupBy("status").OrderBy(o => o.Status);
var r4 = q4.Build(compiler); P(r4.Sql, r4.Parameters);
var dp4 = new DynamicParameters(); foreach(var p in r4.Parameters)
{
    dp4.Add(p.Key, p.Value);
}

(await conn.QueryAsync<dynamic>(r4.Sql, dp4)).ToList().ForEach(row => Row($"{row.status,-12}  {row.cnt} orders  ${row.revenue:N2}"));

// 5. INSERT with OUTPUT INSERTED (SQL Server specific RETURNING)
S("5. INSERT OUTPUT INSERTED — New customer with auto ID");
var email5 = $"mssql_demo_{DateTime.UtcNow:HHmmss}@playground.dev";
// SQL Server uses OUTPUT INSERTED.col syntax
var insertSql = $"""
    INSERT INTO [customers] ([name],[email],[phone],[is_active])
    OUTPUT INSERTED.[id], INSERTED.[name], INSERTED.[email], INSERTED.[created_at]
    VALUES (@name, @email, @phone, @isActive)
""";
var ins5 = await conn.QuerySingleAsync<dynamic>(insertSql, new { name = "MSSQL Demo Corp", email = email5, phone = (string?)null, isActive = true });
Row($"Inserted: #{ins5.id}  {ins5.name}  {ins5.email}");

// 6. MERGE (SQL Server upsert)
S("6. MERGE — Upsert customer (SQL Server MERGE INTO)");
var mergeSql = $"""
    MERGE INTO [customers] AS target
    USING (SELECT @email AS email, @name AS name) AS source
    ON (target.[email] = source.[email])
    WHEN MATCHED THEN
        UPDATE SET [name] = @name
    WHEN NOT MATCHED THEN
        INSERT ([name],[email],[is_active]) VALUES (@name, @email, 1);
""";
var mergeRows = await conn.ExecuteAsync(mergeSql, new { email = email5, name = "MSSQL Demo Corp (Merged)" });
Row($"{mergeRows} row(s) affected by MERGE");

// 7. UPDATE
S("7. UPDATE — SqlBuilder-generated UPDATE");
var q7 = Sql.Update<Customer>().Set(c => c.IsActive, false).Where($"email = {email5}");
var r7 = q7.Build(compiler); P(r7.Sql, r7.Parameters);
var dp7 = new DynamicParameters(); foreach(var p in r7.Parameters)
{
    dp7.Add(p.Key, p.Value);
}

Val("Rows updated", await conn.ExecuteAsync(r7.Sql, dp7));

// 8. DELETE
S("8. DELETE — Remove demo customer");
var q8 = Sql.Delete<Customer>().Where($"email = {email5}");
var r8 = q8.Build(compiler); P(r8.Sql, r8.Parameters);
var dp8 = new DynamicParameters(); foreach(var p in r8.Parameters)
{
    dp8.Add(p.Key, p.Value);
}

Val("Rows deleted", await conn.ExecuteAsync(r8.Sql, dp8));

// 9. INNER JOIN
S("9. INNER JOIN — Top orders with customer");
var q9 = Sql.From<Order>()
    .InnerJoin("customers","c","c.id = orders.customer_id")
    .Select("orders.id","orders.status","orders.total_amount","c.name")
    .Where(o => !o.IsDeleted)
    .OrderByDescending(o => o.TotalAmount).Limit(5);
var r9 = q9.Build(compiler); P(r9.Sql, r9.Parameters);
var dp9 = new DynamicParameters(); foreach(var p in r9.Parameters)
{
    dp9.Add(p.Key, p.Value);
}

(await conn.QueryAsync<dynamic>(r9.Sql, dp9)).ToList().ForEach(row =>
    Row($"#{row.id,-4} {row.status,-12} ${row.total_amount,10:N2}  {row.name}"));

// ─── DONE ─────────────────────────────────────────────────────────────────────
Console.WriteLine();
Console.WriteLine($"{Cy}{B}{"═".PadRight(70,'═')}{R}");
Console.WriteLine($"{Gn}{B}  ✓ All 9 SQL Server demos completed!{R}");
Console.WriteLine($"  {Gr}EricksonLopez.SqlBuilder — SQL Server 2022 playground{R}");
Console.WriteLine($"{Cy}{B}{"═".PadRight(70,'═')}{R}");
Console.WriteLine();

// ─── Entities (must be at end of top-level program file) ──────────────────────







