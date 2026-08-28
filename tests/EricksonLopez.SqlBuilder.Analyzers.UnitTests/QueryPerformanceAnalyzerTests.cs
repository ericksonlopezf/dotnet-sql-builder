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
    public class QueryPerformanceAnalyzerTests
    {
        [Fact]
        public async Task ToStringOrToUpperOrToLower_InsideSqlBuilderQueryLambda_ReportsDiagnostic()
        {
            var code = @"

namespace EricksonLopez.SqlBuilder
{
    public class SelectQuery
    {
        public SelectQuery Where<T>(Expression<Func<T, bool>> predicate) => this;
        public SelectQuery Having<T>(Expression<Func<T, bool>> predicate) => this;
    }
}

namespace TestNamespace
{
    using EricksonLopez.SqlBuilder;

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = """";
    }

    public class TestClass
    {
        public void TestMethod()
        {
            var q = new SelectQuery();
            q.Where<User>(u => {|#0:u.Id.ToString()|} == ""1"");
            q.Where<User>(u => {|#1:u.Name.ToUpper()|} == ""JOHN"");
            q.Having<User>(u => {|#2:u.Name.ToLower()|} == ""john"");
        }
    }
}";

            var expected0 = VerifyQueryPerformance.Diagnostic("ESQL004").WithLocation(0).WithArguments("ToString");
            var expected1 = VerifyQueryPerformance.Diagnostic("ESQL004").WithLocation(1).WithArguments("ToUpper");
            var expected2 = VerifyQueryPerformance.Diagnostic("ESQL004").WithLocation(2).WithArguments("ToLower");

            await VerifyQueryPerformance.VerifyAnalyzerAsync(code, expected0, expected1, expected2);
        }

        [Fact]
        public async Task ToStringOrToUpper_OutsideSqlBuilderQueryLambda_DoesNotReportDiagnostic()
        {
            var code = @"

namespace OtherNamespace
{
    public class OtherQuery
    {
        public OtherQuery Where<T>(Expression<Func<T, bool>> predicate) => this;
    }
}

namespace TestNamespace
{
    using OtherNamespace;

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = """";
    }

    public class TestClass
    {
        public void TestMethod(Action act)
        {
            var user = new User();
            var str = user.Id.ToString();
            var upper = user.Name.ToUpper();

            var o = new OtherQuery();
            o.Where<User>(u => u.Id.ToString() == ""1"");

            Func<User, bool> standaloneLambda = u => { act(); return u.Name == ""test""; };
        }
    }
}";
            await VerifyQueryPerformance.VerifyAnalyzerAsync(code);
        }
    }

    public static class VerifyQueryPerformance
    {
        public static DiagnosticResult Diagnostic(string diagnosticId)
        {
            return CSharpAnalyzerVerifier<QueryPerformanceAnalyzer, DefaultVerifier>.Diagnostic(diagnosticId);
        }

        public static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
        {
            var test = new CSharpAnalyzerTest<QueryPerformanceAnalyzer, DefaultVerifier>
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



