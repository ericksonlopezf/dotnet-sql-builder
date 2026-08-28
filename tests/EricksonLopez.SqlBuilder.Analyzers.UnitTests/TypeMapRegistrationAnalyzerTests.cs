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
    public class TypeMapRegistrationAnalyzerTests
    {
        [Fact]
        public async Task SqlMapper_AddTypeMap_ReportsDiagnostic()
        {
            var code = @"

namespace TestNamespace
{
    public static class SqlMapper
    {
        public static void AddTypeMap(Type type, object map) { }
    }

    public class TestClass
    {
        public void TestMethod()
        {
            {|#0:SqlMapper.AddTypeMap(typeof(string), null!)|};
        }
    }
}";

            var expected = VerifyTypeMapRegistration.Diagnostic("ESQL022").WithLocation(0);

            await VerifyTypeMapRegistration.VerifyAnalyzerAsync(code, expected);
        }

        [Fact]
        public async Task OtherMapper_AddTypeMap_DoesNotReportDiagnostic()
        {
            var code = @"

namespace TestNamespace
{
    public static class OtherMapper
    {
        public static void AddTypeMap(Type type, object map) { }
    }

    public class TestClass
    {
        public void TestMethod()
        {
            OtherMapper.AddTypeMap(typeof(string), null!);
        }
    }
}";
            await VerifyTypeMapRegistration.VerifyAnalyzerAsync(code);
        }
    }

    public static class VerifyTypeMapRegistration
    {
        public static DiagnosticResult Diagnostic(string diagnosticId)
        {
            return CSharpAnalyzerVerifier<TypeMapRegistrationAnalyzer, DefaultVerifier>.Diagnostic(diagnosticId);
        }

        public static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
        {
            var test = new CSharpAnalyzerTest<TypeMapRegistrationAnalyzer, DefaultVerifier>
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



