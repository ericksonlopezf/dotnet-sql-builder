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
    public class SyncOnUiThreadAnalyzerTests
    {
        [Fact]
        public async Task ConnectionSqlExtensions_SyncMethods_ReportsDiagnostic()
        {
            var code = @"
namespace TestNamespace
{
    public static class ConnectionSqlExtensions
    {
        public static void ToResult(this object conn) { }
        public static void ToPagedList(this object conn) { }
        public static void ToStream(this object conn) { }
        public static void ToResultAsync(this object conn) { }
    }

    public class TestClass
    {
        public void TestMethod(object conn)
        {
            {|#0:conn.ToResult()|};
            {|#1:conn.ToPagedList()|};
            {|#2:conn.ToStream()|};
            conn.ToResultAsync();
        }
    }
}";

            var expected0 = VerifySyncOnUiThread.Diagnostic("ESQL023").WithLocation(0);
            var expected1 = VerifySyncOnUiThread.Diagnostic("ESQL023").WithLocation(1);
            var expected2 = VerifySyncOnUiThread.Diagnostic("ESQL023").WithLocation(2);

            await VerifySyncOnUiThread.VerifyAnalyzerAsync(code, expected0, expected1, expected2);
        }

        [Fact]
        public async Task OtherExtensions_SyncMethods_DoesNotReportDiagnostic()
        {
            var code = @"
namespace TestNamespace
{
    public static class OtherExtensions
    {
        public static void ToResult(this object conn) { }
    }

    public class TestClass
    {
        public void TestMethod(object conn)
        {
            conn.ToResult();
        }
    }
}";
            await VerifySyncOnUiThread.VerifyAnalyzerAsync(code);
        }
    }

    public static class VerifySyncOnUiThread
    {
        public static DiagnosticResult Diagnostic(string diagnosticId)
        {
            return CSharpAnalyzerVerifier<SyncOnUiThreadAnalyzer, DefaultVerifier>.Diagnostic(diagnosticId);
        }

        public static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
        {
            var test = new CSharpAnalyzerTest<SyncOnUiThreadAnalyzer, DefaultVerifier>
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



