// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using System;
using System.Linq;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Abstractions;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

public class SqlQueryFingerprintExtensionsTests
{
    [Fact]
    public void GetFingerprint_IdenticalQueries_ProduceSameFingerprint()
    {
        var q1 = new SelectQuery<DummyUser>().Select("id").From("users").Where(u => u.Age > 18);
        var q2 = new SelectQuery<DummyUser>().Select("id").From("users").Where(u => u.Age > 18);

        var f1 = q1.GetFingerprint();
        var f2 = q2.GetFingerprint();

        Assert.Equal(f1, f2);
    }

    [Fact]
    public void GetFingerprint_DifferentWhereValues_ProduceSameFingerprint()
    {
        // Parameter values shouldn't change the AST structure fingerprint
        var q1 = new SelectQuery<DummyUser>().Select("id").From("users").Where(u => u.Age > 18);
        var q2 = new SelectQuery<DummyUser>().Select("id").From("users").Where(u => u.Age > 25);

        var f1 = q1.GetFingerprint();
        var f2 = q2.GetFingerprint();

        Assert.Equal(f1, f2);
    }

    [Fact]
    public void GetFingerprint_DifferentTables_ProduceDifferentFingerprints()
    {
        var q1 = new SelectQuery<DummyUser>().Select("id").From("users");
        var q2 = new SelectQuery<DummyUser>().Select("id").From("admins");

        var f1 = q1.GetFingerprint();
        var f2 = q2.GetFingerprint();

        Assert.NotEqual(f1, f2);
    }

    [Fact]
    public void GetFingerprint_DifferentExpressions_ProduceDifferentFingerprints()
    {
        var q1 = new SelectQuery<DummyUser>().Select("id").From("users").Where(u => u.IsActive);
        var q2 = new SelectQuery<DummyUser>().Select("id").From("users").Where(u => u.Age > 5);

        var f1 = q1.GetFingerprint();
        var f2 = q2.GetFingerprint();

        Assert.NotEqual(f1, f2);
    }
    
    [Fact]
    public void GetFingerprint_SameExpressionDifferentConstants_ProducesSameFingerprint()
    {
        var q1 = new SelectQuery<DummyUser>().Select("id").From("users").Where(u => u.Age > 5);
        var q2 = new SelectQuery<DummyUser>().Select("id").From("users").Where(u => u.Age > 10);

        var f1 = q1.GetFingerprint();
        var f2 = q2.GetFingerprint();

        Assert.Equal(f1, f2);
    }
    
    private sealed class NonAstQueryA : ISqlQuery
    {
        public string? Tag => null;
        public SqlResult Build(ISqlCompiler compiler) => new SqlResult("SELECT 1", null);
    }

    private sealed class NonAstQueryB : ISqlQuery
    {
        public string? Tag => null;
        public SqlResult Build(ISqlCompiler compiler) => new SqlResult("SELECT 2", null);
    }

    [Fact]
    public void GetFingerprint_NonAstQuery_ContributesQueryType()
    {
        var qA = new NonAstQueryA();
        var qB = new NonAstQueryB();

        var fpA = qA.GetFingerprint();
        var fpB = qB.GetFingerprint();

        using var expectedHasherA = new QueryFingerprinter();
        expectedHasherA.Contribute(typeof(NonAstQueryA));
        var expectedA = expectedHasherA.GetFingerprint();

        Assert.Equal(expectedA, fpA);
        Assert.NotEqual(fpA, fpB);
    }

    [Fact]
    public void GetFingerprint_AstQuery_ContributesQueryType()
    {
        var q = new SelectQuery<DummyUser>().Select("id").From("users");
        var fp = q.GetFingerprint();

        using var expectedHasher = new QueryFingerprinter();
        expectedHasher.Contribute(q.GetType());
        foreach (var node in ((EricksonLopez.SqlBuilder.Abstractions.IAstQuery)q).Nodes)
        {
            node.ContributeToFingerprint(expectedHasher);
        }
        var expected = expectedHasher.GetFingerprint();

        Assert.Equal(expected, fp);
    }

    public class DummyUser
    {
        public int Id { get; set; }
        public bool IsActive { get; set; }
        public int Age { get; set; }
    }
}



