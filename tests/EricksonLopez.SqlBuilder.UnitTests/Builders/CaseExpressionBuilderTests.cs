// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using System;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Builders;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

public class CaseExpressionBuilderTests
{
    [Fact]
    public void Build_WithoutWhen_ThrowsInvalidOperationException()
    {
        var builder = new CaseExpressionBuilder();
        Action act = () => builder.Build();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("A CASE expression requires at least one WHEN ... THEN ... branch.");
    }

    [Fact]
    public void Build_WhenWithoutThen_ThrowsInvalidOperationException()
    {
        var builder = new CaseExpressionBuilder().When("status = {0}", 1);
        Action act = () => builder.Then("2");
        // Wait, Then() validates that When() was called, but if we call Then twice?
        var builder2 = new CaseExpressionBuilder();
        Action act2 = () => builder2.Then("2");
        act2.Should().Throw<InvalidOperationException>()
            .WithMessage("Call When() before Then().");
    }

    [Fact]
    public void Build_ValidCaseExpression_ReturnsNode()
    {
        var builder = new CaseExpressionBuilder()
            .When("status = {0}", 1).Then("'Active'", 10)
            .When("status = {0}", 2).Then("'Inactive'")
            .Else("'Unknown'", 20)
            .As("status_label");

        var node = builder.Build();

        node.Branches.Should().HaveCount(2);
        node.Branches[0].WhenSql.Should().Be("status = {0}");
        node.Branches[0].WhenParameters.Should().BeEquivalentTo(new object[] { 1 });
        node.Branches[0].ThenSql.Should().Be("'Active'");
        node.Branches[0].ThenParameters.Should().BeEquivalentTo(new object[] { 10 });

        node.Branches[1].WhenSql.Should().Be("status = {0}");
        node.Branches[1].WhenParameters.Should().BeEquivalentTo(new object[] { 2 });
        node.Branches[1].ThenSql.Should().Be("'Inactive'");
        node.Branches[1].ThenParameters.Should().BeNull();

        node.ElseSql.Should().Be("'Unknown'");
        node.ElseParameters.Should().BeEquivalentTo(new object[] { 20 });
        node.Alias.Should().Be("status_label");
    }

    [Fact]
    public void Build_WithoutParameters_SetsNulls()
    {
        var builder = new CaseExpressionBuilder()
            .When("1=1").Then("1")
            .Else("0");
        
        var node = builder.Build();
        
        node.Branches[0].WhenParameters.Should().BeNull();
        node.Branches[0].ThenParameters.Should().BeNull();
        node.ElseParameters.Should().BeNull();
    }
}



