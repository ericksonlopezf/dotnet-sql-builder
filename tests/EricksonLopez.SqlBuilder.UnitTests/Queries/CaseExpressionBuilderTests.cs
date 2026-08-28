// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Builders;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests.Queries;

public class CaseExpressionBuilderTests
{
    [Fact]
    public void Build_CompleteCaseExpression_ConstructsCaseNodeWithCorrectProperties()
    {
        var builder = new CaseExpressionBuilder()
            .When("status = {0}", 1).Then("'Active'")
            .When("status = {0}", 2).Then("{0}", "Inactive")
            .Else("'Unknown'")
            .As("status_label");

        CaseNode node = builder; // Test implicit conversion

        node.Branches.Should().HaveCount(2);

        node.Branches[0].WhenSql.Should().Be("status = {0}");
        node.Branches[0].WhenParameters.Should().Equal(new object?[] { 1 });
        node.Branches[0].ThenSql.Should().Be("'Active'");
        node.Branches[0].ThenParameters.Should().BeNull();

        node.Branches[1].WhenSql.Should().Be("status = {0}");
        node.Branches[1].WhenParameters.Should().Equal(new object?[] { 2 });
        node.Branches[1].ThenSql.Should().Be("{0}");
        node.Branches[1].ThenParameters.Should().Equal(new object?[] { "Inactive" });

        node.ElseSql.Should().Be("'Unknown'");
        node.ElseParameters.Should().BeNull();
        node.Alias.Should().Be("status_label");
    }

    [Fact]
    public void Build_WithElseParameters_StoresElseParameters()
    {
        var node = new CaseExpressionBuilder()
            .When("age >= 18").Then("'Adult'")
            .Else("{0}", "Minor")
            .Build();

        node.ElseSql.Should().Be("{0}");
        node.ElseParameters.Should().Equal(new object?[] { "Minor" });
        node.Alias.Should().BeNull();
    }

    [Fact]
    public void Then_WithoutPrecedingWhen_ThrowsInvalidOperationException()
    {
        var builder = new CaseExpressionBuilder();
        var act = () => builder.Then("'Active'");
        act.Should().Throw<InvalidOperationException>().WithMessage("*Call When() before Then()*");
    }

    [Fact]
    public void Build_WithNoBranches_ThrowsInvalidOperationException()
    {
        var builder = new CaseExpressionBuilder().Else("'Default'");
        var act = () => builder.Build();
        act.Should().Throw<InvalidOperationException>().WithMessage("*at least one WHEN*");
    }
}


