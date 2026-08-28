// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Testing;
using EricksonLopez.SqlBuilder.Testing.DataBuilders;
using EricksonLopez.SqlBuilder.Testing.Domain;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

public class QueryImmutabilityTests
{
    [Fact]
    public void SelectQuery_WhenApplyingMethods_ShouldReturnNewInstances()
    {
        // Arrange
        var q1 = Sql.From<User>();

        // Act
        var q2 = q1.Select(u => u.Id);
        var q3 = q2.Where(u => u.Id == 1);
        
        // Assert
        q1.Should().NotBeSameAs(q2);
        q2.Should().NotBeSameAs(q3);
    }

    [Fact]
    public void InsertQuery_WhenApplyingMethods_ShouldReturnNewInstances()
    {
        // Arrange
        var q1 = Sql.Insert(ObjectMother.CreateUser());

        // Act
        var q2 = q1.Into("users");
        
        // Assert
        q1.Should().NotBeSameAs(q2);
    }

    [Fact]
    public void UpdateQuery_WhenApplyingMethods_ShouldReturnNewInstances()
    {
        // Arrange
        var q1 = Sql.Update<User>();

        // Act
        var q2 = q1.Set(u => u.Username, "Test");
        
        // Assert
        q1.Should().NotBeSameAs(q2);
    }

    [Fact]
    public void DeleteQuery_WhenApplyingMethods_ShouldReturnNewInstances()
    {
        // Arrange
        var q1 = Sql.Delete<User>();

        // Act
        var q2 = q1.Where(u => u.Id == 1);
        
        // Assert
        q1.Should().NotBeSameAs(q2);
    }
}






