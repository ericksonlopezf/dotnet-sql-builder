// Copyright © Erickson Lopez. MIT License.
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions.Metadata;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace EricksonLopez.SqlBuilder.Abstractions.UnitTests;

public class MetadataTests
{
    [Property]
    public void ColumnToken_Index_ShouldMatch(int index)
    {
        var token = new ColumnToken(index);
        token.Index.Should().Be(index);
    }

    [Property]
    public void ColumnMetadata_Properties_ShouldMatch(int index, string name, ColumnFlags flags)
    {
        var metadata = new ColumnMetadata(index, name, flags);
        metadata.Index.Should().Be(index);
        metadata.Name.Should().Be(name);
        metadata.Flags.Should().Be(flags);
    }

    [Theory]
    [InlineData(ColumnFlags.None, ColumnFlags.PrimaryKey, false)]
    [InlineData(ColumnFlags.PrimaryKey, ColumnFlags.PrimaryKey, true)]
    [InlineData(ColumnFlags.PrimaryKey | ColumnFlags.Identity, ColumnFlags.PrimaryKey, true)]
    [InlineData(ColumnFlags.PrimaryKey | ColumnFlags.Identity, ColumnFlags.Identity, true)]
    [InlineData(ColumnFlags.Identity, ColumnFlags.PrimaryKey, false)]
    public void ColumnMetadata_HasFlag_WorksCorrectly(ColumnFlags flags, ColumnFlags flagToCheck, bool expected)
    {
        var metadata = new ColumnMetadata(0, "test", flags);
        metadata.HasFlag(flagToCheck).Should().Be(expected);
    }
}


