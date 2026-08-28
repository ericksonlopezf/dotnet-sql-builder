// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Annotations;
using EricksonLopez.SqlBuilder.Testing;
using EricksonLopez.SqlBuilder.Testing.Domain;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests.Queries;

public class UpdateQueryCoverageTests
{

    [Fact]
    public void Update_SetEntityWithIgnoreNulls_DoesNotAddNullValues()
    {
        var query = new UpdateQuery<User>();
        var entity = new User { Id = 1, Username = null!, Email = "test@test.com" };
        var result = query.Set(entity, ignoreNulls: true);
        var nodes = ((EricksonLopez.SqlBuilder.Abstractions.IAstQuery)result).Nodes.OfType<SetNode>().ToList();
        nodes.Should().NotContain(n => n.Column == "username");
        nodes.Should().Contain(n => n.Column == "email");
    }

    [Fact]
    public void Update_SetWithInvalidExpression_ThrowsArgumentException()
    {
        var query = new UpdateQuery<User>();
        Action act = () => query.Set(x => "Invalid", "Value");
        act.Should().Throw<ArgumentException>().WithMessage("*Expression must be a member expression*");
    }
}




