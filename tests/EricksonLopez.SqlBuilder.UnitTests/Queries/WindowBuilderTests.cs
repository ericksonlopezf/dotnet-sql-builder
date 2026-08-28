// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests.Queries;

public class WindowBuilderTests
{
    private sealed class Employee : EricksonLopez.SqlBuilder.Annotations.ISqlEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Department { get; set; } = "";
        public decimal Salary { get; set; }
        public DateTime HireDate { get; set; }
        public DateTime CreatedAt { get; set; }

        public string GetTableName() => "employees";
        public string[] GetColumnNames() => new[] { "id", "name", "department", "salary", "hire_date", "created_at" };
        public object?[] GetValues() => new object?[] { Id, Name, Department, Salary, HireDate, CreatedAt };
        public string[] GetAllColumnNames() => GetColumnNames();
        public object?[] GetAllValues() => GetValues();
        public IReadOnlyDictionary<string, string> GetPropertyMap() => new Dictionary<string, string>
        {
            { "Id", "id" }, { "Name", "name" }, { "Department", "department" }, { "Salary", "salary" }, { "HireDate", "hire_date" }, { "CreatedAt", "created_at" }
        };
        public string[] GetIndexedColumns() => Array.Empty<string>();
    }

    [Fact]
    public void Rank_PartitionByOrderByDesc_GeneratesCorrectNode()
    {
        var node = Window.Rank<Employee>()
            .PartitionBy(e => e.Department)
            .OrderByDescending(e => e.Salary)
            .As("rank");

        node.FunctionName.Should().Be("RANK");
        node.PartitionByColumns.Should().ContainSingle().Which.Should().Be("department");
        node.OrderByColumns.Should().ContainSingle().Which.Should().Be("salary");
        node.OrderByDescending.Should().ContainSingle().Which.Should().BeTrue();
        node.Alias.Should().Be("rank");
        node.ColumnName.Should().BeNull();
    }

    [Fact]
    public void RowNumber_OrderBy_GeneratesCorrectNode()
    {
        var node = Window.RowNumber<Employee>()
            .OrderBy(e => e.CreatedAt)
            .As("row_num");

        node.FunctionName.Should().Be("ROW_NUMBER");
        node.ColumnName.Should().BeNull();
        node.OrderByColumns.Should().ContainSingle().Which.Should().Be("created_at");
        node.OrderByDescending.Should().ContainSingle().Which.Should().BeFalse();
        node.Alias.Should().Be("row_num");
    }

    [Fact]
    public void DenseRank_EmptyPartitionBy_GeneratesNodeWithoutPartition()
    {
        var node = Window.DenseRank<Employee>()
            .OrderBy(e => e.Salary)
            .As("dense");

        node.FunctionName.Should().Be("DENSE_RANK");
        node.PartitionByColumns.Should().BeEmpty();
    }

    [Fact]
    public void Lag_WithOffset_GeneratesNodeWithColumnAndOffset()
    {
        var node = Window.Lag<Employee, decimal>(e => e.Salary, offset: 1)
            .PartitionBy(e => e.Department)
            .OrderBy(e => e.HireDate)
            .As("prev_salary");

        node.FunctionName.Should().Be("LAG");
        node.ColumnName.Should().Be("salary");
        node.Offset.Should().Be(1);
        node.Alias.Should().Be("prev_salary");
    }

    [Fact]
    public void Sum_GeneratesCorrectAggregateNode()
    {
        var node = Window.Sum<Employee, decimal>(e => e.Salary)
            .PartitionBy(e => e.Department)
            .As("dept_total");

        node.FunctionName.Should().Be("SUM");
        node.ColumnName.Should().Be("salary");
        node.Alias.Should().Be("dept_total");
    }

    [Fact]
    public void Count_GeneratesCountStarNode()
    {
        var node = Window.Count<Employee>()
            .PartitionBy(e => e.Department)
            .As("dept_count");

        node.FunctionName.Should().Be("COUNT");
        node.ColumnName.Should().BeNull();
        node.Alias.Should().Be("dept_count");
    }

    [Fact]
    public void As_EmptyAlias_ThrowsArgumentException()
    {
        var act = () => Window.Rank<Employee>().As("");
        act.Should().Throw<ArgumentException>().WithMessage("*Alias*");
    }

    [Fact]
    public void As_NullAlias_ThrowsArgumentException()
    {
        var act = () => Window.Rank<Employee>().As(null!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void PartitionBy_NonMemberExpression_ThrowsArgumentException()
    {
        var act = () => Window.Rank<Employee>().PartitionBy(e => e.Salary + 1).As("r");
        act.Should().Throw<ArgumentException>().WithMessage("*property access*");
    }

    [Fact]
    public void WindowFunctionNode_Accept_CallsVisitor()
    {
        var node = new WindowFunctionNode(
            "RANK", null, null, null,
            Array.Empty<string>(), Array.Empty<string>(), Array.Empty<bool>(), "r");

        var visitor = new TrackingVisitor();
        node.Accept(visitor);
        visitor.WindowFunctionVisited.Should().BeTrue();
    }

    [Fact]
    public void SelectQuery_SelectWindowFunctions_AddsToQuery()
    {
        var query = Sql.From<Employee>()
            .Select(e => new { e.Id, e.Name })
            .Select(
                Window.Rank<Employee>()
                      .PartitionBy(e => e.Department)
                      .OrderByDescending(e => e.Salary)
                      .As("rank"));

        var compiler = new EricksonLopez.SqlBuilder.SqlServer.SqlServerCompiler();
        var result = compiler.Compile(query);

        result.Sql.Should().Contain("RANK()");
        result.Sql.Should().Contain("PARTITION BY");
        result.Sql.Should().Contain("[department]");
        result.Sql.Should().Contain("ORDER BY");
        result.Sql.Should().Contain("[salary] DESC");
        result.Sql.Should().Contain("AS [rank]");
    }

    [Fact]
    public void LateralJoin_GeneratesJoinLateralSql()
    {
        var sub = Sql.From<Employee>().Where(e => e.Department == "Engineering");
        var query = Sql.From<Employee>()
            .LateralJoin(sub, "eng", "TRUE");

        var compiler = new EricksonLopez.SqlBuilder.PostgreSql.PostgreSqlCompiler();
        var result = compiler.Compile(query);

        result.Sql.Should().Contain("LATERAL");
        result.Sql.Should().Contain("eng");
    }

    [Fact]
    public void Sum_WithTypedFilter_GeneratesFilterClause()
    {
        var query = Sql.From<Employee>()
            .Select(
                Window.Sum<Employee, decimal>(e => e.Salary)
                      .Filter(e => e.Salary > 50000m)
                      .PartitionBy(e => e.Department)
                      .As("high_salary_sum"));

        var compiler = new EricksonLopez.SqlBuilder.PostgreSql.PostgreSqlCompiler();
        var result = compiler.Compile(query);

        result.Sql.Should().Contain("SUM(\"salary\") FILTER (WHERE (salary > @p0)) OVER (PARTITION BY \"department\") AS \"high_salary_sum\"");
        result.Parameters.Should().ContainKey("p0").WhoseValue.Should().Be(50000m);
    }

    [Fact]
    public void Count_WithRawFilter_GeneratesFilterClause()
    {
        var query = Sql.From<Employee>()
            .Select(
                Window.Count<Employee>()
                      .Filter($"department = 'HR'")
                      .As("hr_count"));

        var compiler = new EricksonLopez.SqlBuilder.PostgreSql.PostgreSqlCompiler();
        var result = compiler.Compile(query);

        result.Sql.Should().Contain("COUNT(*) FILTER (WHERE department = 'HR') OVER () AS \"hr_count\"");
    }

    [Fact]
    public void Window_AllFunctions_ConstructExpectedNodes()
    {
        var ntile = Window.Ntile<Employee>(4).As("q");
        ntile.FunctionName.Should().Be("NTILE");
        ntile.ColumnName.Should().Be("4");

        var lead = Window.Lead<Employee, decimal>(e => e.Salary, 2, 0m).As("next_sal");
        lead.FunctionName.Should().Be("LEAD");
        lead.ColumnName.Should().Be("salary");
        lead.Offset.Should().Be(2);
        lead.DefaultValue.Should().Be(0m);

        var avg = Window.Avg<Employee, decimal>(e => e.Salary).As("avg_sal");
        avg.FunctionName.Should().Be("AVG");
        avg.ColumnName.Should().Be("salary");

        var min = Window.Min<Employee, decimal>(e => e.Salary).As("min_sal");
        min.FunctionName.Should().Be("MIN");
        min.ColumnName.Should().Be("salary");

        var max = Window.Max<Employee, decimal>(e => e.Salary).As("max_sal");
        max.FunctionName.Should().Be("MAX");
        max.ColumnName.Should().Be("salary");

        var first = Window.FirstValue<Employee, decimal>(e => e.Salary).As("first_sal");
        first.FunctionName.Should().Be("FIRST_VALUE");
        first.ColumnName.Should().Be("salary");

        var last = Window.LastValue<Employee, decimal>(e => e.Salary).As("last_sal");
        last.FunctionName.Should().Be("LAST_VALUE");
        last.ColumnName.Should().Be("salary");

        var cumeDist = Window.CumeDist<Employee>().As("cd");
        cumeDist.FunctionName.Should().Be("CUME_DIST");
        cumeDist.ColumnName.Should().BeNull();

        var pctRank = Window.PercentRank<Employee>().As("pr");
        pctRank.FunctionName.Should().Be("PERCENT_RANK");
        pctRank.ColumnName.Should().BeNull();

        var nthValue = Window.NthValue<Employee, decimal>(e => e.Salary, 3).As("nth_sal");
        nthValue.FunctionName.Should().Be("NTH_VALUE");
        nthValue.ColumnName.Should().Be("salary, 3");

        var stdDev = Window.StdDev<Employee, decimal>(e => e.Salary).As("sd");
        stdDev.FunctionName.Should().Be("STDDEV_SAMP");
        stdDev.ColumnName.Should().Be("salary");

        var variance = Window.Variance<Employee, decimal>(e => e.Salary).As("var");
        variance.FunctionName.Should().Be("VAR_SAMP");
        variance.ColumnName.Should().Be("salary");
    }

    [Fact]
    public void WindowBuilder_StringOverloadsAndRawFilter_ConstructExpectedNode()
    {
        var node = Window.RowNumber<Employee>()
            .PartitionBy("dept_code")
            .OrderBy("hire_year")
            .OrderByDescending("emp_grade")
            .Filter("salary > {0}", 50000m)
            .As("rn");

        node.PartitionByColumns.Should().Equal("dept_code");
        node.OrderByColumns.Should().Equal("hire_year", "emp_grade");
        node.OrderByDescending.Should().Equal(false, true);
        node.FilterRaw.Should().Be("salary > {0}");
        node.FilterRawArgs.Should().Equal(new object?[] { 50000m });
        node.Alias.Should().Be("rn");
    }

    [Fact]
    public void WindowBuilder_FormattableStringFilter_ConstructExpectedNode()
    {
        FormattableString formattable = $"department = {"HR"} AND salary > {60000m}";
        var node = Window.Count<Employee>()
            .Filter(formattable)
            .As("hr_high_count");

        node.FilterRaw.Should().Be("department = {0} AND salary > {1}");
        node.FilterRawArgs.Should().Equal(new object?[] { "HR", 60000m });
        node.Alias.Should().Be("hr_high_count");
    }

    [Fact]
    public void WindowBuilder_InvalidExpressions_ThrowArgumentException()
    {
        var actOrderBy = () => Window.Rank<Employee>().OrderBy(e => e.Salary + 10);
        actOrderBy.Should().Throw<ArgumentException>().WithMessage("*property access*");

        var actOrderByDesc = () => Window.Rank<Employee>().OrderByDescending(e => e.Salary + 10);
        actOrderByDesc.Should().Throw<ArgumentException>().WithMessage("*property access*");

        var actGetCol = () => Window.Sum<Employee, decimal>(e => e.Salary + 10);
        actGetCol.Should().Throw<ArgumentException>().WithMessage("*property access*");

        var actWhitespaceAlias = () => Window.Rank<Employee>().As("   ");
        actWhitespaceAlias.Should().Throw<ArgumentException>().WithMessage("Alias cannot be null or empty. (Parameter 'alias')");
    }

    private class TrackingVisitor : EricksonLopez.SqlBuilder.Abstractions.SqlVisitorBase
    {
        public bool WindowFunctionVisited { get; private set; }
        public override void Visit(WindowFunctionNode node) => WindowFunctionVisited = true;
    }
}




