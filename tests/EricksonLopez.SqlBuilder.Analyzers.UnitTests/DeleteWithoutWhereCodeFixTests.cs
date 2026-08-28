// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;

namespace EricksonLopez.SqlBuilder.Analyzers.Tests
{
    public class DeleteWithoutWhereCodeFixTests
    {
        [Fact]
        public void FixableDiagnosticIds_WhenInstantiated_ShouldContainDeleteAndUpdateIdsAndFixAllProvider()
        {
            var provider = new DeleteWithoutWhereCodeFix();
            provider.FixableDiagnosticIds.Should().Contain(DeleteWithoutWhereAnalyzer.DeleteDiagnosticId);
            provider.FixableDiagnosticIds.Should().Contain(DeleteWithoutWhereAnalyzer.UpdateDiagnosticId);
            provider.GetFixAllProvider().Should().NotBeNull();
        }

        [Fact]
        public async Task RegisterCodeFixesAsync_WhenDeleteWithoutWhereEncountered_ShouldAppendWhereClause()
        {
            var original = @"
namespace EricksonLopez.SqlBuilder.Abstractions
{
    public interface ISqlQuery { }
}
namespace EricksonLopez.SqlBuilder
{
    public class Sql { public static DeleteQuery<T> Delete<T>() => new DeleteQuery<T>(); }
    public class DeleteQuery<T> : EricksonLopez.SqlBuilder.Abstractions.ISqlQuery {
        public object Build() => this;
        public DeleteQuery<T> Where(System.Func<object, bool> predicate) => this;
    }
    public class User {}
    public class TestClass
    {
        public void TestMethod()
        {
            {|ESQL001:Sql.Delete<User>().Build()|};
        }
    }
}";

            var fixedCode = @"
namespace EricksonLopez.SqlBuilder.Abstractions
{
    public interface ISqlQuery { }
}
namespace EricksonLopez.SqlBuilder
{
    public class Sql { public static DeleteQuery<T> Delete<T>() => new DeleteQuery<T>(); }
    public class DeleteQuery<T> : EricksonLopez.SqlBuilder.Abstractions.ISqlQuery {
        public object Build() => this;
        public DeleteQuery<T> Where(System.Func<object, bool> predicate) => this;
    }
    public class User {}
    public class TestClass
    {
        public void TestMethod()
        {
            Sql.Delete<User>().Where(x => true).Build();
        }
    }
}";

            var test = new CSharpCodeFixTest<DeleteWithoutWhereAnalyzer, DeleteWithoutWhereCodeFix, DefaultVerifier>
            {
                TestCode = original,
                FixedCode = fixedCode,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                CompilerDiagnostics = CompilerDiagnostics.None,
            };
            test.TestState.Sources.Add("global using System;\nglobal using System.Collections.Generic;\nglobal using System.Linq;\nglobal using System.Linq.Expressions;\nglobal using System.Threading;\nglobal using System.Threading.Tasks;\n");
            test.FixedState.Sources.Add("global using System;\nglobal using System.Collections.Generic;\nglobal using System.Linq;\nglobal using System.Linq.Expressions;\nglobal using System.Threading;\nglobal using System.Threading.Tasks;\n");
            
            
            
            
            await test.RunAsync();
        }

        [Fact]
        public async Task RegisterCodeFixesAsync_WhenUpdateWithoutWhereEncountered_ShouldAppendWhereClause()
        {
            var original = @"
namespace EricksonLopez.SqlBuilder.Abstractions
{
    public interface ISqlQuery { }
}
namespace EricksonLopez.SqlBuilder
{
    public class Sql { public static UpdateQuery<T> Update<T>() => new UpdateQuery<T>(); }
    public class UpdateQuery<T> : EricksonLopez.SqlBuilder.Abstractions.ISqlQuery {
        public object Build() => this;
        public UpdateQuery<T> Set(System.Action<object> s) => this;
        public UpdateQuery<T> Where(System.Func<object, bool> predicate) => this;
    }
    public class User {}
    public class TestClass
    {
        public void TestMethod()
        {
            {|ESQL003:Sql.Update<User>().Set(u => {}).Build()|};
        }
    }
}";

            var fixedCode = @"
namespace EricksonLopez.SqlBuilder.Abstractions
{
    public interface ISqlQuery { }
}
namespace EricksonLopez.SqlBuilder
{
    public class Sql { public static UpdateQuery<T> Update<T>() => new UpdateQuery<T>(); }
    public class UpdateQuery<T> : EricksonLopez.SqlBuilder.Abstractions.ISqlQuery {
        public object Build() => this;
        public UpdateQuery<T> Set(System.Action<object> s) => this;
        public UpdateQuery<T> Where(System.Func<object, bool> predicate) => this;
    }
    public class User {}
    public class TestClass
    {
        public void TestMethod()
        {
            Sql.Update<User>().Set(u => {}).Where(x => true).Build();
        }
    }
}";

            var test = new CSharpCodeFixTest<DeleteWithoutWhereAnalyzer, DeleteWithoutWhereCodeFix, DefaultVerifier>
            {
                TestCode = original,
                FixedCode = fixedCode,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                CompilerDiagnostics = CompilerDiagnostics.None,
            };
            test.TestState.Sources.Add("global using System;\nglobal using System.Collections.Generic;\nglobal using System.Linq;\nglobal using System.Linq.Expressions;\nglobal using System.Threading;\nglobal using System.Threading.Tasks;\n");
            test.FixedState.Sources.Add("global using System;\nglobal using System.Collections.Generic;\nglobal using System.Linq;\nglobal using System.Linq.Expressions;\nglobal using System.Threading;\nglobal using System.Threading.Tasks;\n");
            
            
            
            
            await test.RunAsync();
        }

