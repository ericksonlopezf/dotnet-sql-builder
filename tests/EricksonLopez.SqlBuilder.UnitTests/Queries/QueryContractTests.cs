// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

public class QueryContractTests
{
    private sealed class UserEntity : EricksonLopez.SqlBuilder.Annotations.ISqlEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int RoleId { get; set; }

        public string GetTableName() => "users";
        public string[] GetColumnNames() => new[] { "id", "name", "role_id" };
        public object?[] GetValues() => new object?[] { Id, Name, RoleId };
        public string[] GetAllColumnNames() => GetColumnNames();
        public object?[] GetAllValues() => GetValues();
        public IReadOnlyDictionary<string, string> GetPropertyMap() => new Dictionary<string, string>
        {
            { "Id", "id" }, { "Name", "name" }, { "RoleId", "role_id" }
        };
        public string[] GetIndexedColumns() => Array.Empty<string>();
    }

    [Fact]
    public void QueryContract_Record_PropertiesAndEquality()
    {
        var contract1 = new QueryContract("fp1", new[] { "users" }, new[] { "id", "name" });
        var contract2 = new QueryContract("fp1", new[] { "users" }, new[] { "id", "name" });

        contract1.Fingerprint.Should().Be("fp1");
        contract1.Tables.Should().Equal("users");
        contract1.Columns.Should().Equal("id", "name");
        (contract1 == contract2).Should().BeFalse(); // IReadOnlyList reference equality in records
    }

    [Fact]
    public void GetContract_WithFromJoinAndSelectNodes_ExtractsTablesAndColumns()
    {
        var query = Sql.From<UserEntity>()
            .InnerJoin("roles", "r", "r.id = users.role_id")
            .Select("id", "name", "role_id");

        var contract = query.GetContract();

        contract.Should().NotBeNull();
        contract.Fingerprint.Should().NotBeNullOrWhiteSpace();
        contract.Tables.Should().Equal("users", "roles");
        contract.Columns.Should().Equal("id", "name", "role_id");
    }

    [Fact]
    public void GetContract_WithoutJoinOrExplicitSelect_ExtractsTablesOnly()
    {
        var query = Sql.From<UserEntity>();

        var contract = query.GetContract();

        contract.Should().NotBeNull();
        contract.Fingerprint.Should().NotBeNullOrWhiteSpace();
        contract.Tables.Should().Equal("users");
        contract.Columns.Should().BeEmpty();
    }
}



