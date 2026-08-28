// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;

namespace EricksonLopez.SqlBuilder.Analyzers.Tests
{
    public class JoinConditionAnalyzerTests
    {
        [Fact]
        public async Task Join_WithIncompatibleTypes_ReportsDiagnostic()
        {
            var code = @"

namespace TestNamespace
{
    public class User
    {
        public int Id { get; set; }
        public short ShortId { get; set; }
    }

    public class Order
    {
        public long UserId { get; set; }
        public int OrderId { get; set; }
    }

    public class Query
    {
        public Query Join<T1, T2>(Expression<Func<T1, T2, bool>> predicate) => this;
        public Query LeftJoin<T1, T2>(Expression<Func<T1, T2, bool>> predicate) => this;
        public Query RightJoin<T1, T2>(Expression<Func<T1, T2, bool>> predicate) => this;
        public Query InnerJoin<T1, T2>(Expression<Func<T1, T2, bool>> predicate) => this;
    }

    public class TestClass
    {
        public void TestMethod()
        {
            var q = new Query();
            q.Join<User, Order>((u, o) => {|#0:u.Id == o.UserId|});
            q.LeftJoin<User, Order>((u, o) => {|#1:u.ShortId == o.OrderId|});
            q.RightJoin<User, Order>((u, o) => {|#2:u.Id == o.UserId|});
            q.InnerJoin<User, Order>((u, o) => {|#3:u.ShortId == o.OrderId|});
        }
    }
}";

            var expected0 = VerifyJoinCondition.Diagnostic("ESQL006").WithLocation(0).WithArguments("int", "long");
            var expected1 = VerifyJoinCondition.Diagnostic("ESQL006").WithLocation(1).WithArguments("short", "int");
            var expected2 = VerifyJoinCondition.Diagnostic("ESQL006").WithLocation(2).WithArguments("int", "long");
            var expected3 = VerifyJoinCondition.Diagnostic("ESQL006").WithLocation(3).WithArguments("short", "int");

            await VerifyJoinCondition.VerifyAnalyzerAsync(code, expected0, expected1, expected2, expected3);
        }

        [Fact]
        public async Task Join_WithCompatibleTypes_DoesNotReportDiagnostic()
        {
            var code = @"

namespace TestNamespace
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = """";
    }

    public class Order
    {
        public int UserId { get; set; }
        public int? OptionalUserId { get; set; }
        public string UserName { get; set; } = """";
    }

    public class Query
    {
        public Query Join<T1, T2>(Expression<Func<T1, T2, bool>> predicate) => this;
    }

    public class TestClass
    {
        public void TestMethod()
        {
            var q = new Query();
            q.Join<User, Order>((u, o) => u.Id == o.UserId);
            q.Join<User, Order>((u, o) => u.Id == o.OptionalUserId);
            q.Join<User, Order>((u, o) => u.Name == o.UserName);
        }
    }
}";
            await VerifyJoinCondition.VerifyAnalyzerAsync(code);
        }
    }

    public static class VerifyJoinCondition
    {
        public static DiagnosticResult Diagnostic(string diagnosticId)
        {
            return CSharpAnalyzerVerifier<JoinConditionAnalyzer, DefaultVerifier>.Diagnostic(diagnosticId);
        }

        public static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
        {
            var test = new CSharpAnalyzerTest<JoinConditionAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                CompilerDiagnostics = CompilerDiagnostics.None,
            };
            test.TestState.Sources.Add("global using System;\nglobal using System.Collections.Generic;\nglobal using System.Linq;\nglobal using System.Linq.Expressions;\nglobal using System.Threading;\nglobal using System.Threading.Tasks;\n");
            
            
            test.ExpectedDiagnostics.AddRange(expected);
            return test.RunAsync();
        }
    }
}



