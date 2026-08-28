// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

public class ParameterManagerCoverageTests
{


    [Fact]
    public void Add_WhenCalledWithNull_AddsNullValue()
    {
        var sut = new ParameterManager();
        var name = sut.Add(null);

        var parameters = sut.GetParameters();
        parameters[name.Substring(1)].Should().BeNull();
    }

    [Fact]
    public void Add_WhenCountExceeds2100_ThrowsInvalidOperationException()
    {
        var sut = new ParameterManager(maxParameters: 2100);
        
        for (int i = 0; i < 2100; i++)
        {
            sut.Add(i);
        }

        var action = () => sut.Add(2101);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Maximum number of parameters (2100) exceeded.");
    }
}


