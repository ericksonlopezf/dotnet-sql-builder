// Copyright © Erickson Lopez. MIT License.
// ╔══════════════════════════════════════════════════════════════════════════════╗
// ║  EricksonLopez.SqlBuilder — MySQL 8.0 Playground                            ║
// ║  Demonstrates real-world SQL generation against MySQL 8.0                   ║
// ║                                                                              ║
// ║  Prerequisites:                                                              ║
// ║    docker compose up -d     (in this directory)                              ║
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
using EricksonLopez.SqlBuilder.MySql;
using EricksonLopez.SqlBuilder.Playgrounds.MySql;
using MySqlConnector;

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

H("EricksonLopez.SqlBuilder — MySQL 8.0 Playground");
Console.WriteLine($"  {Gr}Engine: MySQL 8.0 | Compiler: MySqlCompiler{R}");
Console.WriteLine($"  {Gr}Identifiers: `backtick` | Params: @p0, @p1, ...{R}");
Console.WriteLine($"  {Bl}Features: ON DUPLICATE KEY UPDATE, JSON columns, LIMIT/OFFSET{R}");

const string cs = "Server=localhost;Port=3307;Database=sqlbuilder_demo;User=demo;Password=Demo@SqlB1!;AllowPublicKeyRetrieval=true;";
var compiler = new MySqlCompiler();

Console.Write($"\n  {Ye}Connecting to MySQL...{R}");
var sw = Stopwatch.StartNew();
await using var conn = new MySqlConnection(cs);

// Retry loop for container startup
for (int i = 0; i < 20; i++)
{
    try { await conn.OpenAsync(); break; }
    catch { if (i == 19) { throw; } await Task.Delay(2000); Console.Write("."); }
}
sw.Stop();
Console.WriteLine($" {Gn}✓ Connected in {sw.ElapsedMilliseconds}ms{R}");

// ─── Create Schema ────────────────────────────────────────────────────────────
Console.Write($"  {Ye}Creating schema...{R}");

await conn.ExecuteAsync("""
    CREATE TABLE IF NOT EXISTS `categories` (
        `id`        INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
        `name`      VARCHAR(150) NOT NULL,
        `slug`      VARCHAR(150) NOT NULL UNIQUE,
        `is_active` TINYINT(1) NOT NULL DEFAULT 1
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
""");

await conn.ExecuteAsync("""
    CREATE TABLE IF NOT EXISTS `customers` (
        `id`         INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
        `name`       VARCHAR(250) NOT NULL,
        `email`      VARCHAR(320) NOT NULL UNIQUE,
        `phone`      VARCHAR(30),
        `is_active`  TINYINT(1) NOT NULL DEFAULT 1,
        `created_at` DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3)
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
""");

await conn.ExecuteAsync("""
    CREATE TABLE IF NOT EXISTS `products` (
        `id`          INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
        `category_id` INT NOT NULL,
        `name`        VARCHAR(250) NOT NULL,
        `sku`         VARCHAR(100) NOT NULL UNIQUE,
        `price`       DECIMAL(18,4) NOT NULL,
        `cost_price`  DECIMAL(18,4) NOT NULL DEFAULT 0,
        `stock`       INT NOT NULL DEFAULT 0,
        `is_active`   TINYINT(1) NOT NULL DEFAULT 1
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
""");

await conn.ExecuteAsync("""
    CREATE TABLE IF NOT EXISTS `orders` (
        `id`           INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
        `customer_id`  INT NOT NULL,
        `status`       VARCHAR(20) NOT NULL DEFAULT 'pending',
        `total_amount` DECIMAL(18,4) NOT NULL DEFAULT 0,
        `currency`     CHAR(3) NOT NULL DEFAULT 'USD',
        `is_deleted`   TINYINT(1) NOT NULL DEFAULT 0,
        `created_at`   DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
        CONSTRAINT `fk_ord_cust` FOREIGN KEY (`customer_id`) REFERENCES `customers`(`id`)
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
""");

Console.WriteLine($" {Gn}✓{R}");

// ─── Seed ─────────────────────────────────────────────────────────────────────
Console.Write($"  {Ye}Seeding data...{R}");

await conn.ExecuteAsync("""
    INSERT IGNORE INTO `categories` VALUES
        (1,'Electronics','electronics',1),(2,'Laptops','laptops',1),
        (3,'Smartphones','smartphones',1),(4,'Audio','audio',1),(5,'Gaming','gaming',1)
""");

await conn.ExecuteAsync("""
    INSERT IGNORE INTO `customers` (`name`,`email`,`phone`,`is_active`) VALUES
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
    INSERT IGNORE INTO `products` (`category_id`,`name`,`sku`,`price`,`cost_price`,`stock`,`is_active`) VALUES
        (2,'MacBook Pro 16" M4','LAPTOP-MBP16-M4',3499.0000,2800.0000,25,1),
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
    INSERT IGNORE INTO `orders` (`customer_id`,`status`,`total_amount`,`currency`,`is_deleted`) VALUES
        (1,'delivered',4698.0000,'USD',0),(1,'shipped',3748.0000,'USD',0),
        (2,'confirmed',1248.0000,'USD',0),(2,'pending',449.0000,'USD',0),
        (3,'delivered',5998.0000,'USD',0),(4,'shipped',2198.0000,'USD',0),
        (5,'cancelled',0.0000,'USD',0),(6,'confirmed',1399.0000,'USD',0),
        (7,'delivered',3698.0000,'USD',0),(8,'pending',898.0000,'USD',0)
""");

Console.WriteLine($" {Gn}✓ 10 customers, 10 products, 10 orders{R}");

