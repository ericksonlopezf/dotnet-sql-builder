// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Testing;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests.Queries;

public class ThenByTests
{
    private readonly MockSqlCompiler _compiler = new();

    [Fact]
    public void ThenBy_ShouldCreateThenByNodeInAst()
    {
        var query = Sql.From<UserDto>()
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ThenByDescending(u => u.Age);

        var astQuery = (Abstractions.IAstQuery)query;
        var nodes = astQuery.Nodes.ToList();

        Assert.Contains(nodes, n => n is OrderByNode);
        Assert.Equal(2, nodes.Count(n => n is ThenByNode));

        var result = query.Build(_compiler);
        Assert.NotNull(result);
    }

    private class UserDto : EricksonLopez.SqlBuilder.Annotations.ISqlEntity
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public int Age { get; set; }

        public string GetTableName() => "userdtos";
        public string[] GetColumnNames() => new[] { "first_name", "last_name", "age" };
        public object?[] GetValues() => new object?[] { FirstName, LastName, Age };
        public string[] GetAllColumnNames() => GetColumnNames();
        public object?[] GetAllValues() => GetValues();
        public IReadOnlyDictionary<string, string> GetPropertyMap() => new Dictionary<string, string> { { "FirstName", "first_name" }, { "LastName", "last_name" }, { "Age", "age" } };
        public string[] GetIndexedColumns() => System.Array.Empty<string>();
    }
}




