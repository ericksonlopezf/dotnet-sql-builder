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
    public class SqlKataMigrationAnalyzerTests
    {
        [Fact]
        public async Task Query_WithTableNameArg_ReportsDiagnostic_AndMigratesToSqlFrom()
        {
            var code = @"
namespace TestNamespace
{
    public class Query
    {
        public Query(string table) { }
    }

    public static class Sql
    {
        public static object From(string table) => null!;
    }

    public class TestClass
    {
        public void TestMethod()
        {
            var q = {|#0:new Query(""Users"")|};
        }
    }
}";

            var expected = VerifySqlKataCSFix.Diagnostic("ESQL025").WithLocation(0);

            var fixedCode = @"
namespace TestNamespace
{
    public class Query
    {
        public Query(string table) { }
    }

    public static class Sql
    {
        public static object From(string table) => null!;
    }

    public class TestClass
    {
        public void TestMethod()
        {
            var q = Sql.From(""Users"");
        }
    }
}";

            await VerifySqlKataCSFix.VerifyCodeFixAsync(code, expected, fixedCode);
        }

        [Fact]
        public async Task Query_ChainedFrom_ReportsDiagnostic_AndMigratesToSqlFrom()
        {
            var code = @"
namespace TestNamespace
{
    public class Query
    {
        public Query() { }
        public Query From(string table) => this;
    }

    public static class Sql
    {
        public static object From(string table) => null!;
    }

    public class TestClass
    {
        public void TestMethod()
        {
            var q = {|#0:new Query()|}.From(""Users"");
        }
    }
}";

            var expected = VerifySqlKataCSFix.Diagnostic("ESQL025").WithLocation(0);

            var fixedCode = @"
namespace TestNamespace
{
    public class Query
    {
        public Query() { }
        public Query From(string table) => this;
    }

    public static class Sql
    {
        public static object From(string table) => null!;
    }

    public class TestClass
    {
        public void TestMethod()
        {
            var q = Sql.From(""Users"");
        }
    }
}";

            await VerifySqlKataCSFix.VerifyCodeFixAsync(code, expected, fixedCode);
        }

        [Fact]
        public async Task Query_NoArgs_ReportsDiagnostic_AndMigratesToSqlFromUnknown()
        {
            var code = @"
namespace TestNamespace
{
    public class Query
    {
        public Query() { }
    }

    public static class Sql
    {
        public static object From(string table) => null!;
    }

    public class TestClass
    {
        public void TestMethod()
        {
            var q = {|#0:new Query()|};
        }
    }
}";

            var expected = VerifySqlKataCSFix.Diagnostic("ESQL025").WithLocation(0);

            var fixedCode = @"
namespace TestNamespace
{
    public class Query
    {
        public Query() { }
    }

    public static class Sql
    {
        public static object From(string table) => null!;
    }

    public class TestClass
    {
        public void TestMethod()
        {
            var q = Sql.From(""Unknown"");
        }
    }
}";

            await VerifySqlKataCSFix.VerifyCodeFixAsync(code, expected, fixedCode);
        }

        [Fact]
        public async Task OtherObjectCreation_DoesNotReportDiagnostic()
        {
            var code = @"
namespace TestNamespace
{
    public class OtherClass { }

    public class TestClass
    {
        public void TestMethod()
        {
            var o = new OtherClass();
        }
    }
}";
            await VerifySqlKataCSFix.VerifyAnalyzerAsync(code);
        }
    }

    public static class VerifySqlKataCSFix
    {
        public static DiagnosticResult Diagnostic(string diagnosticId)
        {
            return CSharpCodeFixVerifier<SqlKataMigrationAnalyzer, SqlKataMigrationCodeFixProvider, DefaultVerifier>.Diagnostic(diagnosticId);
        }

        public static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
        {
            var test = new CSharpAnalyzerTest<SqlKataMigrationAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                CompilerDiagnostics = CompilerDiagnostics.None,
            };
            test.TestState.Sources.Add("global using System;\nglobal using System.Collections.Generic;\nglobal using System.Linq;\nglobal using System.Linq.Expressions;\nglobal using System.Threading;\nglobal using System.Threading.Tasks;\n");
            
            
            test.ExpectedDiagnostics.AddRange(expected);
            return test.RunAsync();
        }

        public static Task VerifyCodeFixAsync(string source, DiagnosticResult expected, string fixedSource)
        {
            return VerifyCodeFixAsync(source, new[] { expected }, fixedSource);
        }

        public static Task VerifyCodeFixAsync(string source, DiagnosticResult[] expected, string fixedSource)
        {
            var test = new CSharpCodeFixTest<SqlKataMigrationAnalyzer, SqlKataMigrationCodeFixProvider, DefaultVerifier>
            {
                TestCode = source,
                FixedCode = fixedSource,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                CompilerDiagnostics = CompilerDiagnostics.None,
            };
            test.TestState.Sources.Add("global using System;\nglobal using System.Collections.Generic;\nglobal using System.Linq;\nglobal using System.Linq.Expressions;\nglobal using System.Threading;\nglobal using System.Threading.Tasks;\n");
            test.FixedState.Sources.Add("global using System;\nglobal using System.Collections.Generic;\nglobal using System.Linq;\nglobal using System.Linq.Expressions;\nglobal using System.Threading;\nglobal using System.Threading.Tasks;\n");
            
            
            
            
            test.ExpectedDiagnostics.AddRange(expected);
            return test.RunAsync();
        }
    }
}