// ═══ DEMOS ══════════════════════════════════════════════════════════════════════

// 1. SELECT
S("1. SELECT — Active customers");
var q1 = Sql.From<Customer>().Where(c => c.IsActive).OrderBy(c => c.Name);
var r1 = q1.Build(compiler); P(r1.Sql, r1.Parameters);
var dp1 = new DynamicParameters(); foreach(var p in r1.Parameters)
{
    dp1.Add(p.Key, p.Value);
}

(await conn.QueryAsync<Customer>(r1.Sql, dp1)).Take(5).ToList().ForEach(c => Row($"#{c.Id,-3} {c.Name,-25} {c.Email}"));

// 2. Pagination: LIMIT x OFFSET y
S("2. Pagination — LIMIT 4 OFFSET 2 (MySQL style)");
var q2 = Sql.From<Product>().OrderBy(p => p.Price).Limit(4).Offset(2);
var r2 = q2.Build(compiler); P(r2.Sql, r2.Parameters);
var dp2 = new DynamicParameters(); foreach(var p in r2.Parameters)
{
    dp2.Add(p.Key, p.Value);
}

(await conn.QueryAsync<Product>(r2.Sql, dp2)).ToList().ForEach(p => Row($"`{p.Sku}`  ${p.Price:N2}"));

// 3. GROUP BY revenue
S("3. GROUP BY — Revenue by status");
var q3 = Sql.From<Order>().Select("status").RawSelect($"COUNT(*) AS cnt, SUM(total_amount) AS revenue")
    .Where(o => !o.IsDeleted).GroupBy("status").OrderBy(o => o.Status);
var r3 = q3.Build(compiler); P(r3.Sql, r3.Parameters);
var dp3 = new DynamicParameters(); foreach(var p in r3.Parameters)
{
    dp3.Add(p.Key, p.Value);
}

(await conn.QueryAsync<dynamic>(r3.Sql, dp3)).ToList().ForEach(row => Row($"{row.status,-12}  {row.cnt} orders  ${row.revenue:N2}"));

// 4. INSERT ON DUPLICATE KEY UPDATE (MySQL-specific)
S("4. INSERT ON DUPLICATE KEY UPDATE (MySQL-specific UPSERT)");
var upsertEmail = $"mysql_demo_{DateTime.UtcNow:HHmmss}@playground.dev";
var q4 = new InsertQuery<Customer>()
    .Into("customers")
    .Values("MySQL Demo Corp", upsertEmail, "+1-999-0000", null, 1)
    .OnConflict("email")
    .DoUpdate(c => c.Name);
var r4 = q4.Build(compiler); P(r4.Sql, r4.Parameters);
var dp4 = new DynamicParameters(); foreach(var p in r4.Parameters)
{
    dp4.Add(p.Key, p.Value);
}

var rows4 = await conn.ExecuteAsync(r4.Sql, dp4);
Row($"{rows4} row(s) affected (INSERT)");

// Run it again — should trigger ON DUPLICATE KEY
rows4 = await conn.ExecuteAsync(r4.Sql, dp4);
Row($"{rows4} row(s) affected (ON DUPLICATE KEY UPDATE — 2 = update triggered)");

// 5. UPDATE
S("5. UPDATE — Change name");
var q5 = Sql.Update<Customer>().Set(c => c.Name, "MySQL Demo Corp (v2)").Where($"email = {upsertEmail}");
var r5 = q5.Build(compiler); P(r5.Sql, r5.Parameters);
var dp5 = new DynamicParameters(); foreach(var p in r5.Parameters)
{
    dp5.Add(p.Key, p.Value);
}

Val("Rows updated", await conn.ExecuteAsync(r5.Sql, dp5));

// 6. DELETE
S("6. DELETE — Remove demo customer");
var q6 = Sql.Delete<Customer>().Where($"email = {upsertEmail}");
var r6 = q6.Build(compiler); P(r6.Sql, r6.Parameters);
var dp6 = new DynamicParameters(); foreach(var p in r6.Parameters)
{
    dp6.Add(p.Key, p.Value);
}

Val("Rows deleted", await conn.ExecuteAsync(r6.Sql, dp6));

// 7. INNER JOIN
S("7. INNER JOIN — Top orders with customer");
var q7 = Sql.From<Order>()
    .InnerJoin("customers","c","c.id = orders.customer_id")
    .Select("orders.id","orders.status","orders.total_amount","c.name")
    .Where(o => !o.IsDeleted)
    .OrderByDescending(o => o.TotalAmount).Limit(5);
var r7 = q7.Build(compiler); P(r7.Sql, r7.Parameters);
var dp7 = new DynamicParameters(); foreach(var p in r7.Parameters)
{
    dp7.Add(p.Key, p.Value);
}

(await conn.QueryAsync<dynamic>(r7.Sql, dp7)).ToList().ForEach(row =>
    Row($"#{row.id,-4} {row.status,-12} ${row.total_amount,10:N2}  {row.name}"));

// ─── DONE ─────────────────────────────────────────────────────────────────────
Console.WriteLine();
Console.WriteLine($"{Cy}{B}{"═".PadRight(70,'═')}{R}");
Console.WriteLine($"{Gn}{B}  ✓ All 7 MySQL demos completed!{R}");
Console.WriteLine($"  {Gr}EricksonLopez.SqlBuilder — MySQL 8.0 playground{R}");
Console.WriteLine($"{Cy}{B}{"═".PadRight(70,'═')}{R}");
Console.WriteLine();

// ─── Entities (must be at end of top-level program file) ──────────────────────






