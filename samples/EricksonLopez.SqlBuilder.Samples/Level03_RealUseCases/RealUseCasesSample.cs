// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Annotations;
using EricksonLopez.SqlBuilder.Dapper;
using EricksonLopez.SqlBuilder.Sqlite;
using Microsoft.Data.Sqlite;

namespace EricksonLopez.SqlBuilder.Samples.Level03_RealUseCases;

[SqlEntity("employees")]
public partial class Employee
{
    [DatabaseGenerated] public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public int Age { get; set; }
}

[SqlEntity("departments")]
public partial class Department
{
    [DatabaseGenerated] public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

[SqlEntity("categories")]
public partial class Category
{
    [DatabaseGenerated] public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
}

public static class RealUseCasesSample
{
    public static async Task RunAsync()
    {
        Console.WriteLine("\n=== LEVEL 3: REAL USE CASES ===");

        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        DapperExtensions.RegisterCompiler<SqliteConnection>(() => new SqliteCompiler());

        await connection.ExecuteAsync(@"
            CREATE TABLE departments (id INTEGER PRIMARY KEY, name TEXT NOT NULL);
            CREATE TABLE employees (id INTEGER PRIMARY KEY, name TEXT NOT NULL, department TEXT NOT NULL, salary DECIMAL NOT NULL, age INTEGER NOT NULL DEFAULT 30);
            CREATE TABLE categories (id INTEGER PRIMARY KEY, name TEXT NOT NULL, revenue DECIMAL NOT NULL, year INTEGER NOT NULL, month INTEGER NOT NULL);
            INSERT INTO departments (id, name) VALUES (1, 'Engineering'), (2, 'HR');
            INSERT INTO employees (id, name, department, salary, age) VALUES
                (1, 'Alice', 'Engineering', 90000, 35),
                (2, 'Bob', 'Engineering', 85000, 28),
                (3, 'Charlie', 'HR', 60000, 42),
                (4, 'Diana', 'HR', 65000, 31);
            INSERT INTO categories (id, name, revenue, year, month) VALUES
                (1, 'Software', 150000, 2024, 1),
                (2, 'Hardware', 80000, 2024, 1),
                (3, 'Software', 200000, 2024, 2),
                (4, 'Services', 50000, 2024, 3);
        ");

        // ── 1. CTE + Window Functions ─────────────────────────────────────
        Console.WriteLine("\n[+] 1. Report with CTE and Window Functions");
        var cteQuery = Sql.From<Employee>()
            .Select("department", "AVG(salary) as AvgSalary")
            .GroupBy("department");
        var reportQuery = Sql.From<Employee>()
            .CTE("DeptStats", cteQuery)
            .Join("DeptStats", "ds", "employees.department = ds.department")
            .Select("employees.name", "employees.salary", "ds.AvgSalary")
            .Window("RowNumber", partitionBy: new[] { "employees.department" }, orderBy: new[] { "employees.salary DESC" });
        Console.WriteLine($"    Generated SQL:\n    {reportQuery.Build(new SqliteCompiler()).Sql}");

        // ── 2. RecursiveCTE ───────────────────────────────────────────────
        Console.WriteLine("\n[+] 2. Recursive CTE (hierarchy traversal)");
        // The CTE body is provided as a single ISqlQuery — use RawQuery for the full
        // anchor+recursive UNION ALL body:
        var seriesCteBody = Sql.Raw("SELECT 1 AS n UNION ALL SELECT n + 1 FROM series WHERE n < 5");
        var recursiveSql = Sql.From<Employee>()
            .RecursiveCTE("series", seriesCteBody)
            .Select("name").Limit(3)
            .Build(new SqliteCompiler());
        Console.WriteLine($"    Recursive CTE SQL:\n    {recursiveSql.Sql.Substring(0, Math.Min(120, recursiveSql.Sql.Length))}...");

        // ── 3. AsCount / AsSum / AsAvg / AsMin / AsMax ───────────────────
        Console.WriteLine("\n[+] 3. Aggregate Functions — AsCount / AsSum / AsAvg / AsMin / AsMax");
        var aggSql = Sql.From<Employee>()
            .AsCount("total_employees").AsSum("salary", "total_salary")
            .AsAvg("salary", "avg_salary").AsMin("salary", "min_salary").AsMax("salary", "max_salary")
            .GroupBy("department")
            .Build(new SqliteCompiler());
        Console.WriteLine($"    Aggregation SQL:\n    {aggSql.Sql}");

        // ── 4. GroupBy + Having ───────────────────────────────────────────
        Console.WriteLine("\n[+] 4. GroupBy + Having");
        var havingSql = Sql.From<Employee>()
            .Select("department").AsAvg("salary", "avg_salary")
            .GroupBy("department")
            .Having(e => e.Salary > 70000m)
            .Build(new SqliteCompiler());
        Console.WriteLine($"    HAVING (typed) SQL: {havingSql.Sql}");
        decimal threshold = 70000m;
        var rawHavingSql = Sql.From<Employee>()
            .Select("department").AsAvg("salary", "avg_salary")
            .GroupBy("department")
            .Having($"AVG(salary) > {threshold}")
            .Build(new SqliteCompiler());
        Console.WriteLine($"    HAVING (raw) SQL: {rawHavingSql.Sql}");

        // ── 5. GroupByRollup ──────────────────────────────────────────────
        Console.WriteLine("\n[+] 5. GROUP BY ROLLUP — Hierarchical subtotals");
        // NOTE: ROLLUP is not supported by SQLite. Use SqlServerCompiler or PostgreSqlCompiler.
        var rollupSql = Sql.From<Category>()
            .Select("name", "year").AsSum("revenue", "total_revenue")
            .GroupByRollup("name", "year")
            .Build(new EricksonLopez.SqlBuilder.SqlServer.SqlServerCompiler());
        Console.WriteLine($"    ROLLUP SQL (SQL Server): {rollupSql.Sql}");

        // ── 6. GroupByCube ────────────────────────────────────────────────
        Console.WriteLine("\n[+] 6. GROUP BY CUBE — All crossed dimensions");
        // NOTE: CUBE is not supported by SQLite. Use SqlServerCompiler or PostgreSqlCompiler.
        var cubeSql = Sql.From<Category>()
            .Select("name", "year").AsSum("revenue", "total_revenue")
            .GroupByCube("name", "year")
            .Build(new EricksonLopez.SqlBuilder.SqlServer.SqlServerCompiler());
        Console.WriteLine($"    CUBE SQL (SQL Server): {cubeSql.Sql}");

        // ── 7. GroupingSets ───────────────────────────────────────────────
        Console.WriteLine("\n[+] 7. GROUPING SETS — Explicit multi-level groupings");
        // NOTE: GROUPING SETS is not supported by SQLite. Use SqlServerCompiler or PostgreSqlCompiler.
        var gsQuery = Sql.From<Category>()
            .Select("name", "year").AsSum("revenue", "total_revenue")
            .GroupingSets(new[] { "name", "year" }, new[] { "name" }, System.Array.Empty<string>());
        Console.WriteLine($"    GROUPING SETS SQL (SQL Server): {gsQuery.Build(new EricksonLopez.SqlBuilder.SqlServer.SqlServerCompiler()).Sql}");

        // ── 8. WhereDate / WhereYear / WhereMonth ─────────────────────────
        Console.WriteLine("\n[+] 8. WhereDate / WhereYear / WhereMonth");
        Console.WriteLine($"    WhereYear+Month SQL: {Sql.From<Category>().WhereYear("year", "=", 2024).WhereMonth("month", ">=", 2).Build(new SqliteCompiler()).Sql}");
        Console.WriteLine($"    WhereDate SQL: {Sql.From<Category>().WhereDate("year", "=", 2024).Build(new SqliteCompiler()).Sql}");

        // ── 9. WhereColumns ───────────────────────────────────────────────
        Console.WriteLine("\n[+] 9. WhereColumns — Column-to-column comparison");
        Console.WriteLine($"    WhereColumns SQL: {Sql.From<Employee>().WhereColumns("salary", ">", "age").Build(new SqliteCompiler()).Sql}");

        // ── 10. Expression helpers: Between, ILike, Coalesce ─────────────
        Console.WriteLine("\n[+] 10. Between / ILike / Coalesce / NullIf / IsDistinctFrom");
        Console.WriteLine($"    Between SQL: {Sql.From<Employee>().Where(e => e.Age.Between(25, 40)).Build(new SqliteCompiler()).Sql}");
        Console.WriteLine($"    ILike SQL: {Sql.From<Employee>().Where(e => e.Name.ILike("%alice%")).Build(new SqliteCompiler()).Sql}");
        Console.WriteLine($"    Coalesce SQL: {Sql.From<Employee>().Where(e => e.Department.Coalesce("Unknown") == "Engineering").Build(new SqliteCompiler()).Sql}");
        Console.WriteLine("    Sql.NullIf<T>(v, t)             -> NULLIF(v, t)");
        Console.WriteLine("    Sql.IsDistinctFrom<T>(l, r)     -> l IS DISTINCT FROM r");
        Console.WriteLine("    Sql.IsNotDistinctFrom<T>(l, r)  -> l IS NOT DISTINCT FROM r");
        Console.WriteLine("    Sql.Any<T>(v, col)              -> v = ANY(col) [PostgreSQL]");
        Console.WriteLine("    Sql.All<T>(v, col)              -> v = ALL(col) [PostgreSQL]");
        Console.WriteLine("    Sql.Outer<TEntity,TProp>(col)   -> outer column reference in LATERAL");

        // ── 11. Diff Updates ──────────────────────────────────────────────
        Console.WriteLine("\n[+] 11. Diff Updates");
        var origEmp = new Employee { Id = 1, Name = "Alice", Department = "Engineering", Salary = 90000, Age = 35 };
        var updEmp  = new Employee { Id = 1, Name = "Alice", Department = "Engineering", Salary = 95000, Age = 35 };
        var diffUpdate = Sql.Update<Employee>().ApplyDiff(origEmp, updEmp).And(e => e.Id == origEmp.Id);
        Console.WriteLine($"    Diff Update SQL: {diffUpdate.Build(new SqliteCompiler()).Sql}");
        await connection.ExecuteAsync(diffUpdate);

        // ── 12. Pagination ────────────────────────────────────────────────
        Console.WriteLine("\n[+] 12. Pagination with PagedList");
        var pagedParams = EricksonLopez.Pagination.Abstractions.PaginationParameters.Create(page: 1, pageSize: 2);
        var pg = await connection.QueryPagedAsync(Sql.From<Employee>().OrderBy(e => e.Name), pagedParams);
        Console.WriteLine($"    Page 1: {pg.Count} records.");

        // ── 13. NullsPosition ─────────────────────────────────────────────
        Console.WriteLine("\n[+] 13. NullsPosition — ORDER BY with NULLs control");
        Console.WriteLine($"    NULLS FIRST: {Sql.From<Employee>().OrderBy(e => e.Name, NullsPosition.First).Build(new SqliteCompiler()).Sql}");
        Console.WriteLine($"    NULLS LAST:  {Sql.From<Employee>().OrderByDescending(e => e.Salary, NullsPosition.Last).Build(new SqliteCompiler()).Sql}");

        // ── 14. Merge / Upsert ────────────────────────────────────────────
        Console.WriteLine("\n[+] 14. Merge (Upsert / Synchronization)");
        var mergeResult = Sql.Raw("MERGE INTO employees AS tgt USING src ON tgt.id = src.id WHEN MATCHED THEN UPDATE SET salary = src.salary WHEN NOT MATCHED THEN INSERT (id,name,department,salary,age) VALUES (src.id,src.name,src.department,src.salary,src.age);")
            .Build(new EricksonLopez.SqlBuilder.SqlServer.SqlServerCompiler());
        Console.WriteLine($"    Merge SQL:\n    {mergeResult.Sql}");

        // ── 15. Sql.InsertFrom<T> — INSERT INTO ... SELECT ────────────────
        Console.WriteLine("\n[+] 15. Sql.InsertFrom<T> — INSERT INTO ... SELECT");

        // Move all HR employees to a separate archive table via INSERT INTO ... SELECT
        var hrEmployeesQuery = Sql.From<Employee>().Where(e => e.Department == "HR");
        var insertFromSql = Sql.InsertFrom<Employee>(hrEmployeesQuery, "name", "department", "salary", "age")
            .Build(new SqliteCompiler());
        Console.WriteLine($"    InsertFrom SQL:\n    {insertFromSql.Sql}");

        // InsertFrom with all columns (no explicit column list)
        var insertFromAllColsSql = Sql.InsertFrom<Employee>(hrEmployeesQuery)
            .Build(new SqliteCompiler());
        Console.WriteLine($"    InsertFrom (all cols) SQL:\n    {insertFromAllColsSql.Sql}");

        // ── 16. InsertQuery.DefaultValues() ──────────────────────────────
        Console.WriteLine("\n[+] 16. InsertQuery.DefaultValues() — INSERT with all defaults");
        var defaultValSql = new InsertQuery<Employee>().DefaultValues()
            .Build(new SqliteCompiler());
        Console.WriteLine($"    DefaultValues SQL: {defaultValSql.Sql}");

        // ── 17. WhereDay ──────────────────────────────────────────────────
        Console.WriteLine("\n[+] 17. WhereDay — EXTRACT(DAY FROM column)");
        Console.WriteLine($"    WhereDay SQL: {Sql.From<Category>().WhereDay("month", "=", 15).Build(new SqliteCompiler()).Sql}");

        // ── 18. WithTag — diagnostic tags ─────────────────────────────────
        Console.WriteLine("\n[+] 18. WithTag — attaching diagnostic tags to queries");

        // Tags allow identifying queries in logs and traces
        var taggedSelect = Sql.From<Employee>()
            .Where(e => e.Department == "Engineering")
            .WithTag("employees:list:engineering");
        Console.WriteLine($"    Tagged query tag: {taggedSelect.Tag}");
        Console.WriteLine($"    Tagged query SQL: {taggedSelect.Build(new SqliteCompiler()).Sql}");

        // Tags on InsertQuery
        var taggedInsert = Sql.Insert(new Employee { Name = "Eve", Department = "Engineering", Salary = 88000m, Age = 29 })
            .WithTag("employees:insert:onboarding");
        Console.WriteLine($"    Tagged insert tag: {taggedInsert.Tag}");

        // Tags on DeleteQuery — WithTag must be called before Where() since
        // Sql.Delete<T>() returns IDeleteFromBuilder which has WithTag on the concrete type.
        // Pattern: use typed variable or chain WithTag before Where
        var deleteQuery2 = new DeleteQuery<Employee>()
            .WithTag("employees:delete:cleanup")
            .Where(e => e.Department == "HR");
        Console.WriteLine($"    Tagged delete tag: {deleteQuery2.Tag}");

        // ── 19. IntersectAll / ExceptAll ──────────────────────────────────
        Console.WriteLine("\n[+] 19. IntersectAll / ExceptAll — set operations with duplicates");

        var highSalaryQ = Sql.From<Employee>().Where(e => e.Salary > 80000m).Select("department");
        var youngQ = Sql.From<Employee>().Where(e => e.Age < 35).Select("department");

        var intersectAllSql = Sql.From<Employee>().Select("department")
            .IntersectAll(youngQ)
            .Build(new EricksonLopez.SqlBuilder.SqlServer.SqlServerCompiler());
        Console.WriteLine($"    INTERSECT ALL SQL:\n    {intersectAllSql.Sql}");

        var exceptAllSql = highSalaryQ.ExceptAll(youngQ)
            .Build(new EricksonLopez.SqlBuilder.SqlServer.SqlServerCompiler());
        Console.WriteLine($"    EXCEPT ALL SQL:\n    {exceptAllSql.Sql}");

        // ── 20. OrHaving ──────────────────────────────────────────────────
        Console.WriteLine("\n[+] 20. OrHaving — OR condition on HAVING clause");
        var orHavingSql = Sql.From<Employee>()
            .Select("department").AsAvg("salary", "avg_salary").AsCount("total")
            .GroupBy("department")
            .Having(e => e.Salary > 80000m)
            .OrHaving(e => e.Age < 30)
            .Build(new SqliteCompiler());
        Console.WriteLine($"    OrHaving SQL: {orHavingSql.Sql}");

        // OrHaving with raw FormattableString
        decimal minSalary = 50000m;
        var orHavingRawSql = Sql.From<Employee>()
            .Select("department").AsCount("total")
            .GroupBy("department")
            .Having($"COUNT(*) > 1")
            .OrHaving($"AVG(salary) > {minSalary}")
            .Build(new SqliteCompiler());
        Console.WriteLine($"    OrHaving (raw) SQL: {orHavingRawSql.Sql}");

        // ── 21. OrExists / OrNotExists ────────────────────────────────────
        Console.WriteLine("\n[+] 21. OrExists / OrNotExists — set existential checks");
        var deptSubquery = Sql.From<Department>().Where(d => d.Id == 1);

        var orExistsSql = Sql.From<Employee>()
            .Where(e => e.Salary > 90000m)
            .OrExists(deptSubquery)
            .Build(new SqliteCompiler());
        Console.WriteLine($"    OR EXISTS SQL: {orExistsSql.Sql}");

        var whereNotExistsSql = Sql.From<Employee>()
            .Where(e => e.Department == "Engineering")
            .OrNotExists(deptSubquery)
            .Build(new SqliteCompiler());
        Console.WriteLine($"    OR NOT EXISTS SQL: {whereNotExistsSql.Sql}");

        // ── 22. Alias — naming a subquery ─────────────────────────────────
        Console.WriteLine("\n[+] 22. Alias — naming SelectQuery for use as subquery");
        // Alias() names the outer query for use as a derived table reference
        var subQ = Sql.From<Employee>().Where(e => e.Department == "Engineering").Alias("eng");
        Console.WriteLine($"    Query alias: {subQ.Alias}");
        Console.WriteLine($"    Aliased query SQL: {subQ.Build(new SqliteCompiler()).Sql}");

        // ── 23. From(ISqlQuery, alias) — subquery as FROM source ──────────
        Console.WriteLine("\n[+] 23. SelectQuery.From(ISqlQuery, alias) — derived table");
        // Build a subquery and use it as the source table
        var innerQ = Sql.From<Employee>().Select("department", "AVG(salary) as avg_sal").GroupBy("department");
        var outerSql = Sql.From<Employee>()
            .From(innerQ, "dept_stats")
            .Select("dept_stats.department", "dept_stats.avg_sal")
            .Build(new SqliteCompiler());
        Console.WriteLine($"    Derived table SQL:\n    {outerSql.Sql}");
    }
}

