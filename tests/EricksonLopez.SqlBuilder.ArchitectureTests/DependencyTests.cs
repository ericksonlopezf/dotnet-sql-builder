// Copyright © Erickson Lopez. MIT License.
using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.MySql;
using EricksonLopez.SqlBuilder.Oracle;
using EricksonLopez.SqlBuilder.PostgreSql;
using EricksonLopez.SqlBuilder.Sqlite;
using EricksonLopez.SqlBuilder.SqlServer;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using Xunit;

namespace EricksonLopez.SqlBuilder.ArchitectureTests;

public class DependencyTests
{
    private static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(
            typeof(ISqlQuery).Assembly, // Abstractions
            typeof(SqlCompilerBase).Assembly, // Core
            typeof(SqlServerCompiler).Assembly,
            typeof(PostgreSqlCompiler).Assembly,
            typeof(SqliteCompiler).Assembly,
            typeof(MySqlCompiler).Assembly,
            typeof(OracleCompiler).Assembly
        )
        .Build();

    private readonly IObjectProvider<IType> _abstractionsLayer =
        Types().That().ResideInAssembly("EricksonLopez.SqlBuilder.Abstractions").As("Abstractions Layer");
        
    private readonly IObjectProvider<IType> _coreLayer =
        Types().That().ResideInAssembly("EricksonLopez.SqlBuilder").As("Core Layer");

    private readonly IObjectProvider<IType> _dialectLayers =
        Types().That().ResideInAssembly("EricksonLopez.SqlBuilder.*Sql*")
        .Or().ResideInAssembly("EricksonLopez.SqlBuilder.Oracle").As("Dialect Layers");

    [Fact]
    public void CoreLayer_ShouldNotDependOn_DialectLayers()
    {
        var rule = Classes().That().Are(_coreLayer)
            .Should().NotDependOnAny(_dialectLayers).WithoutRequiringPositiveResults();
            
        rule.Check(Architecture);
    }
    
    [Fact]
    public void AbstractionsLayer_ShouldNotDependOn_CoreLayer()
    {
        var rule = Classes().That().Are(_abstractionsLayer)
            .Should().NotDependOnAny(_coreLayer).WithoutRequiringPositiveResults();
            
        rule.Check(Architecture);
    }
    
    [Fact]
    public void AbstractionsLayer_ShouldNotDependOn_DialectLayers()
    {
        var rule = Classes().That().Are(_abstractionsLayer)
            .Should().NotDependOnAny(_dialectLayers).WithoutRequiringPositiveResults();
            
        rule.Check(Architecture);
    }
    
    [Fact]
    public void CoreLayer_ShouldNotDependOn_EntityFrameworkCore()
    {
        var rule = Classes().That().Are(_coreLayer)
            .Should().NotDependOnAny(Types().That().ResideInNamespace("Microsoft.EntityFrameworkCore")).WithoutRequiringPositiveResults();
            
        rule.Check(Architecture);
    }
    
    [Fact]
    public void CoreLayer_ShouldNotDependOn_Dapper()
    {
        var rule = Classes().That().Are(_coreLayer)
            .Should().NotDependOnAny(Types().That().ResideInNamespace("Dapper")).WithoutRequiringPositiveResults();
            
        rule.Check(Architecture);
    }

    [Fact]
    public void AbstractionsLayer_ShouldNotDependOn_Pagination()
    {
        var rule = Classes().That().Are(_abstractionsLayer)
            .Should().NotDependOnAny(Types().That().ResideInNamespace("EricksonLopez.Pagination.*")).WithoutRequiringPositiveResults();
            
        rule.Check(Architecture);
    }

    [Fact]
    public void CoreLayer_ShouldNotDependOn_Pagination()
    {
        var rule = Classes().That().Are(_coreLayer)
            .Should().NotDependOnAny(Types().That().ResideInNamespace("EricksonLopez.Pagination.*")).WithoutRequiringPositiveResults();
            
        rule.Check(Architecture);
    }
}


