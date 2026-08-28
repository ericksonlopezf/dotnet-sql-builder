// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Dapper;
using EricksonLopez.SqlBuilder.MySql;
using EricksonLopez.SqlBuilder.Oracle;
using EricksonLopez.SqlBuilder.PostgreSql;
using EricksonLopez.SqlBuilder.Sqlite;
using EricksonLopez.SqlBuilder.SqlServer;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using System.Threading.Tasks;
using VerifyXunit;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

public class ArchitectureTests
{
    private static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(
            typeof(Sql).Assembly,
            typeof(ISqlNode).Assembly,
            typeof(DapperExtensions).Assembly,
            typeof(PostgreSqlCompiler).Assembly,
            typeof(SqlServerCompiler).Assembly,
            typeof(MySqlCompiler).Assembly,
            typeof(SqliteCompiler).Assembly,
            typeof(OracleCompiler).Assembly)
        .Build();

    private static readonly IObjectProvider<IType> CoreLayer = Types().That().ResideInAssembly(typeof(Sql).Assembly).As("Core Layer");
    private static readonly IObjectProvider<IType> AbstractionsLayer = Types().That().ResideInAssembly(typeof(ISqlNode).Assembly).As("Abstractions Layer");
    
    private static readonly IObjectProvider<IType> PostgreSqlLayer = Types().That().ResideInAssembly(typeof(PostgreSqlCompiler).Assembly).As("PostgreSQL Layer");
    private static readonly IObjectProvider<IType> SqlServerLayer = Types().That().ResideInAssembly(typeof(SqlServerCompiler).Assembly).As("SQL Server Layer");
    private static readonly IObjectProvider<IType> MySqlLayer = Types().That().ResideInAssembly(typeof(MySqlCompiler).Assembly).As("MySQL Layer");
    private static readonly IObjectProvider<IType> SqliteLayer = Types().That().ResideInAssembly(typeof(SqliteCompiler).Assembly).As("SQLite Layer");
    private static readonly IObjectProvider<IType> OracleLayer = Types().That().ResideInAssembly(typeof(OracleCompiler).Assembly).As("Oracle Layer");

    [Fact]
    public void Abstractions_Should_Not_Depend_On_Core()
    {
        var rule = Types().That().Are(AbstractionsLayer)
            .Should().NotDependOnAny(CoreLayer.GetObjects(Architecture));

        rule.Check(Architecture);
    }

    [Fact]
    public void Core_Should_Not_Depend_On_Dialects()
    {
        var rule = Types().That().Are(CoreLayer)
            .Should().NotDependOnAny(PostgreSqlLayer.GetObjects(Architecture).Concat(SqlServerLayer.GetObjects(Architecture)).Concat(MySqlLayer.GetObjects(Architecture)).Concat(SqliteLayer.GetObjects(Architecture)).Concat(OracleLayer.GetObjects(Architecture)));

        rule.Check(Architecture);
    }

    [Fact]
    public void Dialects_Should_Not_Depend_On_Each_Other()
    {
        var pgRule = Types().That().Are(PostgreSqlLayer).Should().NotDependOnAny(SqlServerLayer.GetObjects(Architecture).Concat(MySqlLayer.GetObjects(Architecture)).Concat(SqliteLayer.GetObjects(Architecture)).Concat(OracleLayer.GetObjects(Architecture)));
        var sqlServerRule = Types().That().Are(SqlServerLayer).Should().NotDependOnAny(PostgreSqlLayer.GetObjects(Architecture).Concat(MySqlLayer.GetObjects(Architecture)).Concat(SqliteLayer.GetObjects(Architecture)).Concat(OracleLayer.GetObjects(Architecture)));
        var mySqlRule = Types().That().Are(MySqlLayer).Should().NotDependOnAny(PostgreSqlLayer.GetObjects(Architecture).Concat(SqlServerLayer.GetObjects(Architecture)).Concat(SqliteLayer.GetObjects(Architecture)).Concat(OracleLayer.GetObjects(Architecture)));
        var sqliteRule = Types().That().Are(SqliteLayer).Should().NotDependOnAny(PostgreSqlLayer.GetObjects(Architecture).Concat(SqlServerLayer.GetObjects(Architecture)).Concat(MySqlLayer.GetObjects(Architecture)).Concat(OracleLayer.GetObjects(Architecture)));
        var oracleRule = Types().That().Are(OracleLayer).Should().NotDependOnAny(PostgreSqlLayer.GetObjects(Architecture).Concat(SqlServerLayer.GetObjects(Architecture)).Concat(MySqlLayer.GetObjects(Architecture)).Concat(SqliteLayer.GetObjects(Architecture)));

        pgRule.Check(Architecture);
        sqlServerRule.Check(Architecture);
        mySqlRule.Check(Architecture);
        sqliteRule.Check(Architecture);
        oracleRule.Check(Architecture);
    }
}





