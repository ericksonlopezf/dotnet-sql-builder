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
    public class CartesianJoinAnalyzerTests
    {
        [Fact]
        public async Task RawJoin_WithoutOnCondition_ReportsDiagnostic()
        {
            var code = @"
namespace TestNamespace
{
    public class Query
    {
        public Query RawJoin(string joinClause) => this;
    }

    public class TestClass
    {
        public void TestMethod(string tableName)
        {
            var q = new Query();
            {|#0:q.RawJoin(""JOIN Orders"")|};
            {|#1:q.RawJoin($""JOIN {tableName}"")|};
        }
    }
}";

            var expected0 = VerifyCartesianJoin.Diagnostic("ESQL024").WithLocation(0).WithArguments("RawJoin");
            var expected1 = VerifyCartesianJoin.Diagnostic("ESQL024").WithLocation(1).WithArguments("RawJoin");

            await VerifyCartesianJoin.VerifyAnalyzerAsync(code, expected0, expected1);
        }

        [Fact]
        public async Task RawJoin_WithCrossJoinOrOnCondition_DoesNotReportDiagnostic()
        {
            var code = @"
namespace TestNamespace
{
    public class Query
    {
        public Query RawJoin(string joinClause) => this;
    }

    public class TestClass
    {
        public void TestMethod()
        {
            var q = new Query();
            q.RawJoin(""CROSS JOIN Orders"");
            q.RawJoin(""JOIN Orders ON Orders.Id = Users.OrderId"");
            q.RawJoin($""JOIN Orders ON Orders.Id = {123}"");
            q.RawJoin("""");
        }
    }
}";
            await VerifyCartesianJoin.VerifyAnalyzerAsync(code);
        }

        [Fact]
        public async Task SelectQuery_Join_WithEmptyOnClause_ReportsDiagnostic()
        {
            var code = @"
namespace TestNamespace
{
    public class SelectQuery
    {
        public SelectQuery Join(string table, string on) => this;
        public SelectQuery LeftJoin(string table, string on) => this;
        public SelectQuery RightJoin(string table, string on) => this;
        public SelectQuery InnerJoin(string table, string on) => this;
        public SelectQuery FullJoin(string table, string on) => this;
    }

    public class TestClass
    {
        public void TestMethod()
        {
            var q = new SelectQuery();
            q.Join(""Orders"", {|#0:""   ""|});
            q.LeftJoin(""Orders"", {|#1:""""|});
            q.RightJoin(""Orders"", {|#2:""\t""|});
            q.InnerJoin(""Orders"", {|#3:"" ""|});
            q.FullJoin(""Orders"", {|#4:""""|});
        }
    }
}";

            var expected0 = VerifyCartesianJoin.Diagnostic("ESQL024").WithLocation(0).WithArguments("Join");
            var expected1 = VerifyCartesianJoin.Diagnostic("ESQL024").WithLocation(1).WithArguments("LeftJoin");
            var expected2 = VerifyCartesianJoin.Diagnostic("ESQL024").WithLocation(2).WithArguments("RightJoin");
            var expected3 = VerifyCartesianJoin.Diagnostic("ESQL024").WithLocation(3).WithArguments("InnerJoin");
            var expected4 = VerifyCartesianJoin.Diagnostic("ESQL024").WithLocation(4).WithArguments("FullJoin");

            await VerifyCartesianJoin.VerifyAnalyzerAsync(code, expected0, expected1, expected2, expected3, expected4);
        }

        [Fact]
        public async Task SelectQuery_Join_WithValidOnClause_DoesNotReportDiagnostic()
        {
            var code = @"
namespace TestNamespace
{
    public class SelectQuery
    {
        public SelectQuery Join(string table, string on) => this;
    }

    public class OtherType
    {
        public OtherType Join(string table, string on) => this;
    }

    public class TestClass
    {
        public void TestMethod()
        {
            var q = new SelectQuery();
            q.Join(""Orders"", ""Orders.Id = Users.OrderId"");

            var o = new OtherType();
            o.Join(""Orders"", """");
        }
    }
}";
            await VerifyCartesianJoin.VerifyAnalyzerAsync(code);
        }
    }

    public static class VerifyCartesianJoin
    {
        public static DiagnosticResult Diagnostic(string diagnosticId)
        {
            return CSharpAnalyzerVerifier<CartesianJoinAnalyzer, DefaultVerifier>.Diagnostic(diagnosticId);
        }

        public static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
        {
            var test = new CSharpAnalyzerTest<CartesianJoinAnalyzer, DefaultVerifier>
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



