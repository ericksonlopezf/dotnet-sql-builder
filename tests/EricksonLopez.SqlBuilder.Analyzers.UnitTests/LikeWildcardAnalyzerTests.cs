// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;

namespace EricksonLopez.SqlBuilder.Analyzers.Tests
{
    public class LikeWildcardAnalyzerTests
    {
        [Fact]
        public async Task WhereLike_WithoutWildcards_ReportsESQL009()
        {
            var code = @"
namespace TestNamespace
{
    public class Query
    {
        public Query WhereLike(string column, string pattern) => this;
        public Query WhereILike(string column, string pattern) => this;
    }

    public class TestClass
    {
        public void TestMethod()
        {
            var q = new Query();
            q.WhereLike(""Name"", {|#0:""John""|});
            q.WhereILike(""Name"", {|#1:""Smith""|});
        }
    }
}";

            var expected0 = VerifyLikeWildcard.Diagnostic("ESQL009").WithLocation(0).WithArguments("John");
            var expected1 = VerifyLikeWildcard.Diagnostic("ESQL009").WithLocation(1).WithArguments("Smith");

            await VerifyLikeWildcard.VerifyAnalyzerAsync(code, expected0, expected1);
        }

        [Fact]
        public async Task WhereLike_WithLeadingWildcard_ReportsESQL010()
        {
            var code = @"
namespace TestNamespace
{
    public class Query
    {
        public Query WhereLike(string column, string pattern) => this;
        public Query WhereILike(string column, string pattern) => this;
    }

    public class TestClass
    {
        public void TestMethod()
        {
            var q = new Query();
            q.WhereLike(""Name"", {|#0:""%John""|});
            q.WhereILike(""Name"", {|#1:""%Smith%""|});
        }
    }
}";

            var expected0 = VerifyLikeWildcard.Diagnostic("ESQL010").WithLocation(0).WithArguments("%John");
            var expected1 = VerifyLikeWildcard.Diagnostic("ESQL010").WithLocation(1).WithArguments("%Smith%");

            await VerifyLikeWildcard.VerifyAnalyzerAsync(code, expected0, expected1);
        }

        [Fact]
        public async Task WhereLike_WithValidTrailingOrInternalWildcard_DoesNotReportDiagnostic()
        {
            var code = @"

namespace TestNamespace
{
    public class Query
    {
        public Query WhereLike(string column, string pattern) => this;
        public Query WhereLike(string column) => this;
        public Query WhereILike(string column, string pattern) => this;
        public Query OtherMethod(string column, string pattern) => this;
    }

    public class TestClass
    {
        public void TestMethod(string dynamicPattern, Action act)
        {
            var q = new Query();
            q.WhereLike(""Name"", ""John%"");
            q.WhereILike(""Name"", ""J_hn"");
            q.WhereLike(""Name"", ""J%hn"");
            q.WhereLike(""Name"", dynamicPattern);
            q.WhereLike(""Name"");
            q.OtherMethod(""Name"", ""John"");
            act();
        }
    }
}";
            await VerifyLikeWildcard.VerifyAnalyzerAsync(code);
        }
    }

    public static class VerifyLikeWildcard
    {
        public static DiagnosticResult Diagnostic(string diagnosticId)
        {
            return CSharpAnalyzerVerifier<LikeWildcardAnalyzer, DefaultVerifier>.Diagnostic(diagnosticId);
        }

        public static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
        {
            var test = new CSharpAnalyzerTest<LikeWildcardAnalyzer, DefaultVerifier>
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



