// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Testing;
using EricksonLopez.SqlBuilder.Testing.Domain;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

public class UpdateQueryTests
{

    [Fact]
    public void WhereExists_SetsIsNotAndIsOrToFalse()
    {
        var subquery = Sql.From<User>();
        var query = new UpdateQuery<User>().WhereExists(subquery);

        var node = query.Nodes.OfType<ExistsWhereNode>().Single();
        node.IsNot.Should().BeFalse();
        node.IsOr.Should().BeFalse();
    }

    [Fact]
    public void WhereNotExists_SetsIsNotToTrueAndIsOrToFalse()
    {
        var subquery = Sql.From<User>();
        var query = new UpdateQuery<User>().WhereNotExists(subquery);

        var node = query.Nodes.OfType<ExistsWhereNode>().Single();
        node.IsNot.Should().BeTrue();
        node.IsOr.Should().BeFalse();
    }

    [Fact]
    public void WithConcurrencyToken_LongType_SetsAutoIncrementTrue()
    {
        var query = new UpdateQuery<DummyEntity>().WithConcurrencyToken(x => x.Version, 1);

        var node = query.Nodes.OfType<ConcurrencyTokenNode>().Single();
        node.AutoIncrement.Should().BeTrue();
    }

    [Fact]
    public void WithConcurrencyTokenExplicitValue_InvalidExpression_ThrowsArgumentException()
    {
        var query = new UpdateQuery<DummyEntity>();

        // Expression is not a MemberExpression (it's a ConstantExpression)
        Action act = () => query.WithConcurrencyToken(x => Guid.NewGuid(), Guid.Empty, Guid.NewGuid());

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Expression must be a member expression (e.g. x => x.Version)*")
            .And.ParamName.Should().Be("tokenSelector");
    }

    [Fact]
    public void WithConcurrencyTokenExplicitValue_SetsAutoIncrementFalse()
    {
        var guid = Guid.NewGuid();
        var query = new UpdateQuery<DummyEntity>().WithConcurrencyToken(x => x.RowGuid, Guid.Empty, guid);

        var node = query.Nodes.OfType<ConcurrencyTokenNode>().Single();
        node.AutoIncrement.Should().BeFalse();
        node.NewValue.Should().Be(guid);
    }
}


