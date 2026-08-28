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
    /// <summary>
    /// Tests for ELSB004 — DynamicIdentifierAnalyzer.
    /// Verifies that dynamically built SQL identifiers (table/column names via concatenation
    /// or interpolation without a constant) trigger a Security warning.
    /// </summary>
    public class DynamicIdentifierAnalyzerTests
    {
        [Fact]
        public async Task StringConcatenation_InAllTargetMethods_ReportsDiagnostic()
        {
            var code = @"
namespace EricksonLopez.SqlBuilder
{
    public class SelectQuery<T> where T : class, new()
    {
        public SelectQuery<T> InnerJoin(string table, string alias, string condition) => this;
        public SelectQuery<T> LeftJoin(string table) => this;
        public SelectQuery<T> RightJoin(string table) => this;
        public SelectQuery<T> FullJoin(string table) => this;
        public SelectQuery<T> CrossJoin(string table) => this;
        public SelectQuery<T> RawJoin(string table) => this;
        public SelectQuery<T> GroupBy(string column) => this;
        public SelectQuery<T> From(string table) => this;
    }
}

namespace TestNamespace
{
    using EricksonLopez.SqlBuilder;

    public class TestClass
    {
        public void Test(string userTable)
        {
            var q = new SelectQuery<object>();
            q.InnerJoin({|#0:""prefix_"" + userTable|}, ""t"", ""t.id = x.id"");
            q.LeftJoin({|#1:$""{userTable}_suffix""|});
            q.RightJoin({|#2:""tbl_"" + userTable|});
            q.FullJoin({|#3:$""{userTable}""|});
            q.CrossJoin({|#4:""tbl_"" + userTable|});
            q.RawJoin({|#5:$""{userTable}""|});
            q.GroupBy({|#6:""col_"" + userTable|});
            q.From({|#7:$""{userTable}""|});
        }
    }
}";
            var expected0 = VerifyDynamicIdentifier.Diagnostic("ELSB004").WithLocation(0).WithArguments("\"prefix_\" + userTable");
            var expected1 = VerifyDynamicIdentifier.Diagnostic("ELSB004").WithLocation(1).WithArguments("$\"{userTable}_suffix\"");
            var expected2 = VerifyDynamicIdentifier.Diagnostic("ELSB004").WithLocation(2).WithArguments("\"tbl_\" + userTable");
            var expected3 = VerifyDynamicIdentifier.Diagnostic("ELSB004").WithLocation(3).WithArguments("$\"{userTable}\"");
            var expected4 = VerifyDynamicIdentifier.Diagnostic("ELSB004").WithLocation(4).WithArguments("\"tbl_\" + userTable");
            var expected5 = VerifyDynamicIdentifier.Diagnostic("ELSB004").WithLocation(5).WithArguments("$\"{userTable}\"");
            var expected6 = VerifyDynamicIdentifier.Diagnostic("ELSB004").WithLocation(6).WithArguments("\"col_\" + userTable");
            var expected7 = VerifyDynamicIdentifier.Diagnostic("ELSB004").WithLocation(7).WithArguments("$\"{userTable}\"");

            await VerifyDynamicIdentifier.VerifyAnalyzerAsync(code, expected0, expected1, expected2, expected3, expected4, expected5, expected6, expected7);
        }

        [Fact]
        public async Task ConstantExpressions_AndOtherClasses_DoNotReportDiagnostic()
        {
            var code = @"
namespace EricksonLopez.SqlBuilder
{
    public class SelectQuery<T> where T : class, new()
    {
        public SelectQuery<T> InnerJoin(string table, string alias, string condition) => this;
        public SelectQuery<T> From(string table) => this;
        public SelectQuery<T> From() => this;
        public SelectQuery<T> OtherMethod(string table) => this;
    }
}

namespace OtherNamespace
{
    public class OtherQuery
    {
        public OtherQuery From(string table) => this;
    }
}

namespace TestNamespace
{
    using OtherNamespace;

    public class TestClass
    {
        private const string ConstTable = ""Orders"";

        public void Test(string userTable)
        {
            var q = new SelectQuery<object>();
            q.InnerJoin(""prefix_"" + ""orders"", ""o"", ""o.id = x.id"");
            q.From($""dbo.{ConstTable}"");
            q.From();
            q.OtherMethod(""table_"" + userTable);

            var o = new OtherQuery();
            o.From(""table_"" + userTable);
        }
    }
}";
            await VerifyDynamicIdentifier.VerifyAnalyzerAsync(code);
        }
    }

    public static class VerifyDynamicIdentifier
    {
        public static DiagnosticResult Diagnostic(string diagnosticId)
        {
            return CSharpAnalyzerVerifier<DynamicIdentifierAnalyzer, DefaultVerifier>.Diagnostic(diagnosticId);
        }

        public static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
        {
            var test = new CSharpAnalyzerTest<DynamicIdentifierAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
                CompilerDiagnostics = CompilerDiagnostics.None,
            };
            test.TestState.Sources.Add("global using System;\nglobal using System.Collections.Generic;\nglobal using System.Linq;\nglobal using System.Linq.Expressions;\nglobal using System.Threading;\nglobal using System.Threading.Tasks;\n");
            
            
            test.ExpectedDiagnostics.AddRange(expected);
            return test.RunAsync();
        }
    }
}




