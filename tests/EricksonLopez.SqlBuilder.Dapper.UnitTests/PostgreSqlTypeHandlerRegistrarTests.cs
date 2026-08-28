// Copyright © Erickson Lopez. MIT License.
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Dapper;
using Xunit;

namespace EricksonLopez.SqlBuilder.Dapper.UnitTests;

public class PostgreSqlTypeHandlerRegistrarTests
{
    private class DummyDto1 { }
    private class DummyDto2 { }

    [Fact]
    public void RegisterJsonbHandler_CanBeCalled()
    {
        // Act
        var ex = Record.Exception(() => PostgreSqlTypeHandlerRegistrar.RegisterJsonbHandler<DummyDto1>());

        // Assert
        ex.Should().BeNull();
    }

    [Fact]
    public void RegisterJsonbHandlers_CanBeCalledForMultiple()
    {
        // Act
        var ex = Record.Exception(() => PostgreSqlTypeHandlerRegistrar.RegisterJsonbHandlers(
            PostgreSqlTypeHandlerRegistrar.RegisterJsonbHandler<DummyDto1>,
            PostgreSqlTypeHandlerRegistrar.RegisterJsonbHandler<DummyDto2>
        ));

        // Assert
        ex.Should().BeNull();
    }
}
