// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Annotations;
using EricksonLopez.SqlBuilder.PostgreSql;
using EricksonLopez.SqlBuilder.Sqlite;
using EricksonLopez.SqlBuilder.SqlServer;

namespace EricksonLopez.SqlBuilder.Samples.Level00_Conceptual;

// ─── Representative entity for all concept demonstrations ────────────────────

/// <summary>
/// Minimal entity illustrating the SqlEntity + DatabaseGenerated pattern.
/// The [SqlEntity] attribute provides the table name; the source generator
/// emits all metadata (GetTableName, GetColumnNames, GetValues, etc.) at
/// compile time — no reflection is required at runtime.
/// </summary>
[SqlEntity("concept_users")]
public partial class ConceptUser
{
    /// <summary>Marks this column as database-generated (e.g. AUTOINCREMENT / IDENTITY).
    /// The column is excluded from INSERT statements automatically.</summary>
    [DatabaseGenerated] public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Age { get; set; }
    public bool IsActive { get; set; }
}

// ─── ConceptualSample ────────────────────────────────────────────────────────

/// <summary>
/// Level 0: Conceptual Foundation.
/// Demonstrates the core principles of EricksonLopez.SqlBuilder through
/// executable, self-documenting code examples. No database connection is
/// required — all examples compile the AST to SQL strings for verification.
/// </summary>
public static class ConceptualSample
{
    public static void Run()
    {
        Console.WriteLine("\n=== LEVEL 0: CONCEPTUAL FOUNDATION ===");

        DemoImmutability();
        DemoTypeSafety();
        DemoMultiDialect();
        DemoCrudEntryPoints();
        DemoRawQuery();
        DemoSqlMarkerMethods();
        DemoExpressionMarkers();
    }

    // ─── 1. IMMUTABILITY ─────────────────────────────────────────────────────

    /// <summary>
    /// Pillar 1: Immutable AST — every method call returns a NEW instance.
    /// The original query is never mutated. This makes queries safe to share
    /// across threads and reuse as base templates.
    /// </summary>
    private static void DemoImmutability()
    {
        Console.WriteLine("\n[+] 1. Immutability — base queries are never mutated");

        // Base query — can be shared safely across the application
        var baseQuery = Sql.From<ConceptUser>()
            .Where(u => u.IsActive == true);

        // Deriving two independent queries from the same base — no side effects
        var youngActiveUsers  = baseQuery.Where(u => u.Age < 30).Limit(5);
        var seniorActiveUsers = baseQuery.Where(u => u.Age >= 60).OrderBy(u => u.Age);

        var compiler = new SqliteCompiler();

        var baseSql  = baseQuery.Build(compiler).Sql;
        var youngSql = youngActiveUsers.Build(compiler).Sql;
        var seniorSql = seniorActiveUsers.Build(compiler).Sql;

        Console.WriteLine($"    Base SQL   : {baseSql}");
        Console.WriteLine($"    Young SQL  : {youngSql}");
        Console.WriteLine($"    Senior SQL : {seniorSql}");

        // Proof: base query is unchanged
        Console.WriteLine($"    [OK] Base query unchanged after derivation: {baseQuery.Build(compiler).Sql == baseSql}");
    }

    // ─── 2. TYPE SAFETY ──────────────────────────────────────────────────────

