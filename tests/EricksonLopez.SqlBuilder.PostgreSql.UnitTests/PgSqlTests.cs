// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.PostgreSql;
using Xunit;

namespace EricksonLopez.SqlBuilder.PostgreSql.UnitTests;

public class PgSqlTests
{
    [Fact]
    public void ILike_ShouldThrowInvalidOperationException()
    {
        Action act = () => PgSql.ILike("column", "pattern");
        act.Should().Throw<InvalidOperationException>().WithMessage("PgSql.ILike is for SQL expression building only.");
    }

    [Fact]
    public void Any_ShouldThrowInvalidOperationException()
    {
        Action act = () => PgSql.Any(1, new List<int>());
        act.Should().Throw<InvalidOperationException>().WithMessage("PgSql.Any is for SQL expression building only.");
    }

    [Fact]
    public void All_ShouldThrowInvalidOperationException()
    {
        Action act = () => PgSql.All(1, new List<int>());
        act.Should().Throw<InvalidOperationException>().WithMessage("PgSql.All is for SQL expression building only.");
    }
}



