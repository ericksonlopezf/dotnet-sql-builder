// Copyright © Erickson Lopez. MIT License.
using System;
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

    [Theory]
    [InlineData("=")]
    [InlineData("<>")]
    [InlineData("!=")]
    [InlineData("<")]
    [InlineData(">")]
    [InlineData("<=")]
    [InlineData(">=")]
    [InlineData("LIKE")]
    [InlineData("ILIKE")]
    [InlineData("NOT LIKE")]
    [InlineData("NOT ILIKE")]
    [InlineData("IS")]
    [InlineData("IS NOT")]
    public void ValidateOperator_AllAllowedOperators_DoNotThrow(string op)
    {
        var act = () => SqlNamingHelper.ValidateOperator(op, "testParam");
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateOperator_NullOrWhitespace_ThrowsArgumentException(string? op)
    {
        var ex = Assert.Throws<ArgumentException>(() => SqlNamingHelper.ValidateOperator(op!, "testParam"));
        ex.ParamName.Should().Be("testParam");
        ex.Message.Should().Contain("Operator cannot be null or whitespace.");
    }

    [Theory]
    [InlineData("UNSUPPORTED")]
    [InlineData("SELECT")]
    [InlineData("DROP")]
    [InlineData("&&")]
    public void ValidateOperator_UnsupportedOperator_ThrowsArgumentException(string op)
    {
        var ex = Assert.Throws<ArgumentException>(() => SqlNamingHelper.ValidateOperator(op, "testParam"));
        ex.ParamName.Should().Be("testParam");
        ex.Message.Should().Contain($"Operator '{op}' is not supported or contains invalid characters.");
    }

    [Theory]
    [InlineData("all")]
    [InlineData("analyse")]
    [InlineData("analyze")]
    [InlineData("and")]
    [InlineData("any")]
    [InlineData("array")]
    [InlineData("as")]
    [InlineData("asc")]
    [InlineData("asymmetric")]
    [InlineData("authorization")]
    [InlineData("binary")]
    [InlineData("both")]
    [InlineData("case")]
    [InlineData("cast")]
    [InlineData("check")]
    [InlineData("collate")]
    [InlineData("collation")]
    [InlineData("column")]
    [InlineData("concurrently")]
    [InlineData("constraint")]
    [InlineData("create")]
    [InlineData("cross")]
    [InlineData("current_catalog")]
    [InlineData("current_date")]
    [InlineData("current_role")]
    [InlineData("current_schema")]
    [InlineData("current_time")]
    [InlineData("current_timestamp")]
    [InlineData("current_user")]
    [InlineData("default")]
    [InlineData("deferrable")]
    [InlineData("desc")]
    [InlineData("distinct")]
    [InlineData("do")]
    [InlineData("else")]
    [InlineData("end")]
    [InlineData("except")]
    [InlineData("false")]
    [InlineData("fetch")]
    [InlineData("for")]
    [InlineData("foreign")]
    [InlineData("freeze")]
    [InlineData("from")]
    [InlineData("full")]
    [InlineData("grant")]
    [InlineData("group")]
    [InlineData("having")]
    [InlineData("ilike")]
    [InlineData("in")]
    [InlineData("initially")]
    [InlineData("inner")]
    [InlineData("intersect")]
    [InlineData("into")]
    [InlineData("is")]
    [InlineData("isnull")]
    [InlineData("join")]
    [InlineData("lateral")]
    [InlineData("leading")]
    [InlineData("left")]
    [InlineData("like")]
    [InlineData("limit")]
    [InlineData("localtime")]
    [InlineData("localtimestamp")]
    [InlineData("natural")]
    [InlineData("not")]
    [InlineData("notnull")]
    [InlineData("null")]
    [InlineData("offset")]
    [InlineData("on")]
    [InlineData("only")]
    [InlineData("or")]
    [InlineData("order")]
    [InlineData("outer")]
    [InlineData("overlaps")]
    [InlineData("placing")]
    [InlineData("primary")]
    [InlineData("references")]
    [InlineData("returning")]
    [InlineData("right")]
    [InlineData("select")]
    [InlineData("session_user")]
    [InlineData("similar")]
    [InlineData("some")]
    [InlineData("symmetric")]
    [InlineData("table")]
    [InlineData("tablesample")]
    [InlineData("then")]
    [InlineData("to")]
    [InlineData("trailing")]
    [InlineData("true")]
    [InlineData("union")]
    [InlineData("unique")]
    [InlineData("user")]
    [InlineData("using")]
    [InlineData("variadic")]
    [InlineData("verbose")]
    [InlineData("when")]
    [InlineData("where")]
    [InlineData("window")]
    [InlineData("with")]
    public void IsReservedKeyword_AllReservedKeywords_ReturnTrue(string kw)
    {
        SqlNamingHelper.IsReservedKeyword(kw).Should().BeTrue();
        SqlNamingHelper.IsReservedKeyword(kw.ToUpperInvariant()).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("my_custom_column")]
    [InlineData("normal_table")]
    public void IsReservedKeyword_NonReserved_ReturnsFalse(string? kw)
    {
        SqlNamingHelper.IsReservedKeyword(kw!).Should().BeFalse();
    }

    [Theory]
    [InlineData("id")]
    [InlineData("first_name")]
    [InlineData("col123")]
    public void ValidateIdentifier_Valid_DoesNotThrow(string id)
    {
        var act = () => SqlNamingHelper.ValidateIdentifier(id, "param1");
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateIdentifier_NullOrWhitespace_ThrowsArgumentException(string? id)
    {
        var ex = Assert.Throws<ArgumentException>(() => SqlNamingHelper.ValidateIdentifier(id!, "param1"));
        ex.ParamName.Should().Be("param1");
        ex.Message.Should().Contain("Identifier cannot be null or whitespace.");
    }

    [Theory]
    [InlineData("col;drop")]
    [InlineData("col'test")]
    [InlineData("col\"test")]
    [InlineData("col`test")]
    [InlineData("col/test")]
    [InlineData("col\\test")]
    [InlineData("col-test")]
    [InlineData("col\0test")]
    [InlineData("col(test)")]
    [InlineData("col=test")]
    [InlineData("col test")]
    public void ValidateIdentifier_InvalidChars_ThrowsArgumentException(string id)
    {
        var ex = Assert.Throws<ArgumentException>(() => SqlNamingHelper.ValidateIdentifier(id, "param1"));
        ex.ParamName.Should().Be("param1");
        ex.Message.Should().Contain($"Identifier '{id}' contains invalid characters.");
    }
}
