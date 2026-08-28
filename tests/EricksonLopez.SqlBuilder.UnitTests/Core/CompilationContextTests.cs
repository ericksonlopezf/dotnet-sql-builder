// Copyright © Erickson Lopez. MIT License.
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using NSubstitute;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

public class CompilationContextTests
{
    [Fact]
    public void CompilationContext_Dispose_ReturnsSqlBuilderToPool()
    {
        // Arrange
        var parameters = Substitute.For<IParameterManager>();
        
        // Ensure pool is somewhat empty by renting one out, but not really needed 
        // if we just check the internal effect on a StringBuilder.
        var context = new CompilationContext(parameters);
        
        // Keep reference to the builder
        var sqlBuilder = context.Sql;
        sqlBuilder.Append("something");

        // Act
        context.Dispose();

        // Assert
        // When returned to the pool, the policy clears the StringBuilder
        // Kills mutation: "Statement mutation" on StringBuilderPool.Return(Sql)
        sqlBuilder.Length.Should().Be(0);
    }
}


