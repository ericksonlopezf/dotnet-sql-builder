// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Annotations;

namespace EricksonLopez.SqlBuilder.Testing.UnitTests;

public class TestingUser : ISqlEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public bool IsActive { get; set; }

    public string GetTableName() => "testingusers";
    public string[] GetColumnNames() => new[] { "id", "name", "age", "is_active" };
    public object?[] GetValues() => new object?[] { Id, Name, Age, IsActive };
    public string[] GetAllColumnNames() => GetColumnNames();
    public object?[] GetAllValues() => GetValues();
    public IReadOnlyDictionary<string, string> GetPropertyMap() => new Dictionary<string, string>
    {
        { "Id", "id" }, { "Name", "name" }, { "Age", "age" }, { "IsActive", "is_active" }
    };
    public string[] GetIndexedColumns() => System.Array.Empty<string>();
}

public class StubQuery : IAstQuery
{
    private readonly string _sql;
    private readonly IReadOnlyDictionary<string, object> _parameters;
    private readonly IReadOnlyList<ISqlNode> _nodes;

    public StubQuery(string sql, IReadOnlyDictionary<string, object>? parameters = null, IReadOnlyList<ISqlNode>? nodes = null)
    {
        _sql = sql;
        _parameters = parameters ?? new Dictionary<string, object>();
        _nodes = nodes ?? Array.Empty<ISqlNode>();
    }

    public string? Tag => null;
    public IReadOnlyList<ISqlNode> Nodes => _nodes;
    public SqlResult Build(ISqlCompiler compiler) => new(_sql, _parameters);
}