    /// <summary>
    /// Pillar 2: Type-safe lambda expressions — columns are referenced via
    /// strongly-typed expressions. Rename a property in the entity and the
    /// compiler will flag all usages. No magic strings.
    /// </summary>
    private static void DemoTypeSafety()
    {
        Console.WriteLine("\n[+] 2. Type Safety — expression trees, not magic strings");

        var compiler = new SqliteCompiler();

        // ✅ Typed WHERE — expression tree translates to parameterized SQL
        var typedQuery = Sql.From<ConceptUser>()
            .Where(u => u.Status == "active" && u.Age >= 18)
            .OrderByDescending(u => u.Age)
            .Select(u => new { u.Name, u.Age })
            .Limit(10);

        var result = typedQuery.Build(compiler);
        Console.WriteLine($"    Typed query : {result.Sql}");
        Console.WriteLine($"    Parameters  : {string.Join(", ", System.Linq.Enumerable.Select(result.Parameters, kv => $"{kv.Key}={kv.Value}"))}");

        // ✅ Typed SELECT with projection
        var projectedQuery = Sql.From<ConceptUser>()
            .Select(u => new { u.Id, u.Name })
            .Where(u => u.Id > 0);
        Console.WriteLine($"    Projection  : {projectedQuery.Build(compiler).Sql}");
    }

    // ─── 3. MULTI-DIALECT COMPILATION ────────────────────────────────────────

    /// <summary>
    /// Pillar 3: The SAME immutable AST compiles to any supported SQL dialect.
    /// Swap the compiler — the query logic stays identical.
    /// </summary>
    private static void DemoMultiDialect()
    {
        Console.WriteLine("\n[+] 3. Multi-Dialect — one AST, multiple SQL dialects");

        // Single query definition
        var query = Sql.From<ConceptUser>()
            .Where(u => u.IsActive == true)
            .OrderBy(u => u.Name)
            .Limit(5);

        // Compile to multiple dialects
        var sqliteSql   = query.Build(new SqliteCompiler()).Sql;
        var pgSql       = query.Build(new PostgreSqlCompiler()).Sql;
        var msSql       = query.Build(new SqlServerCompiler()).Sql;
        var mysqlSql    = query.Build(new EricksonLopez.SqlBuilder.MySql.MySqlCompiler()).Sql;
        var mariaDbSql  = query.Build(new EricksonLopez.SqlBuilder.MariaDb.MariaDbCompiler()).Sql;
        var oracleSql   = query.Build(new EricksonLopez.SqlBuilder.Oracle.OracleCompiler()).Sql;

        Console.WriteLine($"    SQLite      : {sqliteSql}");
        Console.WriteLine($"    PostgreSQL  : {pgSql}");
        Console.WriteLine($"    SQL Server  : {msSql}");
        Console.WriteLine($"    MySQL       : {mysqlSql}");
        Console.WriteLine($"    MariaDB     : {mariaDbSql}");
        Console.WriteLine($"    Oracle      : {oracleSql}");
    }

    // ─── 4. CRUD ENTRY POINTS ────────────────────────────────────────────────

