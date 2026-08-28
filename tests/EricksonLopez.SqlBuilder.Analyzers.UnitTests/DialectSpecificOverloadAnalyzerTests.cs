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
    public class DialectSpecificOverloadAnalyzerTests
    {
        [Fact]
        public async Task MethodWithRequiresCapabilityAttribute_ReportsDiagnostic()
        {
            var code = @"

namespace TestNamespace
{
    public class RequiresCapabilityAttribute : Attribute
    {
        public RequiresCapabilityAttribute(string capability) { }
    }

    public class Query
    {
        [RequiresCapability(""PostgreSql.Jsonb"")]
        public Query WhereJsonContains(string column, string json) => this;

        public Query WhereNormal(string column, string value) => this;
    }

    public class TestClass
    {
        public void TestMethod()
        {
            var q = new Query();
            {|#0:q.WhereJsonContains(""data"", ""{}"")|};
            q.WhereNormal(""id"", ""1"");
        }
    }
}";

            var expected = VerifyDialectSpecific.Diagnostic("ESQL020")
                .WithLocation(0)
                .WithArguments("WhereJsonContains", "PostgreSql.Jsonb");

            await VerifyDialectSpecific.VerifyAnalyzerAsync(code, expected);
        }

        [Fact]
        public async Task MethodWithoutAttribute_DoesNotReportDiagnostic()
        {
            var code = @"
namespace TestNamespace
{
    public class Query
    {
        public Query NormalMethod() => this;
    }

    public class TestClass
    {
        public void TestMethod()
        {
            var q = new Query();
            q.NormalMethod();
        }
    }
}";
            await VerifyDialectSpecific.VerifyAnalyzerAsync(code);
        }
    }

    public static class VerifyDialectSpecific
    {
        public static DiagnosticResult Diagnostic(string diagnosticId)
        {
            return CSharpAnalyzerVerifier<DialectSpecificOverloadAnalyzer, DefaultVerifier>.Diagnostic(diagnosticId);
        }

        public static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
        {
            var test = new CSharpAnalyzerTest<DialectSpecificOverloadAnalyzer, DefaultVerifier>
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



