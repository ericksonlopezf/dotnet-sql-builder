// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Annotations;
using NSubstitute;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

public class DiffUpdateExtensionsTests
{
    public class DiffEntity : ISqlEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        
        public string GetTableName() => "DiffEntity";
        public string[] GetColumnNames() => new[] { "Id", "Name" };
        public object?[] GetValues() => new object?[] { Id, Name };
        public string[] GetAllColumnNames() => GetColumnNames();
        public object?[] GetAllValues() => GetValues();
        public IReadOnlyDictionary<string, string> GetPropertyMap() => new Dictionary<string, string>();
        public string[] GetIndexedColumns() => Array.Empty<string>();
    }

    private class NonSqlEntity
    {
        public int Id { get; set; }
    }


    [Fact]
    public void ApplyDiff_WithChanges_AddsSetNodes()
    {
        var orig = new DiffEntity { Id = 1, Name = "Old" };
        var curr = new DiffEntity { Id = 1, Name = "New" };
        var query = (UpdateQuery<DiffEntity>)Sql.Update<DiffEntity>();

        var result = query.ApplyDiff(orig, curr);
        
        var buildResult = ((ISqlQuery)result).Build(new EricksonLopez.SqlBuilder.PostgreSql.PostgreSqlCompiler());
        buildResult.Sql.Should().Contain("SET \"Name\" = @p0");
        buildResult.Parameters["p0"].Should().Be("New");
    }

    [Fact]
    public void ApplyDiff_NoChanges_ThrowsInvalidOperationException()
    {
        var orig = new DiffEntity { Id = 1, Name = "Same" };
        var curr = new DiffEntity { Id = 1, Name = "Same" };
        var query = (UpdateQuery<DiffEntity>)Sql.Update<DiffEntity>();

        Action act = () => query.ApplyDiff(orig, curr);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("No changes detected between original and current entity.");
    }

    [Fact]
    public void ApplyDiff_NotISqlEntity_ThrowsInvalidOperationException()
    {
        var orig = new NonSqlEntity { Id = 1 };
        var curr = new NonSqlEntity { Id = 2 };
        Action act = () => { var query = (UpdateQuery<NonSqlEntity>)Sql.Update<NonSqlEntity>(); };

        act.Should().Throw<TypeInitializationException>()
           .WithInnerException<InvalidOperationException>()
           .WithMessage("*does not implement ISqlEntity*");
    }

    [Fact]
    public void ApplyDiff_NotUpdateQuery_ThrowsInvalidOperationException()
    {
        var orig = new DiffEntity { Id = 1 };
        var curr = new DiffEntity { Id = 2 };
        var dummyBuilder = Substitute.For<IUpdateSetBuilder<DiffEntity>>();

        Action act = () => dummyBuilder.ApplyDiff(orig, curr);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Builder is not of type UpdateQuery<T>.");
    }

    [Fact]
    public void ApplyDiff_NullOriginal_ThrowsInvalidOperationException()
    {
        var curr = new DiffEntity { Id = 1 };
        var query = (UpdateQuery<DiffEntity>)Sql.Update<DiffEntity>();

        Action act = () => query.ApplyDiff(null!, curr);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ApplyDiff_NullCurrent_ThrowsInvalidOperationException()
    {
        var orig = new DiffEntity { Id = 1 };
        var query = (UpdateQuery<DiffEntity>)Sql.Update<DiffEntity>();

        Action act = () => query.ApplyDiff(orig, null!);

        act.Should().Throw<InvalidOperationException>();
    }
}



