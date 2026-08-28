// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;

namespace EricksonLopez.SqlBuilder.Analyzers.Tests
{
    public class AnalyzerEdgeCasesTests
    {
        [Fact]
        public async Task CodeFixProviders_MetadataAndFixAll()
        {
            var selectStarFix = new SelectStarCodeFix();
            Assert.Contains("SQL0003", selectStarFix.FixableDiagnosticIds);
            Assert.NotNull(selectStarFix.GetFixAllProvider());

            var sqlKataFix = new SqlKataMigrationCodeFixProvider();
            Assert.Contains("ESQL025", sqlKataFix.FixableDiagnosticIds);
            Assert.NotNull(sqlKataFix.GetFixAllProvider());

            var unsafeStringFix = new UnsafeStringConcatenationCodeFix();
            Assert.Contains("ESQL002", unsafeStringFix.FixableDiagnosticIds);
            Assert.NotNull(unsafeStringFix.GetFixAllProvider());

            var deleteFix = new DeleteWithoutWhereCodeFix();
            Assert.Contains("ESQL001", deleteFix.FixableDiagnosticIds);
            Assert.NotNull(deleteFix.GetFixAllProvider());

            var project = new AdhocWorkspace().AddProject("TestProj", LanguageNames.CSharp);
            var doc = project.AddDocument("Empty.cs", "class C {}");
            var diagEmpty = Diagnostic.Create(new DiagnosticDescriptor("D001", "title", "msg", "cat", DiagnosticSeverity.Warning, true), Location.None);

            // 1. SelectStarCodeFix direct registration
            var docStar = project.AddDocument("Star.cs", "class C { void M() { var s = \"*\"; } }");
            var treeStar = await docStar.GetSyntaxTreeAsync();
            var nodeStar = treeStar!.GetRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().First();
            var diagStar = Diagnostic.Create(new DiagnosticDescriptor("SQL0003", "title", "msg", "cat", DiagnosticSeverity.Warning, true), nodeStar.GetLocation());
            var actionsStar = new List<CodeAction>();
            var ctxStar = new CodeFixContext(docStar, diagStar, (a, d) => actionsStar.Add(a), CancellationToken.None);
            await selectStarFix.RegisterCodeFixesAsync(ctxStar);
            Assert.Single(actionsStar);
            Assert.Equal("Replace '*' with specific columns", actionsStar[0].Title);
            Assert.Equal("SelectStarLiteralFix", actionsStar[0].EquivalenceKey);

            var actionsStarNull = new List<CodeAction>();
            var ctxStarNull = new CodeFixContext(doc, diagEmpty, (a, d) => actionsStarNull.Add(a), CancellationToken.None);
            await selectStarFix.RegisterCodeFixesAsync(ctxStarNull);
            Assert.Empty(actionsStarNull);

            // 2. SqlKataMigrationCodeFixProvider direct registration
            var docKata = project.AddDocument("Kata.cs", "class C { void M() { var q = new Query(); } }");
            var treeKata = await docKata.GetSyntaxTreeAsync();
            var nodeKata = treeKata!.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>().First();
            var diagKata = Diagnostic.Create(new DiagnosticDescriptor("ESQL025", "title", "msg", "cat", DiagnosticSeverity.Warning, true), nodeKata.GetLocation());
            var actionsKata = new List<CodeAction>();
            var ctxKata = new CodeFixContext(docKata, diagKata, (a, d) => actionsKata.Add(a), CancellationToken.None);
            await sqlKataFix.RegisterCodeFixesAsync(ctxKata);
            Assert.Single(actionsKata);
            Assert.Equal("Migrate to Sql.From", actionsKata[0].Title);
            Assert.Equal("Migrate to Sql.From", actionsKata[0].EquivalenceKey);

            var docKataChainedWhere = project.AddDocument("KataWhere.cs", "class C { void M() { var q = new Query().Where(\"x = 1\"); } }");
            var treeKataWhere = await docKataChainedWhere.GetSyntaxTreeAsync();
            var nodeKataWhere = treeKataWhere!.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>().First();
            var diagKataWhere = Diagnostic.Create(new DiagnosticDescriptor("ESQL025", "title", "msg", "cat", DiagnosticSeverity.Warning, true), nodeKataWhere.GetLocation());
            var actionsKataWhere = new List<CodeAction>();
            var ctxKataWhere = new CodeFixContext(docKataChainedWhere, diagKataWhere, (a, d) => actionsKataWhere.Add(a), CancellationToken.None);
            await sqlKataFix.RegisterCodeFixesAsync(ctxKataWhere);
            Assert.Single(actionsKataWhere);
            var ops = await actionsKataWhere[0].GetOperationsAsync(CancellationToken.None);
            var applyOp = ops.OfType<ApplyChangesOperation>().First();
            var newDocWhere = applyOp.ChangedSolution.GetDocument(docKataChainedWhere.Id);
            var changedTextWhere = (await newDocWhere!.GetTextAsync()).ToString();
            Assert.Contains("Sql.From(\"Unknown\").Where(\"x = 1\")", changedTextWhere);

            var actionsKataNull = new List<CodeAction>();
            var ctxKataNull = new CodeFixContext(doc, diagEmpty, (a, d) => actionsKataNull.Add(a), CancellationToken.None);
            await sqlKataFix.RegisterCodeFixesAsync(ctxKataNull);
            Assert.Empty(actionsKataNull);

            // 3. UnsafeStringConcatenationCodeFix direct registration
            var docConcat = project.AddDocument("Concat.cs", "class C { void M() { var s = \"a\" + \"b\"; } }");
            var treeConcat = await docConcat.GetSyntaxTreeAsync();
            var nodeConcat = treeConcat!.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().First();
            var diagConcat = Diagnostic.Create(new DiagnosticDescriptor("ESQL002", "title", "msg", "cat", DiagnosticSeverity.Warning, true), nodeConcat.GetLocation());
            var actionsConcat = new List<CodeAction>();
            var ctxConcat = new CodeFixContext(docConcat, diagConcat, (a, d) => actionsConcat.Add(a), CancellationToken.None);
            await unsafeStringFix.RegisterCodeFixesAsync(ctxConcat);
            Assert.Single(actionsConcat);
            Assert.Equal("Convert to string interpolation", actionsConcat[0].Title);
            Assert.Equal("InterpolatedStringFix", actionsConcat[0].EquivalenceKey);

            var actionsConcatNull = new List<CodeAction>();
            var ctxConcatNull = new CodeFixContext(doc, diagEmpty, (a, d) => actionsConcatNull.Add(a), CancellationToken.None);
            await unsafeStringFix.RegisterCodeFixesAsync(ctxConcatNull);
            Assert.Empty(actionsConcatNull);
        }

