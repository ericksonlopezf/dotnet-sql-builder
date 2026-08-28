// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;

namespace EricksonLopez.SqlBuilder.Analyzers.Tests
{
    public class MergeQueryAnalyzerTests
    {
        [Fact]
        public async Task SqlMerge_ReportsESQL026Diagnostic()
        {
            var code = @"
namespace EricksonLopez.SqlBuilder
{
    public static class Sql
    {
        public static MergeQuery<T> Merge<T>() where T : class, new() => new MergeQuery<T>();
    }

    public class MergeQuery<T> where T : class, new()
    {
        public MergeQuery<T> Merge(string table) => this;
    }

    public class TestUser { }

    public class Program
    {
        public void Run()
        {
            var q = {|#0:Sql.Merge<TestUser>()|};
            var m = new MergeQuery<TestUser>();
            {|#1:m.Merge(""Users"")|};
        }
    }
}";
            var expected0 = VerifyMergeQuery.Diagnostic("ESQL026").WithLocation(0);
            var expected1 = VerifyMergeQuery.Diagnostic("ESQL026").WithLocation(1);

            await VerifyMergeQuery.VerifyAnalyzerAsync(code, expected0, expected1);
        }

        [Fact]
        public async Task StaticImport_Merge_ReportsDiagnostic()
        {
            var code = @"
namespace EricksonLopez.SqlBuilder
{
    public static class Sql
    {
        public static object Merge<T>() => null!;
    }
}

namespace TestNamespace
{
    using static EricksonLopez.SqlBuilder.Sql;

    public class Program
    {
        public void Run()
        {
            var q = {|#0:Merge<object>()|};
        }
    }
}";
            var expected = VerifyMergeQuery.Diagnostic("ESQL026").WithLocation(0);
            await VerifyMergeQuery.VerifyAnalyzerAsync(code, expected);
        }

        [Fact]
        public async Task OtherMerge_OrNonMergeMethod_DoesNotReportDiagnostic()
        {
            var code = @"
namespace OtherNamespace
{
    public static class OtherClass
    {
        public static void Merge() { }
        public static void Select() { }
    }
}

namespace EricksonLopez.SqlBuilder
{
    public static class Sql
    {
        public static object Select() => null!;
    }
}

namespace TestNamespace
{
    using OtherNamespace;
    using EricksonLopez.SqlBuilder;

    public class Program
    {
        public void Run()
        {
            OtherClass.Merge();
            OtherClass.Select();
            Sql.Select();
        }
    }
}";
            await VerifyMergeQuery.VerifyAnalyzerAsync(code);
        }
    }

    public static class VerifyMergeQuery
    {
        public static DiagnosticResult Diagnostic(string diagnosticId)
        {
            return CSharpAnalyzerVerifier<MergeQueryAnalyzer, DefaultVerifier>.Diagnostic(diagnosticId);
        }

        public static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
        {
            var test = new CSharpAnalyzerTest<MergeQueryAnalyzer, DefaultVerifier>
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




