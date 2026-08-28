// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.PostgreSql;
using EricksonLopez.SqlBuilder.Testing.DataBuilders;
using Xunit;

namespace EricksonLopez.SqlBuilder.PostgreSql.UnitTests;

public class CopyQueryTests
{
    private readonly PostgreSqlCompiler _compiler = new();

    [Fact]
    public void CopyQuery_DefaultConstructor_ShouldIncludeAllColumns()
    {
        var query = new CopyQuery<TestEntity>();
        var result = query.Build(_compiler);

        // Uses SqlEntityCache<TestEntity>.TableName and ColumnNames
        result.Sql.Trim().Should().Be("COPY \"testentitys\" (\"id\", \"name\", \"is_active\") FROM STDIN WITH (FORMAT BINARY)");
    }

    [Fact]
    public void CopyQuery_ColumnsConstructor_ShouldIncludeOnlySpecifiedColumns()
    {
        var query = new CopyQuery<TestEntity>(new[] { "id" });
        var result = query.Build(_compiler);

        result.Sql.Trim().Should().Be("COPY \"testentitys\" (\"id\") FROM STDIN WITH (FORMAT BINARY)");
    }
}



