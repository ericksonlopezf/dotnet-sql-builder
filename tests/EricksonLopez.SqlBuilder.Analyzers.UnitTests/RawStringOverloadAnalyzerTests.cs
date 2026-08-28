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
    public class RawStringOverloadAnalyzerTests
    {
        [Fact]
        public async Task SqlRaw_WithStringParameter_ReportsDiagnostic()
        {
            var code = @"

namespace EricksonLopez.SqlBuilder
{
    public static class Sql
    {
        public static object Raw(string sql) => null!;
        public static object Raw(FormattableString formattable) => null!;
    }
}

namespace TestNamespace
{
    using EricksonLopez.SqlBuilder;

    public class TestClass
    {
        public void TestMethod(string param)
        {
            var raw1 = {|#0:Sql.Raw(""SELECT 1"")|};
            var raw2 = {|#1:Sql.Raw(""SELECT * FROM Users WHERE Id = "" + param)|};
        }
    }
}";

            var expected0 = VerifyRawStringOverload.Diagnostic("ESQL011").WithLocation(0);
            var expected1 = VerifyRawStringOverload.Diagnostic("ESQL011").WithLocation(1);

            await VerifyRawStringOverload.VerifyAnalyzerAsync(code, expected0, expected1);
        }

        [Fact]
        public async Task StaticImport_Raw_ReportsDiagnostic()
        {
            var code = @"

namespace EricksonLopez.SqlBuilder
{
    public static class Sql
    {
        public static object Raw(string sql) => null!;
    }
}

namespace TestNamespace
{
    using static EricksonLopez.SqlBuilder.Sql;

    public class TestClass
    {
        public void TestMethod()
        {
            var raw = {|#0:Raw(""SELECT 1"")|};
        }
    }
}";
            var expected = VerifyRawStringOverload.Diagnostic("ESQL011").WithLocation(0);
            await VerifyRawStringOverload.VerifyAnalyzerAsync(code, expected);
        }

        [Fact]
        public async Task SqlRaw_WithFormattableString_OrOtherClass_DoesNotReportDiagnostic()
        {
            var code = @"

namespace EricksonLopez.SqlBuilder
{
    public static class Sql
    {
        public static object Raw(string sql) => null!;
        public static object Raw(FormattableString formattable) => null!;
        public static object Raw() => null!;
        public static object Raw(int numeric) => null!;
    }
}

namespace OtherNamespace
{
    public static class OtherSql
    {
        public static object Raw(string sql) => null!;
    }
}

namespace TestNamespace
{
    using OtherNamespace;

    public class TestClass
    {
        public void TestMethod(int id)
        {
            FormattableString query = $""SELECT * FROM Users WHERE Id = {id}"";
            var safeRaw = Sql.Raw(query);
            var otherRaw = OtherSql.Raw(""SELECT 1"");
            Sql.Raw();
            Sql.Raw(123);
        }
    }
}";
            await VerifyRawStringOverload.VerifyAnalyzerAsync(code);
        }
    }

    public static class VerifyRawStringOverload
    {
        public static DiagnosticResult Diagnostic(string diagnosticId)
        {
            return CSharpAnalyzerVerifier<RawStringOverloadAnalyzer, DefaultVerifier>.Diagnostic(diagnosticId);
        }

        public static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
        {
            var test = new CSharpAnalyzerTest<RawStringOverloadAnalyzer, DefaultVerifier>
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