    /// <summary>
    /// Demonstrates all four static factory methods on <see cref="Sql"/>:
    /// <c>Sql.From</c>, <c>Sql.Insert</c>, <c>Sql.Update</c>, <c>Sql.Delete</c>.
    /// Each returns an immutable query builder that compiles via <c>.Build(compiler)</c>.
    /// </summary>
    private static void DemoCrudEntryPoints()
    {
        Console.WriteLine("\n[+] 4. CRUD Entry Points — Sql.From / Insert / Update / Delete");

        var compiler = new SqliteCompiler();
        var entity   = new ConceptUser { Id = 1, Name = "Alice", Status = "active", Age = 30, IsActive = true };

        // ── SELECT ──────────────────────────────────────────────────────────
        var selectSql = Sql.From<ConceptUser>()
            .Where(u => u.Id == 1)
            .Build(compiler);
        Console.WriteLine($"    SELECT : {selectSql.Sql}");

        // ── SELECT with explicit table name ─────────────────────────────────
        var selectAltSql = Sql.From<ConceptUser>("legacy_users")
            .Where(u => u.IsActive == true)
            .Build(compiler);
        Console.WriteLine($"    SELECT (explicit table) : {selectAltSql.Sql}");

        // ── INSERT ──────────────────────────────────────────────────────────
        var insertSql = Sql.Insert(entity)
            .Build(compiler);
        Console.WriteLine($"    INSERT : {insertSql.Sql}");

        // ── BULK INSERT ──────────────────────────────────────────────────────
        var entities = new[]
        {
            new ConceptUser { Name = "Bob",   Status = "active", Age = 25, IsActive = true },
            new ConceptUser { Name = "Carol", Status = "pending", Age = 35, IsActive = false },
        };
        var bulkInsertSql = Sql.BulkInsert(entities).Build(compiler);
        Console.WriteLine($"    BULK INSERT : {bulkInsertSql.Sql}");

        // ── UPDATE — explicit SET ────────────────────────────────────────────
        var updateSql = Sql.Update<ConceptUser>()
            .Set(u => u.Status, "inactive")
            .Where(u => u.Id == 1)
            .Build(compiler);
        Console.WriteLine($"    UPDATE (Set) : {updateSql.Sql}");

        // ── UPDATE — entity SET ──────────────────────────────────────────────
        var updateEntitySql = Sql.Update(entity)
            .Where(u => u.Id == entity.Id)
            .Build(compiler);
        Console.WriteLine($"    UPDATE (entity) : {updateEntitySql.Sql}");

        // ── DELETE ───────────────────────────────────────────────────────────
        var deleteSql = Sql.Delete<ConceptUser>()
            .Where(u => u.Id == 1)
            .Build(compiler);
        Console.WriteLine($"    DELETE : {deleteSql.Sql}");

        // ── INSERT INTO ... SELECT ───────────────────────────────────────────
        var selectSource = Sql.From<ConceptUser>().Where(u => u.IsActive == false);
        var insertFromSql = Sql.InsertFrom<ConceptUser>(selectSource, "name", "status", "age", "is_active")
            .Build(compiler);
        Console.WriteLine($"    INSERT FROM SELECT : {insertFromSql.Sql}");

        // ── RAW query ────────────────────────────────────────────────────────
        var rawSql = Sql.Raw("SELECT COUNT(*) FROM concept_users WHERE is_active = @active",
                             new System.Collections.Generic.Dictionary<string, object?> { ["@active"] = true })
            .Build(compiler);
        Console.WriteLine($"    RAW : {rawSql.Sql}");
    }

    // ─── 5. RAW QUERY ────────────────────────────────────────────────────────

    /// <summary>
    /// Demonstrates both overloads of <see cref="Sql.Raw"/>:
    /// interpolated <see cref="FormattableString"/> and explicit string+params.
    /// </summary>
    private static void DemoRawQuery()
    {
        Console.WriteLine("\n[+] 5. RawQuery — safe escape hatch for complex SQL");

        var compiler = new SqliteCompiler();
        string status = "active";
        int minAge = 18;

        // Overload 1: FormattableString — parameters are extracted automatically
        var rawInterpolated = Sql.Raw($"SELECT * FROM concept_users WHERE status = {status} AND age >= {minAge}");
        Console.WriteLine($"    Interpolated : {rawInterpolated.Build(compiler).Sql}");
        Console.WriteLine($"    Parameters   : p0={status}, p1={minAge}");

        // Overload 2: string + Dictionary
        var rawDict = Sql.Raw(
            "SELECT * FROM concept_users WHERE status = @status",
            new System.Collections.Generic.Dictionary<string, object?> { ["@status"] = status });
        Console.WriteLine($"    Dict-based   : {rawDict.Build(compiler).Sql}");

        // WithTag — diagnostic tagging for telemetry
        var taggedRaw = rawInterpolated.WithTag("concept-demo");
        Console.WriteLine($"    Tag applied  : {taggedRaw.Tag}");
    }

    // ─── 6. SQL MARKER METHODS ────────────────────────────────────────────────

