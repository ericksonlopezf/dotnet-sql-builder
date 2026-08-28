// Copyright © Erickson Lopez. MIT License.
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using NSubstitute;
using Xunit;

namespace EricksonLopez.SqlBuilder.Abstractions.UnitTests;

public class ISqlQueryTests
{
    public class TestSqlQuery : ISqlQuery
    {
        public SqlResult Build(ISqlCompiler compiler) => null!;
        public string? Tag => null;
    }

    [Fact]
    public void ContributeToFingerprint_DefaultImplementation_ContributesTypeName()
    {
        // Arrange
        var query = new TestSqlQuery();
        var fingerprinter = Substitute.For<IQueryFingerprinter>();

        // Act
        ((ISqlQuery)query).ContributeToFingerprint(fingerprinter);

        // Assert
        fingerprinter.Received(1).Contribute(nameof(TestSqlQuery));
    }
}


