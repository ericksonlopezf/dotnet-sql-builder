// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;

namespace EricksonLopez.SqlBuilder.Analyzers.Tests
{
    public class MissingColumnAnalyzerTests
    {
        [Fact]
        public async Task Select_WithMissingColumn_ReportsDiagnostic()
        {
            var code = @"

namespace EricksonLopez.SqlBuilder
{
    public class SelectQuery<T> 
    {
        public SelectQuery<T> Select(string column) => this;
        public SelectQuery<T> Select(params string[] columns) => this;
        public SelectQuery<T> OrderBy(string column) => this;
        public SelectQuery<T> OrderByDescending(string column) => this;
        public SelectQuery<T> GroupBy(params string[] columns) => this;
    }

    public class Sql
    {
        public static SelectQuery<T> From<T>() => new SelectQuery<T>();
    }
}

namespace EricksonLopez.SqlBuilder.Annotations
{
    public interface ISqlEntity { }
    [AttributeUsage(AttributeTargets.Class)]
    public class SqlEntityAttribute : Attribute { public SqlEntityAttribute(string name) { } }
}

namespace TestNamespace
{
    using EricksonLopez.SqlBuilder;
    using EricksonLopez.SqlBuilder.Annotations;

    [SqlEntity(""users"")]
    public class User : ISqlEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = """";
        public int Age { get; set; }
    }

    public class TestClass
    {
        public void TestMethod()
        {
            var q = Sql.From<User>()
                .Select({|#0:""invalid_col""|})
                .OrderBy(""age"")
                .OrderByDescending({|#1:""invalid_desc""|})
                .GroupBy(""name"", {|#2:""missing_group""|});
        }
    }
}";
            var expected0 = VerifyCSMissingColumn.Diagnostic("SQL0009")
                .WithLocation(0)
                .WithArguments("invalid_col", "User");
                
            var expected1 = VerifyCSMissingColumn.Diagnostic("SQL0009")
                .WithLocation(1)
                .WithArguments("invalid_desc", "User");

            var expected2 = VerifyCSMissingColumn.Diagnostic("SQL0009")
                .WithLocation(2)
                .WithArguments("missing_group", "User");

            await VerifyCSMissingColumn.VerifyAnalyzerAsync2(code, expected0, expected1, expected2);
        }

        [Fact]
        public async Task Select_WithValidColumnOrStarOrOpenGeneric_DoesNotReportDiagnostic()
        {
            var code = @"

namespace EricksonLopez.SqlBuilder
{
    public class SelectQuery<T> 
    {
        public SelectQuery<T> Select(string column) => this;
        public SelectQuery<T> Select(params string[] columns) => this;
        public SelectQuery<T> OrderBy(string column) => this;
        public SelectQuery<T> GroupBy(params string[] columns) => this;
    }

    public class Sql
    {
        public static SelectQuery<T> From<T>() => new SelectQuery<T>();
    }
}

namespace TestNamespace
{

    public class User
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = """";
    }

    public class TestClass
    {
        public void TestMethod()
        {
            var q = Sql.From<User>()
                .Select(""Id"")
                .Select(""first_name"")
                .Select(""*"")
                .OrderBy(""FirstName"")
                .GroupBy(""id"");
        }

        public void TestOpenGeneric<T>(SelectQuery<T> openQ)
        {
            openQ.Select(""any_col"");
        }
    }
}";
            await VerifyCSMissingColumn.VerifyAnalyzerAsync2(code);
        }
    }

    public static class VerifyCSMissingColumn
    {
        public static DiagnosticResult Diagnostic(string diagnosticId)
        {
            return CSharpAnalyzerVerifier<MissingColumnAnalyzer, DefaultVerifier>.Diagnostic(diagnosticId);
        }

        public static Task VerifyAnalyzerAsync2(string source, params DiagnosticResult[] expected)
        {
            var test = new CSharpAnalyzerTest<MissingColumnAnalyzer, DefaultVerifier>
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




