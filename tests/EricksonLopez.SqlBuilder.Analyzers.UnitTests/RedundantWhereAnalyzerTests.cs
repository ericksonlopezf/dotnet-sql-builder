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
    public class RedundantWhereAnalyzerTests
    {
        [Fact]
        public async Task WhereAndOr_WithTautologicalConditions_ReportsDiagnostic()
        {
            var code = @"
namespace TestNamespace
{
    public class Query
    {
        public Query Where(string clause) => this;
        public Query And(string clause) => this;
        public Query Or(string clause) => this;
    }

    public class TestClass
    {
        public void TestMethod()
        {
            var q = new Query();
            q.Where({|#0:""1 = 1""|});
            q.And({|#1:""true=true""|});
            q.Or({|#2:""0 = 0""|});
        }
    }
}";

            var expected0 = VerifyRedundantWhere.Diagnostic("SQL0004").WithLocation(0).WithArguments("1 = 1");
            var expected1 = VerifyRedundantWhere.Diagnostic("SQL0004").WithLocation(1).WithArguments("true=true");
            var expected2 = VerifyRedundantWhere.Diagnostic("SQL0004").WithLocation(2).WithArguments("0 = 0");

            await VerifyRedundantWhere.VerifyAnalyzerAsync(code, expected0, expected1, expected2);
        }

        [Fact]
        public async Task Where_WithMeaningfulConditions_DoesNotReportDiagnostic()
        {
            var code = @"
namespace TestNamespace
{
    public class Query
    {
        public Query Where(string clause) => this;
        public Query And(string clause) => this;
        public Query OtherMethod(string clause) => this;
    }

    public class TestClass
    {
        public void TestMethod(string dynamicClause)
        {
            var q = new Query();
            q.Where(""Id = 1"");
            q.And(""Status = 'Active'"");
            q.Where(dynamicClause);
            q.OtherMethod(""1 = 1"");
        }
    }
}";
            await VerifyRedundantWhere.VerifyAnalyzerAsync(code);
        }
    }

    public static class VerifyRedundantWhere
    {
        public static DiagnosticResult Diagnostic(string diagnosticId)
        {
            return CSharpAnalyzerVerifier<RedundantWhereAnalyzer, DefaultVerifier>.Diagnostic(diagnosticId);
        }

        public static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
        {
            var test = new CSharpAnalyzerTest<RedundantWhereAnalyzer, DefaultVerifier>
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




