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
    public class UnsafeStringConcatenationAnalyzerTests
    {
        [Fact]
        public async Task StringConcatenation_InRawWhere_ReportsDiagnostic()
        {
            var code = @"
namespace TestNamespace
{
    public class Query { 
        public Query RawWhere(string sql) => this; 
        public Query RawSelect(string sql) => this;
        public Query Raw(string sql) => this;
    }
    
    public class TestClass
    {
        public void TestMethod(string param)
        {
            var q = new Query();
            q.RawWhere({|#0:""id = "" + param|});
            q.RawSelect({|#1:""id = "" + param|});
            q.Raw({|#2:""id = "" + param|});
        }
    }
}";
            var expected1 = VerifyCSFix.Diagnostic("ESQL002").WithLocation(0);
            var expected2 = VerifyCSFix.Diagnostic("ESQL002").WithLocation(1);
            var expected3 = VerifyCSFix.Diagnostic("ESQL002").WithLocation(2);
            var fixedCode = @"
namespace TestNamespace
{
    public class Query { 
        public Query RawWhere(string sql) => this; 
        public Query RawSelect(string sql) => this;
        public Query Raw(string sql) => this;
    }
    
    public class TestClass
    {
        public void TestMethod(string param)
        {
            var q = new Query();
            q.RawWhere($""id = {param}"");
            q.RawSelect($""id = {param}"");
            q.Raw($""id = {param}"");
        }
    }
}";

            await VerifyCSFix.VerifyCodeFixAsync(code, new[] { expected1, expected2, expected3 }, fixedCode);
        }

        [Fact]
        public async Task InterpolatedString_OrNoArgs_OrOtherMethod_DoesNotReportDiagnostic()
        {
            var code = @"
namespace TestNamespace
{
    public class Query { 
        public Query RawWhere(string sql) => this; 
        public Query RawWhere() => this;
        public Query OtherMethod(string sql) => this;
    }
    
    public class TestClass
    {
        public void TestMethod(string param)
        {
            var q = new Query();
            q.RawWhere($""id = {param}"");
            q.RawWhere();
            q.OtherMethod(""id = "" + param);
        }
    }
}";
            await VerifyCSFix.VerifyAnalyzerAsync(code);
        }
    }
    
    public static class VerifyCSFix
    {
        public static DiagnosticResult Diagnostic(string diagnosticId)
        {
            return CSharpCodeFixVerifier<UnsafeStringConcatenationAnalyzer, UnsafeStringConcatenationCodeFix, DefaultVerifier>.Diagnostic(diagnosticId);
        }
        
        public static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
        {
            var test = new CSharpAnalyzerTest<UnsafeStringConcatenationAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                CompilerDiagnostics = CompilerDiagnostics.None,
            };
            test.TestState.Sources.Add("global using System;\nglobal using System.Collections.Generic;\nglobal using System.Linq;\nglobal using System.Linq.Expressions;\nglobal using System.Threading;\nglobal using System.Threading.Tasks;\n");
            
            
            test.ExpectedDiagnostics.AddRange(expected);
            return test.RunAsync();
        }

        public static Task VerifyCodeFixAsync(string source, DiagnosticResult[] expected, string fixedSource)
        {
            var test = new CSharpCodeFixTest<UnsafeStringConcatenationAnalyzer, UnsafeStringConcatenationCodeFix, DefaultVerifier>
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



