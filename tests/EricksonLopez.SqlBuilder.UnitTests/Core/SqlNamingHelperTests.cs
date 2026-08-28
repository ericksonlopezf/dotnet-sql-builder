// Copyright © Erickson Lopez. MIT License.
using AwesomeAssertions;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

public class SqlNamingHelperTests
{
    [Fact]
    public void SqlNamingHelper_WhenNullOrEmpty_ReturnsOriginal()
    {
        SqlNamingHelper.ToSnakeCase(null!).Should().BeNull();
        SqlNamingHelper.ToSnakeCase("").Should().Be("");
    }

    [Fact]
    public void SqlNamingHelper_WhenNoExtraSpaces_ReturnsSameInstanceOrToLowerInvariant()
    {
        string input = "lowercase";
        var result = SqlNamingHelper.ToSnakeCase(input);
        
        // This assertion verifies that when there are no extra spaces (i.e. no upper case letters),
        // the code avoids a new string allocation and just returns ToLowerInvariant() which for 
        // an all-lowercase string is the exact same instance in modern .NET.
        Assert.Same(input, result);
        result.Should().Be("lowercase");
    }

    [Theory]
    [InlineData("CamelCase", "camel_case")]
    [InlineData("camelCase", "camel_case")]
    [InlineData("HTML", "h_t_m_l")]
    [InlineData("Already_Snake_Case", "already__snake__case")]
    public void SqlNamingHelper_ToSnakeCase_ReturnsCorrectSnakeCase(string input, string expected)
    {
        var result = SqlNamingHelper.ToSnakeCase(input);
        result.Should().Be(expected);
    }
}