    /// <summary>
    /// Demonstrates the SQL expression marker methods that exist on the <see cref="Sql"/>
    /// static class. These methods are never called at runtime — they are expression
    /// markers that the <see cref="EricksonLopez.SqlBuilder.SqlExpressionVisitor"/> translates
    /// into SQL idioms inside a <c>Where()</c>, <c>Having()</c>, or <c>Select()</c> lambda.
    /// </summary>
    private static void DemoSqlMarkerMethods()
    {
        Console.WriteLine("\n[+] 6. SQL Marker Methods — expression-level SQL idioms");

        var compiler = new SqliteCompiler();

        // Sql.ILike — case-insensitive LIKE (PostgreSQL)
        var ilikeSql = Sql.From<ConceptUser>()
            .Where(u => u.Name.ILike("%alice%"))
            .Build(new PostgreSqlCompiler());
        Console.WriteLine($"    ILIKE (PG)   : {ilikeSql.Sql}");

        // Sql.Any / Sql.All — IN / ALL array operators
        int[] ids = [1, 2, 3, 4, 5];
        var anySql = Sql.From<ConceptUser>()
            .Where(u => u.Id.Any(ids))
            .Build(compiler);
        Console.WriteLine($"    ANY          : {anySql.Sql}");

        // Sql.Between — BETWEEN @from AND @to
        var betweenSql = Sql.From<ConceptUser>()
            .Where(u => u.Age.Between(18, 65))
            .Build(compiler);
        Console.WriteLine($"    BETWEEN      : {betweenSql.Sql}");

        // Sql.Coalesce — COALESCE(column, fallback)
        var coalesceExprSql = Sql.From<ConceptUser>()
            .Where(u => u.Name.Coalesce("Unknown") == "Alice")
            .Build(compiler);
        Console.WriteLine($"    COALESCE     : {coalesceExprSql.Sql}");
    }

    // ─── 7. EXPRESSION MARKERS (STATIC) ──────────────────────────────────────

    /// <summary>
    /// Demonstrates the static <see cref="Sql"/> methods used as expression markers
    /// for NULL-safe comparisons, NULLIF, and outer column references.
    /// These methods throw <see cref="InvalidOperationException"/> when invoked
    /// directly — they are intercepted by the expression visitor inside a query.
    /// </summary>
    private static void DemoExpressionMarkers()
    {
        Console.WriteLine("\n[+] 7. Static SQL Expression Markers — NullIf, IsDistinctFrom, Outer");

        // These calls WORK inside a query expression — the visitor intercepts them.
        // We demonstrate the produced SQL by building the query.

        var compiler = new PostgreSqlCompiler();

        // Sql.IsDistinctFrom — null-safe inequality
        var isDistinctSql = Sql.From<ConceptUser>()
            .Where(u => Sql.IsDistinctFrom(u.Name, "Alice"))
            .Build(compiler);
        Console.WriteLine($"    IS DISTINCT FROM : {isDistinctSql.Sql}");

        // Sql.IsNotDistinctFrom — null-safe equality
        var isNotDistinctSql = Sql.From<ConceptUser>()
            .Where(u => Sql.IsNotDistinctFrom(u.Name, "Alice"))
            .Build(compiler);
        Console.WriteLine($"    IS NOT DISTINCT FROM : {isNotDistinctSql.Sql}");

        // Sql.NullIf — NULLIF(value, target) — requires a nullable type for null comparison
        // Age is int (non-nullable); demonstrate via raw WHERE with Sql.Raw instead
        var nullIfSql = Sql.From<ConceptUser>()
            .Where($"NULLIF(age, 0) IS NOT NULL")
            .Build(compiler);
        Console.WriteLine($"    NULLIF           : {nullIfSql.Sql}");

        // Sql.RegisterTypeHandler — register once at startup (idempotent)
        // This demonstrates the API surface; actual usage is shown in Level 2.
        Console.WriteLine("    [+] Sql.RegisterTypeHandler<T> — demonstrated in Level 2");

        Console.WriteLine("\n[OK] Level 0 (Conceptual) completed.\n");
    }
}
