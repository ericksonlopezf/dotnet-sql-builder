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
    public class DeleteWithoutWhereAnalyzerTests
    {
        // ─────────────────────────────────────────────────────────────────────
        // ESQL001 — DELETE without WHERE
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task Delete_ExecuteAsync_WithoutWhere_ReportsDiagnostic()
        {
            var code = @"

namespace EricksonLopez.SqlBuilder.Abstractions
{
    public interface ISqlQuery { }
}

namespace EricksonLopez.SqlBuilder
{
    public class DbConnection { public Task ExecuteAsync(object query) => Task.CompletedTask; }
    public class Sql { public static DeleteQuery<T> Delete<T>() => new DeleteQuery<T>(); }
    public class DeleteQuery<T> : EricksonLopez.SqlBuilder.Abstractions.ISqlQuery { public object Build() => this; }
    public class User {}

    public class TestClass
    {
        public async Task TestMethod()
        {
            var conn = new DbConnection();
            await conn.ExecuteAsync(Sql.Delete<User>().Build());
        }
    }
}";
            var expected1 = VerifyCS.Diagnostic("ESQL001")
                .WithSpan(20, 19, 20, 64)
                .WithSeverity(DiagnosticSeverity.Error);
            var expected2 = VerifyCS.Diagnostic("ESQL001")
                .WithSpan(20, 37, 20, 63)
                .WithSeverity(DiagnosticSeverity.Error);

            await VerifyCS.VerifyAnalyzerAsync(code, expected1, expected2);
        }

        [Fact]
        public async Task Delete_WithWhere_DoesNotReportDiagnostic()
        {
            var code = @"

namespace EricksonLopez.SqlBuilder.Abstractions
{
    public interface ISqlQuery { }
}

namespace EricksonLopez.SqlBuilder
{
    public class DbConnection { public Task ExecuteAsync(object query) => Task.CompletedTask; }
    public class Sql {
        public static DeleteQuery<T> Delete<T>() => new DeleteQuery<T>();
        public static SelectQuery<T> Select<T>() => new SelectQuery<T>();
    }
    public class DeleteQuery<T> : EricksonLopez.SqlBuilder.Abstractions.ISqlQuery {
        public DeleteQuery<T> Where(string c) => this;
        public DeleteQuery<T> And(string c) => this;
        public DeleteQuery<T> Or(string c) => this;
        public object Build() => this;
    }
    public class SelectQuery<T> : EricksonLopez.SqlBuilder.Abstractions.ISqlQuery {
        public object Build() => this;
    }
    public class User {}

    public class TestClass
    {
        public async Task TestMethod()
        {
            var conn = new DbConnection();
            await conn.ExecuteAsync(Sql.Delete<User>().Where(""id"").Build());
            await conn.ExecuteAsync(Sql.Delete<User>().And(""id"").Build());
            await conn.ExecuteAsync(Sql.Delete<User>().Or(""id"").Build());
            // To cover hasDelete = false
            await conn.ExecuteAsync(Sql.Select<User>().Build());
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(code); // Expects no diagnostics
        }

        [Fact]
        public async Task Delete_WithWhereAll_DoesNotReportDiagnostic()
        {
            var code = @"

namespace EricksonLopez.SqlBuilder.Abstractions
{
    public interface ISqlQuery { }
}

namespace EricksonLopez.SqlBuilder
{
    public class DbConnection { public Task ExecuteAsync(object query) => Task.CompletedTask; }
    public class Sql { public static DeleteQuery<T> Delete<T>() => new DeleteQuery<T>(); }
    public class DeleteQuery<T> : EricksonLopez.SqlBuilder.Abstractions.ISqlQuery {
        public DeleteQuery<T> WhereAll() => this;
        public object Build() => this;
    }
    public class User {}

    public class TestClass
    {
        public async Task TestMethod()
        {
            var conn = new DbConnection();
            await conn.ExecuteAsync(Sql.Delete<User>().WhereAll().Build());
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(code); // WhereAll() satisfies the requirement
        }

        [Fact]
        public async Task Delete_WithWhereExists_DoesNotReportDiagnostic()
        {
            var code = @"

namespace EricksonLopez.SqlBuilder.Abstractions
{
    public interface ISqlQuery { }
}

namespace EricksonLopez.SqlBuilder
{
    public class DbConnection { public Task ExecuteAsync(object query) => Task.CompletedTask; }
    public class Sql { public static DeleteQuery<T> Delete<T>() => new DeleteQuery<T>(); }
    public class DeleteQuery<T> : EricksonLopez.SqlBuilder.Abstractions.ISqlQuery {
        public DeleteQuery<T> WhereExists(EricksonLopez.SqlBuilder.Abstractions.ISqlQuery subquery) => this;
        public DeleteQuery<T> WhereNotExists(EricksonLopez.SqlBuilder.Abstractions.ISqlQuery subquery) => this;
        public object Build() => this;
    }
    public class User {}

    public class TestClass
    {
        public async Task TestMethod()
        {
            var conn = new DbConnection();
            EricksonLopez.SqlBuilder.Abstractions.ISqlQuery sub = null;
            await conn.ExecuteAsync(Sql.Delete<User>().WhereExists(sub).Build());
            await conn.ExecuteAsync(Sql.Delete<User>().WhereNotExists(sub).Build());
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(code); // WhereExists / WhereNotExists satisfy the requirement
        }

        // ─────────────────────────────────────────────────────────────────────
        // ESQL003 — UPDATE without WHERE
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task Update_Build_WithoutWhere_ReportsDiagnostic()
        {
            var code = @"
namespace EricksonLopez.SqlBuilder.Abstractions
{
    public interface ISqlQuery { }
}

namespace EricksonLopez.SqlBuilder
{
    public class Sql { public static UpdateQuery<T> Update<T>() => new UpdateQuery<T>(); }
    public class UpdateQuery<T> : EricksonLopez.SqlBuilder.Abstractions.ISqlQuery {
        public UpdateQuery<T> Set<TVal>(string col, TVal val) => this;
        public object Build() => this;
    }
    public class User {}

    public class TestClass
    {
        public void TestMethod()
        {
            Sql.Update<User>().Set(""name"", ""Alice"").Build();
        }
    }
}";
            var expected = VerifyCS.Diagnostic("ESQL003")
                .WithSpan(20, 13, 20, 60)
                .WithSeverity(DiagnosticSeverity.Error);

            await VerifyCS.VerifyAnalyzerAsync(code, expected);
        }

        [Fact]
        public async Task Update_WithWhere_DoesNotReportDiagnostic()
        {
            var code = @"
namespace EricksonLopez.SqlBuilder.Abstractions
{
    public interface ISqlQuery { }
}

namespace EricksonLopez.SqlBuilder
{
    public class Sql { public static UpdateQuery<T> Update<T>() => new UpdateQuery<T>(); }
    public class UpdateQuery<T> : EricksonLopez.SqlBuilder.Abstractions.ISqlQuery {
        public UpdateQuery<T> Set<TVal>(string col, TVal val) => this;
        public UpdateQuery<T> Where(string c) => this;
        public object Build() => this;
    }
    public class User {}

    public class TestClass
    {
        public void TestMethod()
        {
            Sql.Update<User>().Set(""name"", ""Alice"").Where(""id = 1"").Build();
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(code); // No diagnostics expected
        }

        [Fact]
        public async Task Update_WithWhereAll_DoesNotReportDiagnostic()
        {
            var code = @"
namespace EricksonLopez.SqlBuilder.Abstractions
{
    public interface ISqlQuery { }
}

namespace EricksonLopez.SqlBuilder
{
    public class Sql { public static UpdateQuery<T> Update<T>() => new UpdateQuery<T>(); }
    public class UpdateQuery<T> : EricksonLopez.SqlBuilder.Abstractions.ISqlQuery {
        public UpdateQuery<T> Set<TVal>(string col, TVal val) => this;
        public UpdateQuery<T> WhereAll() => this;
        public object Build() => this;
    }
    public class User {}

    public class TestClass
    {
        public void TestMethod()
        {
            Sql.Update<User>().Set(""name"", ""Alice"").WhereAll().Build();
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(code); // WhereAll() satisfies the requirement
        }

        [Fact]
        public void Rules_Metadata_IsConfiguredProperly()
        {
            var analyzer = new DeleteWithoutWhereAnalyzer();
            var delRule = analyzer.SupportedDiagnostics[0];
            AwesomeAssertions.AssertionExtensions.Should(delRule.Id).Be("ESQL001");
            AwesomeAssertions.AssertionExtensions.Should(delRule.Title.ToString()).Be("DELETE without WHERE clause");
            AwesomeAssertions.AssertionExtensions.Should(delRule.MessageFormat.ToString()).Contain("DELETE will affect the entire table");
            AwesomeAssertions.AssertionExtensions.Should(delRule.Category).Be("Usage");
            AwesomeAssertions.AssertionExtensions.Should(delRule.DefaultSeverity).Be(DiagnosticSeverity.Error);
            AwesomeAssertions.AssertionExtensions.Should(delRule.IsEnabledByDefault).BeTrue();
            AwesomeAssertions.AssertionExtensions.Should(delRule.Description.ToString()).Contain("Avoid accidentally deleting all rows");
            AwesomeAssertions.AssertionExtensions.Should(delRule.HelpLinkUri).Be("https://github.com/ericksonlopezf/dotnet-sql-builder/blob/main/docs/analyzers/ESQL001.md");

            var updRule = analyzer.SupportedDiagnostics[1];
            AwesomeAssertions.AssertionExtensions.Should(updRule.Id).Be("ESQL003");
            AwesomeAssertions.AssertionExtensions.Should(updRule.Title.ToString()).Be("UPDATE without WHERE clause");
            AwesomeAssertions.AssertionExtensions.Should(updRule.MessageFormat.ToString()).Contain("UPDATE will affect the entire table");
            AwesomeAssertions.AssertionExtensions.Should(updRule.Category).Be("Usage");
            AwesomeAssertions.AssertionExtensions.Should(updRule.DefaultSeverity).Be(DiagnosticSeverity.Error);
            AwesomeAssertions.AssertionExtensions.Should(updRule.IsEnabledByDefault).BeTrue();
            AwesomeAssertions.AssertionExtensions.Should(updRule.Description.ToString()).Contain("Avoid accidentally updating all rows");
            AwesomeAssertions.AssertionExtensions.Should(updRule.HelpLinkUri).Be("https://github.com/ericksonlopezf/dotnet-sql-builder/blob/main/docs/analyzers/ESQL003.md");
        }

        [Fact]
        public async Task LocalVariable_Delete_WithSeparateWhere_DoesNotReportDiagnostic()
        {
            var code = @"
namespace EricksonLopez.SqlBuilder.Abstractions
{
    public interface ISqlQuery { }
}
namespace EricksonLopez.SqlBuilder
{
    public class Sql { public static DeleteQuery<T> Delete<T>() => new DeleteQuery<T>(); }
    public class DeleteQuery<T> : EricksonLopez.SqlBuilder.Abstractions.ISqlQuery {
        public DeleteQuery<T> Where(string c) => this;
        public object Build() => this;
    }
    public class User {}
    public class TestClass
    {
        public void TestMethod()
        {
            var query = Sql.Delete<User>();
            query.Where(""id = 1"");
            query.Build();
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(code);
        }

        [Fact]
        public async Task LocalVariable_Delete_WithAssignmentWhere_DoesNotReportDiagnostic()
        {
            var code = @"
namespace EricksonLopez.SqlBuilder.Abstractions
{
    public interface ISqlQuery { }
}
namespace EricksonLopez.SqlBuilder
{
    public class Sql { public static DeleteQuery<T> Delete<T>() => new DeleteQuery<T>(); }
    public class DeleteQuery<T> : EricksonLopez.SqlBuilder.Abstractions.ISqlQuery {
        public DeleteQuery<T> Where(string c) => this;
        public object Build() => this;
    }
    public class User {}
    public class TestClass
    {
        public void TestMethod()
        {
            var query = Sql.Delete<User>();
            query = query.Where(""id = 1"");
            query.Build();
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(code);
        }

        [Fact]
        public async Task LocalVariable_Delete_WithoutWhere_ReportsDiagnosticOnBuild()
        {
            var code = @"
namespace EricksonLopez.SqlBuilder.Abstractions
{
    public interface ISqlQuery { }
}
namespace EricksonLopez.SqlBuilder
{
    public class Sql { public static DeleteQuery<T> Delete<T>() => new DeleteQuery<T>(); }
    public class DeleteQuery<T> : EricksonLopez.SqlBuilder.Abstractions.ISqlQuery {
        public object Build() => this;
    }
    public class User {}
    public class TestClass
    {
        public void TestMethod()
        {
            var query = Sql.Delete<User>();
            query.Build();
        }
    }
}";
            var expected = VerifyCS.Diagnostic("ESQL001")
                .WithSpan(18, 13, 18, 26)
                .WithSeverity(DiagnosticSeverity.Error);
            await VerifyCS.VerifyAnalyzerAsync(code, expected);
        }

        [Fact]
        public async Task LocalVariable_Update_WithoutWhere_ReportsDiagnosticOnBuild()
        {
            var code = @"
namespace EricksonLopez.SqlBuilder.Abstractions
{
    public interface ISqlQuery { }
}
namespace EricksonLopez.SqlBuilder
{
    public class Sql { public static UpdateQuery<T> Update<T>() => new UpdateQuery<T>(); }
    public class UpdateQuery<T> : EricksonLopez.SqlBuilder.Abstractions.ISqlQuery {
        public UpdateQuery<T> Set<TVal>(string col, TVal val) => this;
        public object Build() => this;
    }
    public class User {}
    public class TestClass
    {
        public void TestMethod()
        {
            var query = Sql.Update<User>().Set(""name"", ""Bob"");
            query.Build();
        }
    }
}";
            var expected = VerifyCS.Diagnostic("ESQL003")
                .WithSpan(19, 13, 19, 26)
                .WithSeverity(DiagnosticSeverity.Error);
            await VerifyCS.VerifyAnalyzerAsync(code, expected);
        }

        [Fact]
        public async Task Delete_WithOrExists_DoesNotReportDiagnostic()
        {
            var code = @"
namespace EricksonLopez.SqlBuilder.Abstractions
{
    public interface ISqlQuery { }
}
namespace EricksonLopez.SqlBuilder
{
    public class DbConnection { public Task ExecuteAsync(object query) => Task.CompletedTask; }
    public class Sql { public static DeleteQuery<T> Delete<T>() => new DeleteQuery<T>(); }
    public class DeleteQuery<T> : EricksonLopez.SqlBuilder.Abstractions.ISqlQuery {
        public DeleteQuery<T> OrExists(EricksonLopez.SqlBuilder.Abstractions.ISqlQuery subquery) => this;
        public DeleteQuery<T> OrNotExists(EricksonLopez.SqlBuilder.Abstractions.ISqlQuery subquery) => this;
        public object Build() => this;
    }
    public class User {}
    public class TestClass
    {
        public async Task TestMethod()
        {
            var conn = new DbConnection();
            EricksonLopez.SqlBuilder.Abstractions.ISqlQuery sub = null;
            await conn.ExecuteAsync(Sql.Delete<User>().OrExists(sub).Build());
            await conn.ExecuteAsync(Sql.Delete<User>().OrNotExists(sub).Build());
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(code);
        }

        [Fact]
        public async Task LocalVariable_ExecuteAsync_WithoutWhere_ReportsDiagnostic()
        {
            var code = @"
namespace EricksonLopez.SqlBuilder.Abstractions
{
    public interface ISqlQuery { }
}
namespace EricksonLopez.SqlBuilder
{
    public class DbConnection { public Task ExecuteAsync(object query) => Task.CompletedTask; }
    public class Sql { public static DeleteQuery<T> Delete<T>() => new DeleteQuery<T>(); }
    public class DeleteQuery<T> : EricksonLopez.SqlBuilder.Abstractions.ISqlQuery {
        public object Build() => this;
    }
    public class User {}
    public class TestClass
    {
        public async Task TestMethod()
        {
            var conn = new DbConnection();
            var query = Sql.Delete<User>();
            await conn.ExecuteAsync(query);
        }
    }
}";
            var expected = VerifyCS.Diagnostic("ESQL001")
                .WithSpan(20, 19, 20, 43)
                .WithSeverity(DiagnosticSeverity.Error);
            await VerifyCS.VerifyAnalyzerAsync(code, expected);
        }

        [Fact]
        public async Task LocalVariable_ExecuteAsync_WithWhere_DoesNotReportDiagnostic()
        {
            var code = @"
namespace EricksonLopez.SqlBuilder.Abstractions
{
    public interface ISqlQuery { }
}
namespace EricksonLopez.SqlBuilder
{
    public class DbConnection { public Task ExecuteAsync(object query) => Task.CompletedTask; }
    public class Sql { public static DeleteQuery<T> Delete<T>() => new DeleteQuery<T>(); }
    public class DeleteQuery<T> : EricksonLopez.SqlBuilder.Abstractions.ISqlQuery {
        public DeleteQuery<T> Where(string c) => this;
        public object Build() => this;
    }
    public class User {}
    public class TestClass
    {
        public async Task TestMethod()
        {
            var conn = new DbConnection();
            var query = Sql.Delete<User>();
            query = query.Where(""id = 1"");
            await conn.ExecuteAsync(query);
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(code);
        }

        [Fact]
        public async Task ExpressionBodiedMethod_WithoutWhere_ReportsDiagnostic()
        {
            var code = @"
namespace EricksonLopez.SqlBuilder.Abstractions
{
    public interface ISqlQuery { }
}
namespace EricksonLopez.SqlBuilder
{
    public class Sql { public static DeleteQuery<T> Delete<T>() => new DeleteQuery<T>(); }
    public class DeleteQuery<T> : EricksonLopez.SqlBuilder.Abstractions.ISqlQuery {
        public object Build() => this;
    }
    public class User {}
    public class TestClass
    {
        public object TestMethod() => Sql.Delete<User>().Build();
    }
}";
            var expected = VerifyCS.Diagnostic("ESQL001")
                .WithSpan(15, 39, 15, 65)
                .WithSeverity(DiagnosticSeverity.Error);
            await VerifyCS.VerifyAnalyzerAsync(code, expected);
        }

        [Fact]
        public async Task ThirdParty_Delete_DoesNotReportDiagnostic()
        {
            var code = @"
namespace ThirdParty
{
    public class MyQuery
    {
        public MyQuery Delete() => this;
        public object Build() => this;
    }
}
namespace TestNamespace
{
    public class TestClass
    {
        public void Test()
        {
            var q = new ThirdParty.MyQuery().Delete();
            q.Build();
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(code);
        }

        [Fact]
        public async Task ExecuteAsync_ZeroArguments_DoesNotReportDiagnostic()
        {
            var code = @"
namespace TestNamespace
{
    public class DbConnection
    {
        public Task ExecuteAsync() => Task.CompletedTask;
    }
    public class TestClass
    {
        public async Task Test()
        {
            var conn = new DbConnection();
            await conn.ExecuteAsync();
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(code);
        }

        [Fact]
        public async Task AssignmentToOtherVariable_DoesNotSatisfyWhere_ReportsDiagnostic()
        {
            var code = @"
namespace EricksonLopez.SqlBuilder.Abstractions
{
    public interface ISqlQuery { }
}
namespace EricksonLopez.SqlBuilder
{
    public class Sql { public static DeleteQuery<T> Delete<T>() => new DeleteQuery<T>(); }
    public class DeleteQuery<T> : EricksonLopez.SqlBuilder.Abstractions.ISqlQuery {
        public DeleteQuery<T> Where(string c) => this;
        public object Build() => this;
    }
    public class User {}
    public class TestClass
    {
        public void TestMethod()
        {
            var query = Sql.Delete<User>();
            var other = Sql.Delete<User>();
            other = other.Where(""id = 1"");
            query.Build();
        }
    }
}";
            var expected = VerifyCS.Diagnostic("ESQL001")
                .WithSpan(21, 13, 21, 26)
                .WithSeverity(DiagnosticSeverity.Error);
            await VerifyCS.VerifyAnalyzerAsync(code, expected);
        }

        [Fact]
        public async Task NonMemberAccessAssignment_DoesNotCrash()
        {
            var code = @"
namespace EricksonLopez.SqlBuilder.Abstractions
{
    public interface ISqlQuery { }
}
namespace EricksonLopez.SqlBuilder
{
    public class Sql { public static DeleteQuery<T> Delete<T>() => new DeleteQuery<T>(); }
    public class DeleteQuery<T> : EricksonLopez.SqlBuilder.Abstractions.ISqlQuery {
        public object Build() => this;
    }
    public class User {}
    public class TestClass
    {
        private DeleteQuery<User> GetQ() => Sql.Delete<User>();
        public void TestMethod()
        {
            var query = Sql.Delete<User>();
            query = GetQ();
            query.Build();
        }
    }
}";
            var expected = VerifyCS.Diagnostic("ESQL001")
                .WithSpan(20, 13, 20, 26)
                .WithSeverity(DiagnosticSeverity.Error);
            await VerifyCS.VerifyAnalyzerAsync(code, expected);
        }
    }

    // Helper class for CSharpAnalyzerVerifier
    public static partial class VerifyCS
    {
        public static DiagnosticResult Diagnostic(string diagnosticId)
        {
            return CSharpAnalyzerVerifier<DeleteWithoutWhereAnalyzer, DefaultVerifier>.Diagnostic(diagnosticId);
        }

        public static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
        {
            var test = new CSharpAnalyzerTest<DeleteWithoutWhereAnalyzer, DefaultVerifier>
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





