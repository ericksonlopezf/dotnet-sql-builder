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
    public class DapperCompilerAnalyzerTests
    {
        [Fact]
        public async Task DapperExtensions_QueryMethod_ReportsDiagnostic()
        {
            var code = @"
namespace TestNamespace
{
    public static class DapperExtensions
    {
        public static void Query(this object connection) { }
        public static void QueryAsync(this object connection) { }
    }

    public class TestClass
    {
        public void TestMethod(object conn)
        {
            {|#0:conn.Query()|};
            {|#1:conn.QueryAsync()|};
        }
    }
}";

            var expected0 = VerifyDapperCompiler.Diagnostic("ESQL005").WithLocation(0);
            var expected1 = VerifyDapperCompiler.Diagnostic("ESQL005").WithLocation(1);

            await VerifyDapperCompiler.VerifyAnalyzerAsync(code, expected0, expected1);
        }

        [Fact]
        public async Task NonDapperExtensions_OrNonMatchingMethod_DoesNotReportDiagnostic()
        {
            var code = @"

namespace TestNamespace
{
    public static class OtherExtensions
    {
        public static void Query(this object connection) { }
    }

    public static class DapperExtensions
    {
        public static void Execute(this object connection) { }
    }

    public class TestClass
    {
        public void TestMethod(object conn, Action act)
        {
            conn.Query();
            conn.Execute();
            act();
        }
    }
}";
            await VerifyDapperCompiler.VerifyAnalyzerAsync(code);
        }
    }

    public static class VerifyDapperCompiler
    {
        public static DiagnosticResult Diagnostic(string diagnosticId)
        {
            return CSharpAnalyzerVerifier<DapperCompilerAnalyzer, DefaultVerifier>.Diagnostic(diagnosticId);
        }

        public static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
        {
            var test = new CSharpAnalyzerTest<DapperCompilerAnalyzer, DefaultVerifier>
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