        [Fact]
        public async Task CartesianJoinAnalyzer_EdgeCases()
        {
            var code = @"
namespace EricksonLopez.SqlBuilder
{
    public class SelectQuery
    {
        public SelectQuery RawJoin() => this;
        public SelectQuery RawJoin(string raw) => this;
        public SelectQuery RawJoin(FormattableString raw) => this;
        public SelectQuery Join(string table, string on = """") => this;
        public SelectQuery InnerJoin(string table, string on = """") => this;
        public SelectQuery LeftJoin(string table, string on = """") => this;
        public SelectQuery RightJoin(string table, string on = """") => this;
        public SelectQuery FullJoin(string table, string on = """") => this;
    }
}

namespace TestNamespace
{
    using EricksonLopez.SqlBuilder;

    public class TestClass
    {
        public void TestMethod()
        {
            var q = new SelectQuery();
            q.RawJoin();
            q.RawJoin("""");
            q.RawJoin(""CROSS JOIN Users"");
            q.RawJoin($""JOIN Users u ON u.Id = 1"");
            q.Join(""Users"", on: ""u.Id = 1"");
            q.InnerJoin(""Users"", on: {|#0:""""|});
            q.LeftJoin(""Users"", on: {|#1:""   ""|});
            {|#2:q.RawJoin(""JOIN Users u"")|};
        }
    }
}";
            var expected0 = CSharpAnalyzerVerifier<CartesianJoinAnalyzer, DefaultVerifier>.Diagnostic("ESQL024").WithLocation(0).WithArguments("InnerJoin");
            var expected1 = CSharpAnalyzerVerifier<CartesianJoinAnalyzer, DefaultVerifier>.Diagnostic("ESQL024").WithLocation(1).WithArguments("LeftJoin");
            var expected2 = CSharpAnalyzerVerifier<CartesianJoinAnalyzer, DefaultVerifier>.Diagnostic("ESQL024").WithLocation(2).WithArguments("RawJoin");
            await VerifyAnalyzerAsync<CartesianJoinAnalyzer>(code, expected0, expected1, expected2);
        }

        [Fact]
        public async Task DialectSpecificOverloadAnalyzer_EdgeCases()
        {
            var code = @"
namespace EricksonLopez.SqlBuilder
{
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public class RequiresCapabilityAttribute : System.Attribute
    {
        public RequiresCapabilityAttribute() { }
        public RequiresCapabilityAttribute(string capability) { }
    }

    public static class SqlExtensions
    {
        [RequiresCapability(""JsonAgg"")]
        public static void MethodWithCap() { }

        [RequiresCapability(null)]
        public static void MethodWithNullCap() { }

        [RequiresCapability]
        public static void MethodWithEmptyCap() { }

        public static void MethodWithoutCap() { }
    }
}

namespace TestNamespace
{
    using EricksonLopez.SqlBuilder;

    public class TestClass
    {
        public void TestMethod()
        {
            {|#0:SqlExtensions.MethodWithCap()|};
            {|#1:SqlExtensions.MethodWithNullCap()|};
            SqlExtensions.MethodWithEmptyCap();
            SqlExtensions.MethodWithoutCap();
            UnresolvedMethod();
        }
    }
}";
            var expected0 = CSharpAnalyzerVerifier<DialectSpecificOverloadAnalyzer, DefaultVerifier>.Diagnostic("ESQL020").WithLocation(0).WithArguments("MethodWithCap", "JsonAgg");
            var expected1 = CSharpAnalyzerVerifier<DialectSpecificOverloadAnalyzer, DefaultVerifier>.Diagnostic("ESQL020").WithLocation(1).WithArguments("MethodWithNullCap", "Unknown");
            await VerifyAnalyzerAsync<DialectSpecificOverloadAnalyzer>(code, expected0, expected1);
        }

        [Fact]
        public async Task DynamicIdentifierAnalyzer_ExternalNamespace_AndZeroArgs_NoDiagnostic()
        {
            var code = @"
namespace ExternalLibrary
{
    public class ExternalQuery
    {
        public ExternalQuery From() => this;
        public ExternalQuery From(string s) => this;
        public ExternalQuery OtherMethod(string s) => this;
    }
}

namespace TestNamespace
{
    using ExternalLibrary;

    public class TestClass
    {
        public void TestMethod(string dynamicTable)
        {
            var q = new ExternalQuery();
            q.From();
            q.From(""prefix_"" + dynamicTable);
            q.OtherMethod(""prefix_"" + dynamicTable);
            UnresolvedFrom(""prefix_"" + dynamicTable);
        }
    }
}";
            await VerifyAnalyzerAsync<DynamicIdentifierAnalyzer>(code);
        }

        [Fact]
        public async Task JoinConditionAnalyzer_EdgeCases()
        {
            var code = @"
namespace EricksonLopez.SqlBuilder
{
    public class SelectQuery
    {
        public SelectQuery Join() => this;
        public SelectQuery Join(string table, Func<User, Order, bool> predicate) => this;
        public SelectQuery InnerJoin(string table, string alias, Func<User, Order, bool> predicate) => this;
    }

    public class User { public int Id { get; set; } }
    public class Order { public int UserId { get; set; } public string Code { get; set; } }
}

namespace TestNamespace
{
    using EricksonLopez.SqlBuilder;

    public class TestClass
    {
        public void TestMethod()
        {
            var q = new SelectQuery();
            q.Join();
            q.Join(""Orders"", (u, o) => u.Id == 123);
            q.Join(""Orders"", (u, o) => 123 == o.UserId);
            q.Join(""Orders"", (u, o) => u.Id != o.UserId);
            q.InnerJoin(""Orders"", ""o"", (u, o) => {|#0:u.Id == o.Code|});
        }
    }
}";
            var expected0 = CSharpAnalyzerVerifier<JoinConditionAnalyzer, DefaultVerifier>.Diagnostic("ESQL006").WithLocation(0).WithArguments("int", "string");
            await VerifyAnalyzerAsync<JoinConditionAnalyzer>(code, expected0);
        }

        [Fact]
        public async Task MissingColumnAnalyzer_OrderByDescending_GroupBy_AndOpenGeneric_Tests()
        {
            var code = @"
namespace EricksonLopez.SqlBuilder
{
    public class SelectQuery<T>
    {
        public SelectQuery<T> Select(params string[] cols) => this;
        public SelectQuery<T> OrderBy(params string[] cols) => this;
        public SelectQuery<T> OrderByDescending(params string[] cols) => this;
        public SelectQuery<T> GroupBy(params string[] cols) => this;
    }
}

namespace TestNamespace
{
    using EricksonLopez.SqlBuilder;

    public class UserDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class GenericRepo<T>
    {
        public void Query(SelectQuery<T> q)
        {
            q.Select(""AnyCol"");
        }
    }

    public class TestClass
    {
        public void TestMethod()
        {
            var q = new SelectQuery<UserDto>();
            q.OrderBy(""Name"");
            q.OrderByDescending(""Name"");
            q.GroupBy(""Id"");
            q.OrderBy({|#0:""NonExistentCol""|});
            q.OrderByDescending({|#1:""BadOrderCol""|});
            q.GroupBy({|#2:""BadGroupCol""|});
        }
    }
}";
            var expected0 = CSharpAnalyzerVerifier<MissingColumnAnalyzer, DefaultVerifier>.Diagnostic("SQL0009").WithLocation(0).WithArguments("NonExistentCol", "UserDto");
            var expected1 = CSharpAnalyzerVerifier<MissingColumnAnalyzer, DefaultVerifier>.Diagnostic("SQL0009").WithLocation(1).WithArguments("BadOrderCol", "UserDto");
            var expected2 = CSharpAnalyzerVerifier<MissingColumnAnalyzer, DefaultVerifier>.Diagnostic("SQL0009").WithLocation(2).WithArguments("BadGroupCol", "UserDto");
            await VerifyAnalyzerAsync<MissingColumnAnalyzer>(code, expected0, expected1, expected2);
        }

        [Fact]
        public async Task MissingIndexAnalyzer_And_RedundantWhere_EmptyArgs_NoDiagnostic()
        {
            var code = @"
namespace EricksonLopez.SqlBuilder
{
    public class Query
    {
        public Query Where() => this;
        public Query Where(string col, object val) => this;
    }
}

namespace TestNamespace
{
    using EricksonLopez.SqlBuilder;

    public class TestClass
    {
        public void TestMethod()
        {
            var q = new Query();
            q.Where();
        }
    }
}";
            await VerifyAnalyzerAsync<MissingIndexAnalyzer>(code);
            await VerifyAnalyzerAsync<RedundantWhereAnalyzer>(code);
        }

        [Fact]
        public async Task MissingSourceGeneratorAnalyzer_NonGenericInterface_ReportsDiagnostic()
        {
            var code = @"
namespace EricksonLopez.SqlBuilder.Annotations
{
    public class SqlEntityAttribute : System.Attribute { }
}

namespace EricksonLopez.SqlBuilder.Abstractions.Metadata
{
    public interface IStaticEntityMetadata<T> { }
}

namespace TestNamespace
{
    using EricksonLopez.SqlBuilder.Annotations;

    [SqlEntity]
    public class {|#0:Customer|} : System.IDisposable
    {
        public int Id { get; set; }
        public void Dispose() { }
    }
}";
            var expected0 = CSharpAnalyzerVerifier<MissingSourceGeneratorAnalyzer, DefaultVerifier>.Diagnostic("ESQL021").WithLocation(0).WithArguments("Customer");
            await VerifyAnalyzerAsync<MissingSourceGeneratorAnalyzer>(code, expected0);
        }

        [Fact]
        public async Task RetryInsideTransactionAnalyzer_PollyExecute_AndOtherHelpers()
        {
            var code = @"
namespace Polly
{
    public static class RetryHelper
    {
        public static void Execute(Action a) { a(); }
    }
}

namespace OtherLib
{
    public static class SafeHelper
    {
        public static void ExecuteAsync(Action a) { a(); }
    }
}

namespace TestNamespace
{
    using Polly;
    using OtherLib;

    public class UnitOfWork
    {
        public void Commit() { }
    }

    public class TestClass
    {
        public void TestMethod(UnitOfWork uow)
        {
            SafeHelper.ExecuteAsync(() => uow.Commit());
            RetryHelper.Execute(() => {|#0:uow.Commit()|});
        }
    }
}";
            var expected0 = CSharpAnalyzerVerifier<RetryInsideTransactionAnalyzer, DefaultVerifier>.Diagnostic("ESQL012").WithLocation(0);
            await VerifyAnalyzerAsync<RetryInsideTransactionAnalyzer>(code, expected0);
        }

        [Fact]
        public async Task RawStringOverloadAnalyzer_StaticImport_ReportsDiagnostic()
        {
            var code = @"
namespace EricksonLopez.SqlBuilder
{
    public static class Sql
    {
        public static void Raw(string sql, object param = null) { }
    }
}

namespace TestNamespace
{
    using static EricksonLopez.SqlBuilder.Sql;

    public class TestClass
    {
        public void TestMethod()
        {
            {|#0:Raw(""SELECT 1"")|};
        }
    }
}";
            var expected0 = CSharpAnalyzerVerifier<RawStringOverloadAnalyzer, DefaultVerifier>.Diagnostic("ESQL011").WithLocation(0);
            await VerifyAnalyzerAsync<RawStringOverloadAnalyzer>(code, expected0);
        }

        [Fact]
        public async Task SyncOnUiThreadAnalyzer_And_TypeMapRegistrationAnalyzer_UnresolvedSymbols_NoDiagnostic()
        {
            var code = @"
namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            UnresolvedMethod();
        }
    }
}";
            await VerifyAnalyzerAsync<SyncOnUiThreadAnalyzer>(code);
            await VerifyAnalyzerAsync<TypeMapRegistrationAnalyzer>(code);
        }

        [Fact]
        public async Task DapperCompilerAnalyzer_UnresolvedSymbol_NoDiagnostic()
        {
            var code = @"
namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            UnresolvedMethod();
        }
    }
}";
            await VerifyAnalyzerAsync<DapperCompilerAnalyzer>(code);
        }

        [Fact]
        public async Task LargeOffsetAnalyzer_EdgeCases_NoDiagnostic()
        {
            var code = @"
namespace EricksonLopez.SqlBuilder
{
    public class SelectQuery<T>
    {
        public SelectQuery<T> Offset(int count) => this;
        public SelectQuery<T> Offset(string count) => this;
    }
}

namespace TestNamespace
{
    using EricksonLopez.SqlBuilder;

    public class TestClass
    {
        public void TestMethod(int variableOffset)
        {
            var q = new SelectQuery<object>();
            q.Offset(100);
            q.Offset(10000);
            q.Offset(variableOffset);
            q.Offset(""100"");
            UnresolvedOffset(20000);
        }
    }
}";
            await VerifyAnalyzerAsync<LargeOffsetAnalyzer>(code);
        }

        [Fact]
        public async Task LikeWildcardAnalyzer_EdgeCases_NoDiagnostic()
        {
            var code = @"
namespace TestNamespace
{
    public class Query
    {
        public Query WhereLike(string column, int count) => this;
        public Query WhereLike() => this;
    }

    public class TestClass
    {
        public void TestMethod()
        {
            var q = new Query();
            q.WhereLike(""Name"", 123);
            q.WhereLike();
            UnresolvedMethod();
        }
    }
} ";
            await VerifyAnalyzerAsync<LikeWildcardAnalyzer>(code);
        }

        [Fact]
        public async Task MergeQueryAnalyzer_EdgeCases_NoDiagnostic()
        {
            var code = @"
namespace OtherNamespace
{
    public class OtherClass
    {
        public void Merge() { }
        public void Merge(int a, int b) { }
        public void OtherMethod() { }
    }
}

namespace TestNamespace
{
    using OtherNamespace;

    public class TestClass
    {
        public void TestMethod()
        {
            var o = new OtherClass();
            o.Merge();
            o.OtherMethod();
            o.Merge(1); // candidate mismatch
            Merge();    // unresolved Merge
        }
    }
}";
            await VerifyAnalyzerAsync<MergeQueryAnalyzer>(code);
        }

        [Fact]
        public async Task MissingColumnAnalyzer_SelectStar_AndValidColumns_NoDiagnostic()
        {
            var code = @"
namespace EricksonLopez.SqlBuilder
{
    public class SelectQuery<T>
    {
        public SelectQuery<T> Select(params string[] columns) => this;
    }
}

namespace TestNamespace
{
    using EricksonLopez.SqlBuilder;

    public class UserDto
    {
        public int Id { get; set; }
        public string UserName { get; set; }
    }

    public class TestClass
    {
        public void TestMethod()
        {
            var q = new SelectQuery<UserDto>();
            q.Select(""*"");
            q.Select(""Id"", ""user_name"");
            UnresolvedSelect();
        }
    }
}";
            await VerifyAnalyzerAsync<MissingColumnAnalyzer>(code);
        }

        [Fact]
        public async Task MissingSourceGeneratorAnalyzer_WhenImplemented_NoDiagnostic()
        {
            var code = @"
namespace EricksonLopez.SqlBuilder.Annotations
{
    public class SqlEntityAttribute : System.Attribute
    {
        public string TableName { get; set; }
    }
}

namespace EricksonLopez.SqlBuilder.Abstractions.Metadata
{
    public interface IStaticEntityMetadata<T> { }
}

namespace TestNamespace
{
    using EricksonLopez.SqlBuilder.Annotations;
    using EricksonLopez.SqlBuilder.Abstractions.Metadata;

    [SqlEntity(TableName = ""Users"")]
    public class UserEntity : IStaticEntityMetadata<UserEntity>
    {
        public int Id { get; set; }
    }
}";
            await VerifyAnalyzerAsync<MissingSourceGeneratorAnalyzer>(code);
        }

        [Fact]
        public async Task MissingSourceGeneratorAnalyzer_NoAttributeInCompilation_NoDiagnostic()
        {
            var code = @"
namespace TestNamespace
{
    public class PlainClass
    {
        public int Id { get; set; }
    }
}";
            await VerifyAnalyzerAsync<MissingSourceGeneratorAnalyzer>(code);
        }

        [Fact]
        public async Task QueryPerformanceAnalyzer_EdgeCases_NoDiagnostic()
        {
            var code = @"
namespace OtherNamespace
{
    public class OtherQuery
    {
        public void Where(Func<string, bool> predicate) { }
    }
}

namespace TestNamespace
{
    using OtherNamespace;

    public class TestClass
    {
        public void TestMethod()
        {
            var o = new OtherQuery();
            o.Where(x => x.ToString() == ""test"");
            o.Where(x => UnresolvedMethodInLambda());
            Action act = () => { var s = ""hello"".ToString(); };
            act();
            UnresolvedMethod();
        }
    }
}";
            await VerifyAnalyzerAsync<QueryPerformanceAnalyzer>(code);
        }

        [Fact]
        public async Task RawStringOverloadAnalyzer_EdgeCases_NoDiagnostic()
        {
            var code = @"
namespace EricksonLopez.SqlBuilder
{
    public static class Sql
    {
        public static void Raw() { }
        public static void Raw(FormattableString sql) { }
        public static void OtherMethod(string s) { }
    }
}

namespace TestNamespace
{
    using EricksonLopez.SqlBuilder;

    public class TestClass
    {
        public void TestMethod()
        {
            Sql.Raw();
            Sql.Raw($""SELECT 1"");
            Sql.OtherMethod(""SELECT 1"");
            UnresolvedRaw();
        }
    }
}";
            await VerifyAnalyzerAsync<RawStringOverloadAnalyzer>(code);
        }

        [Fact]
        public async Task RetryInsideTransactionAnalyzer_EdgeCases()
        {
            var code = @"
namespace Polly
{
    public class ResiliencePipeline
    {
        public void ExecuteAsync(Action act) { act(); }
    }
}

namespace TestNamespace
{
    using Polly;

    public class TestClass
    {
        public void TestMethod(ResiliencePipeline pipeline, dynamic uow)
        {
            Action plainAction = () => { };
            pipeline.ExecuteAsync(plainAction);

            void ExecuteAsync(Action a) { a(); }
            ExecuteAsync(() => { });

            pipeline.ExecuteAsync(() =>
            {
                {|#0:uow.CommitAsync()|};
            });
        }
    }
}";
            var expected0 = CSharpAnalyzerVerifier<RetryInsideTransactionAnalyzer, DefaultVerifier>.Diagnostic("ESQL012").WithLocation(0);
            await VerifyAnalyzerAsync<RetryInsideTransactionAnalyzer>(code, expected0);
        }

        [Fact]
        public async Task UnsafeStringConcatenationAnalyzer_EdgeCases_NoDiagnostic()
        {
            var code = @"
namespace EricksonLopez.SqlBuilder
{
    public class RawQuery
    {
        public RawQuery RawWhere() => this;
        public RawQuery RawWhere(string s) => this;
        public RawQuery OtherMethod(string s) => this;
    }
}

namespace TestNamespace
{
    using EricksonLopez.SqlBuilder;

    public class TestClass
    {
        public void TestMethod()
        {
            var q = new RawQuery();
            q.RawWhere();
            q.RawWhere(""SELECT 1"");
            q.OtherMethod(""SELECT "" + ""1"");
            UnresolvedMethod();
        }
    }
}";
            await VerifyAnalyzerAsync<UnsafeStringConcatenationAnalyzer>(code);
        }

        [Fact]
        public async Task DynamicIdentifierAnalyzer_ConstantHoleAndPlainString_NoDiagnostic()
        {
            var code = @"
namespace EricksonLopez.SqlBuilder
{
    public class SelectQuery<T>
    {
        public SelectQuery<T> From() => this;
        public SelectQuery<T> From(string table) => this;
        public SelectQuery<T> Where(string condition) => this;
    }
}

namespace ExternalNamespace
{
    public class OtherLibrary
    {
        public static void From(string table) { }
    }
}

namespace TestNamespace
{
    using EricksonLopez.SqlBuilder;
    using ExternalNamespace;

    public class TestClass
    {
        private const string TableConst = ""Users"";

        public void TestMethod(string dynamicVal)
        {
            var q = new SelectQuery<object>();
            q.From();
            q.From($""dbo.Users"");
            q.From($""dbo.{TableConst}"");
            q.From(""prefix_"" + ""users"");
            q.Where(""x = "" + dynamicVal);
            OtherLibrary.From(""table_"" + dynamicVal);
        }
    }
}";
            await VerifyAnalyzerAsync<DynamicIdentifierAnalyzer>(code);
        }


        [Fact]
        public async Task MissingColumnAnalyzer_NonGenericAndNonQuery_NoDiagnostic()
        {
            var code = @"
namespace EricksonLopez.SqlBuilder
{
    public class RawQuery
    {
        public RawQuery Select(params string[] columns) => this;
    }

    public class OtherService
    {
        public void Select(string col) { }
    }

    public class GenericService<T>
    {
        public void Select(string col) { }
    }
}

namespace TestNamespace
{
    using EricksonLopez.SqlBuilder;

    public class TestClass
    {
        public void TestMethod()
        {
            var q = new RawQuery();
            q.Select(""MissingCol"");

            var o = new OtherService();
            o.Select(""MissingCol"");

            var g = new GenericService<object>();
            g.Select(""MissingCol"");
        }
    }
}";
            await VerifyAnalyzerAsync<MissingColumnAnalyzer>(code);
        }

        [Fact]
        public async Task MissingIndexAnalyzer_ParameterlessOrderBy_NoDiagnostic()
        {
            var code = @"
namespace EricksonLopez.SqlBuilder
{
    public class SelectQuery<T>
    {
        public SelectQuery<T> OrderBy() => this;
    }
}

namespace TestNamespace
{
    using EricksonLopez.SqlBuilder;

    public class TestClass
    {
        public void TestMethod()
        {
            var q = new SelectQuery<object>();
            q.OrderBy();
        }
    }
}";
            await VerifyAnalyzerAsync<MissingIndexAnalyzer>(code);
        }

        [Fact]
        public async Task MissingSourceGeneratorAnalyzer_GenericInterface_ReportsDiagnostic()
        {
            var code = @"
namespace EricksonLopez.SqlBuilder.Abstractions.Metadata
{
    public interface IStaticEntityMetadata<T> { }
}

namespace EricksonLopez.SqlBuilder.Annotations
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class SqlEntityAttribute : System.Attribute { }
}

namespace TestNamespace
{
    using EricksonLopez.SqlBuilder.Annotations;

    [SqlEntity]
    public class {|#0:User|} : System.IEquatable<User>
    {
        public int Id { get; set; }
        public bool Equals(User? other) => other?.Id == Id;
    }
}";
            var expected = CSharpAnalyzerVerifier<MissingSourceGeneratorAnalyzer, DefaultVerifier>.Diagnostic("ESQL021").WithLocation(0).WithArguments("User");
            await VerifyAnalyzerAsync<MissingSourceGeneratorAnalyzer>(code, expected);
        }

        [Fact]
        public async Task QueryPerformanceAnalyzer_NonQueryType_NoDiagnostic()
        {
            var code = @"
namespace EricksonLopez.SqlBuilder
{
    public class SqlBuilderHelper
    {
        public static void Where<T>(Func<T, object> predicate) { }
    }
}

namespace TestNamespace
{
    using EricksonLopez.SqlBuilder;

    public class TestClass
    {
        public void TestMethod()
        {
            SqlBuilderHelper.Where<int>(x => x.ToString());
        }
    }
}";
            await VerifyAnalyzerAsync<QueryPerformanceAnalyzer>(code);
        }

        [Fact]
        public async Task RetryInsideTransactionAnalyzer_NonExecutionAndNonUowCommit_NoDiagnostic()
        {
            var code = @"
namespace Polly
{
    public class ResiliencePipeline
    {
        public void Configure(Action action) { }
        public Task ExecuteAsync(Func<Task> action) => Task.CompletedTask;
    }
}

namespace EricksonLopez.SqlBuilder
{
    public interface IUnitOfWork
    {
        Task CommitAsync();
    }
}

namespace OtherDomain
{
    public class CustomService
    {
        public Task CommitAsync() => Task.CompletedTask;
    }
}

namespace TestNamespace
{
    using Polly;
    using EricksonLopez.SqlBuilder;
    using OtherDomain;

    public class TestClass
    {
        public async Task TestMethod(IUnitOfWork uow)
        {
            var pipeline = new ResiliencePipeline();
            var service = new CustomService();

            pipeline.Configure(() => uow.CommitAsync());
            await pipeline.ExecuteAsync(() => service.CommitAsync());
        }
    }
}";
            await VerifyAnalyzerAsync<RetryInsideTransactionAnalyzer>(code);
        }

        private static Task VerifyAnalyzerAsync<TAnalyzer>(string source, params DiagnosticResult[] expected)
            where TAnalyzer : Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer, new()
        {
            var test = new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
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
