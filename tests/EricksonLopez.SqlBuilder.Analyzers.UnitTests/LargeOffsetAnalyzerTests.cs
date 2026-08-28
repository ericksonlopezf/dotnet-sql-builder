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
    public class LargeOffsetAnalyzerTests
    {
        [Fact]
        public async Task Offset_GreaterThan10000_ReportsDiagnostic()
        {
            var code = @"
namespace TestNamespace
{
    public class SelectQuery
    {
        public SelectQuery Offset(int count) => this;
    }

    public class TestClass
    {
        public void TestMethod()
        {
            var q = new SelectQuery();
            q.Offset({|#0:15000|});
        }
    }
}";

            var expected = VerifyLargeOffset.Diagnostic("ESQL008").WithLocation(0).WithArguments("15000");

            await VerifyLargeOffset.VerifyAnalyzerAsync(code, expected);
        }

        [Fact]
        public async Task Offset_LessThanOrEqualTo10000_DoesNotReportDiagnostic()
        {
            var code = @"

namespace TestNamespace
{
    public class SelectQuery
    {
        public SelectQuery Offset(int count) => this;
        public SelectQuery Offset() => this;
    }

    public class OtherQuery
    {
        public OtherQuery Offset(int count) => this;
    }

    public class TestClass
    {
        public void TestMethod(int dynamicOffset, Action act)
        {
            var q = new SelectQuery();
            q.Offset(100);
            q.Offset(10000);
            q.Offset(dynamicOffset);
            q.Offset();

            var o = new OtherQuery();
            o.Offset(20000);

            act();
        }
    }
}";
            await VerifyLargeOffset.VerifyAnalyzerAsync(code);
        }
    }

    public static class VerifyLargeOffset
    {
        public static DiagnosticResult Diagnostic(string diagnosticId)
        {
            return CSharpAnalyzerVerifier<LargeOffsetAnalyzer, DefaultVerifier>.Diagnostic(diagnosticId);
        }

        public static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
        {
            var test = new CSharpAnalyzerTest<LargeOffsetAnalyzer, DefaultVerifier>
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



