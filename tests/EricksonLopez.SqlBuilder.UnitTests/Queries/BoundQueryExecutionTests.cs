// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Linq;
using System.Threading;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Dapper;
using EricksonLopez.SqlBuilder.PostgreSql;
using NSubstitute;
using System.Threading.Tasks;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

public class BoundQueryExecutionTests
{
    static BoundQueryExecutionTests()
    {
        DapperExtensions.RegisterCompiler<IDbConnection>(() => new PostgreSqlCompiler());
    }

    [Fact]
    public void ConnectionSqlSelect_ReturnsBoundQuery()
    {
        var connection = Substitute.For<IDbConnection>();
        var boundQuery = connection.Sql().Select<TestUser>();

        Assert.NotNull(boundQuery.Connection);
        Assert.Equal(connection, boundQuery.Connection);
    }

    [Fact]
    public void BoundQuery_FluentChaining_PreservesConnection()
    {
        var connection = Substitute.For<IDbConnection>();

        var chained = connection
            .Sql()
            .Select<TestUser>()
            .Where(u => u.IsActive)
            .OrderBy(u => u.Name)
            .Paginate(1, 10);

        Assert.Equal(connection, chained.Connection);
    }
}



