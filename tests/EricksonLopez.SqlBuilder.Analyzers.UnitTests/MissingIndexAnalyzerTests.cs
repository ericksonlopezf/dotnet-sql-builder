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
    public class MissingIndexAnalyzerTests
    {
        [Fact]
        public async Task OrderBy_OnUnindexedProperty_ReportsDiagnostic()
        {
            var code = @"

namespace TestNamespace
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = """";
    }

    public class Query
    {
        public Query OrderBy<T>(Expression<Func<T, object>> keySelector) => this;
        public Query OrderByDescending<T>(Expression<Func<T, object>> keySelector) => this;
        public Query ThenBy<T>(Expression<Func<T, object>> keySelector) => this;
        public Query ThenByDescending<T>(Expression<Func<T, object>> keySelector) => this;
    }

    public class TestClass
    {
        public void TestMethod()
        {
            var q = new Query();
            q.OrderBy<User>({|#0:x => x.Id|});
            q.OrderByDescending<User>({|#1:x => x.Name|});
            q.ThenBy<User>({|#2:x => x.Id|});
            q.ThenByDescending<User>({|#3:x => x.Name|});
        }
    }
}";

            var expected0 = VerifyMissingIndex.Diagnostic("ESQL007").WithLocation(0).WithArguments("Id");
            var expected1 = VerifyMissingIndex.Diagnostic("ESQL007").WithLocation(1).WithArguments("Name");
            var expected2 = VerifyMissingIndex.Diagnostic("ESQL007").WithLocation(2).WithArguments("Id");
            var expected3 = VerifyMissingIndex.Diagnostic("ESQL007").WithLocation(3).WithArguments("Name");

            await VerifyMissingIndex.VerifyAnalyzerAsync(code, expected0, expected1, expected2, expected3);
        }

        [Fact]
        public async Task OrderBy_OnIndexedProperty_DoesNotReportDiagnostic()
        {
            var code = @"

namespace TestNamespace
{
    public class IndexedAttribute : Attribute { }

    public class User
    {
        [Indexed]
        public int Id { get; set; }

        [Indexed]
        public string Name { get; set; } = """";
    }

    public class Query
    {
        public Query OrderBy<T>(Expression<Func<T, object>> keySelector) => this;
        public Query OtherMethod(string col) => this;
    }

    public class TestClass
    {
        public void TestMethod()
        {
            var q = new Query();
            q.OrderBy<User>(x => x.Id);
            q.OrderBy<User>(x => x.Name);
            q.OtherMethod(""Id"");
        }
    }
}";
            await VerifyMissingIndex.VerifyAnalyzerAsync(code);
        }
    }

    public static class VerifyMissingIndex
    {
        public static DiagnosticResult Diagnostic(string diagnosticId)
        {
            return CSharpAnalyzerVerifier<MissingIndexAnalyzer, DefaultVerifier>.Diagnostic(diagnosticId);
        }

        public static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
        {
            var test = new CSharpAnalyzerTest<MissingIndexAnalyzer, DefaultVerifier>
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