        [Fact]
        public async Task RegisterCodeFixesAsync_WhenAsyncExecutionWithoutWhereEncountered_ShouldAppendWhereClause()
        {
            var original = @"
namespace EricksonLopez.SqlBuilder.Abstractions
{
    public interface ISqlQuery { }
}
namespace EricksonLopez.SqlBuilder
{
    public class DbConnection { public Task ExecuteAsync(object query) => Task.CompletedTask; }
    public class Sql { public static DeleteQuery<T> Delete<T>() => new DeleteQuery<T>(); }
    public class DeleteQuery<T> : EricksonLopez.SqlBuilder.Abstractions.ISqlQuery {
        public DeleteQuery<T> Where(System.Func<object, bool> predicate) => this;
    }
    public class User {}
    public class TestClass
    {
        public async Task TestMethod()
        {
            var conn = new DbConnection();
            var q = Sql.Delete<User>();
            await {|ESQL001:conn.ExecuteAsync(q)|};
        }
    }
}";

            var fixedCode = @"
namespace EricksonLopez.SqlBuilder.Abstractions
{
    public interface ISqlQuery { }
}
namespace EricksonLopez.SqlBuilder
{
    public class DbConnection { public Task ExecuteAsync(object query) => Task.CompletedTask; }
    public class Sql { public static DeleteQuery<T> Delete<T>() => new DeleteQuery<T>(); }
    public class DeleteQuery<T> : EricksonLopez.SqlBuilder.Abstractions.ISqlQuery {
        public DeleteQuery<T> Where(System.Func<object, bool> predicate) => this;
    }
    public class User {}
    public class TestClass
    {
        public async Task TestMethod()
        {
            var conn = new DbConnection();
            var q = Sql.Delete<User>();
            await conn.ExecuteAsync(q.Where(x => true));
        }
    }
}";

            var test = new CSharpCodeFixTest<DeleteWithoutWhereAnalyzer, DeleteWithoutWhereCodeFix, DefaultVerifier>
            {
                TestCode = original,
                FixedCode = fixedCode,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                CompilerDiagnostics = CompilerDiagnostics.None,
            };
            test.TestState.Sources.Add("global using System;\nglobal using System.Collections.Generic;\nglobal using System.Linq;\nglobal using System.Linq.Expressions;\nglobal using System.Threading;\nglobal using System.Threading.Tasks;\n");
            test.FixedState.Sources.Add("global using System;\nglobal using System.Collections.Generic;\nglobal using System.Linq;\nglobal using System.Linq.Expressions;\nglobal using System.Threading;\nglobal using System.Threading.Tasks;\n");
            
            
            
            
            await test.RunAsync();
        }

        [Fact]
        public async Task RegisterCodeFixesAsync_WhenElseBranchInvoked_ShouldAppendWhereDirectlyAndSetTitle()
        {
            var code = @"
public class TestClass
{
    public void TestMethod()
    {
        DoSomething();
    }
    public void DoSomething() {}
}";
            var workspace = new AdhocWorkspace();
            var project = workspace.AddProject("TestProj", LanguageNames.CSharp)
                .AddMetadataReference(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
            var document = project.AddDocument("Test.cs", code);
            var root = (await document.GetSyntaxRootAsync())!;
            var invocation = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax>().First();
            var diag = Diagnostic.Create(DeleteWithoutWhereAnalyzer.DeleteDiagnosticId, "Usage", "msg", DiagnosticSeverity.Error, DiagnosticSeverity.Error, true, 0, location: invocation.GetLocation());

            var actions = new List<Microsoft.CodeAnalysis.CodeActions.CodeAction>();
            var context = new Microsoft.CodeAnalysis.CodeFixes.CodeFixContext(document, diag, (a, d) => actions.Add(a), CancellationToken.None);
            var provider = new DeleteWithoutWhereCodeFix();
            await provider.RegisterCodeFixesAsync(context);

            actions.Should().HaveCount(1);
            actions[0].Title.Should().Be("Chain security .Where(...) filter");
            var operations = await actions[0].GetOperationsAsync(CancellationToken.None);
            var applyDocOp = operations.OfType<Microsoft.CodeAnalysis.CodeActions.ApplyChangesOperation>().First();
            var newDoc = applyDocOp.ChangedSolution.GetDocument(document.Id)!;
            var newText = (await newDoc.GetTextAsync()).ToString();
            newText.Should().Contain("DoSomething().Where(x => true)");
        }
    }
}





