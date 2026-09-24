// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Testing;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests.Core;

public class NegativeLimitOffsetTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(-5)]
    [InlineData(-100)]
    public void Limit_WhenNegative_ThrowsArgumentOutOfRangeException(int negativeLimit)
    {
        var query = Sql.From<DummyEntity>();
        Action act = () => query.Limit(negativeLimit);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Offset_WhenNegative_ThrowsArgumentOutOfRangeException(int negativeOffset)
    {
        var query = Sql.From<DummyEntity>();
        Action act = () => query.Offset(negativeOffset);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Limit_WhenZero_Succeeds()
    {
        var query = Sql.From<DummyEntity>().Limit(0);
        query.Nodes.Should().ContainSingle(n => n is EricksonLopez.SqlBuilder.Abstractions.Nodes.LimitOffsetNode);
    }

    [Fact]
    public void Offset_WhenZero_Succeeds()
    {
        var query = Sql.From<DummyEntity>().Offset(0);
        query.Nodes.Should().ContainSingle(n => n is EricksonLopez.SqlBuilder.Abstractions.Nodes.LimitOffsetNode);
    }
}
