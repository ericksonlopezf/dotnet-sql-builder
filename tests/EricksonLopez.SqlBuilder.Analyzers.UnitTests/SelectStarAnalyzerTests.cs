// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
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
    public class SelectStarAnalyzerTests
    {
        [Fact]
        public void SelectStarCodeFix_MetadataAndFixAll_AreValid()
        {
            var provider = new SelectStarCodeFix();
            Assert.Contains(SelectStarAnalyzer.DiagnosticId, provider.FixableDiagnosticIds);
            Assert.NotNull(provider.GetFixAllProvider());
        }

        [Fact]
        public void SelectStarAnalyzer_SupportedDiagnostics_AreValid()
        {
            var analyzer = new SelectStarAnalyzer();
            Assert.Single(analyzer.SupportedDiagnostics);
            var rule = analyzer.SupportedDiagnostics[0];
            Assert.Equal("SQL0003", rule.Id);
            Assert.Equal("Avoid explicit SELECT *", rule.Title.ToString());
            Assert.Equal("The use of '*' in RawSelect or Select(\"*\") is not recommended for performance and maintainability reasons", rule.MessageFormat.ToString());
            Assert.Equal("Explicitly specify the desired columns instead of using '*'.", rule.Description.ToString());
            Assert.True(rule.IsEnabledByDefault);
        }

        [Fact]
        public async Task SelectStarCodeFix_RegisterCodeFixesAsync_DirectTest()
        {
            var provider = new SelectStarCodeFix();
            var project = new AdhocWorkspace().CurrentSolution
                .AddProject("TestProj", "TestProj", LanguageNames.CSharp)
                .AddMetadataReference(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
            var document = project.AddDocument("Test.cs", @"
namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            Select(""*"");
        }
        public void Select(string s) { }
    }
}");
            var tree = await document.GetSyntaxTreeAsync();
            var literal = tree!.GetRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().First();
            var diagnostic = Diagnostic.Create(new SelectStarAnalyzer().SupportedDiagnostics[0], literal.GetLocation());
            
            var actions = new List<CodeAction>();
            var context = new CodeFixContext(document, diagnostic, (action, diag) => actions.Add(action), CancellationToken.None);
            await provider.RegisterCodeFixesAsync(context);

            Assert.Single(actions);
            Assert.Equal("Replace '*' with specific columns", actions[0].Title);
            Assert.Equal("SelectStarLiteralFix", actions[0].EquivalenceKey);

            // Test when document has no literal
            var doc2 = project.AddDocument("Empty.cs", "namespace TestNamespace { class Empty { } }");
            var tree2 = await doc2.GetSyntaxTreeAsync();
            var emptyNode = tree2!.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().First();
            var diagOnClass = Diagnostic.Create(new SelectStarAnalyzer().SupportedDiagnostics[0], emptyNode.GetLocation());
            var actionsEmpty = new List<CodeAction>();
            var context2 = new CodeFixContext(doc2, diagOnClass, (action, diag) => actionsEmpty.Add(action), CancellationToken.None);
            await provider.RegisterCodeFixesAsync(context2);
            Assert.Empty(actionsEmpty);
        }

        [Fact]
        public async Task Select_WithStar_ReportsDiagnostic_AndAppliesCodeFix()
        {
            var code = @"
namespace TestNamespace
{
    public class Query
    {
        public Query Select(string columns) => this;
        public Query RawSelect(string columns) => this;
    }

    public class TestClass
    {
        public void TestMethod()
        {
            var q = new Query();
            q.Select({|#0:""*""|});
            q.RawSelect({|#1:""SELECT * FROM Users""|});
        }
    }
}";

            var expected0 = VerifySelectStarCSFix.Diagnostic("SQL0003").WithLocation(0);
            var expected1 = VerifySelectStarCSFix.Diagnostic("SQL0003").WithLocation(1);

            var fixedCode = @"
namespace TestNamespace
{
    public class Query
    {
        public Query Select(string columns) => this;
        public Query RawSelect(string columns) => this;
    }

    public class TestClass
    {
        public void TestMethod()
        {
            var q = new Query();
            q.Select(""id"");
            q.RawSelect(""SELECT id FROM Users"");
        }
    }
}";

            await VerifySelectStarCSFix.VerifyCodeFixAsync(code, new[] { expected0, expected1 }, fixedCode);
        }

        [Fact]
        public async Task Select_WithExplicitColumns_DoesNotReportDiagnostic()
        {
            var code = @"
namespace TestNamespace
{
    public class Query
    {
        public Query Select(string columns) => this;
        public Query RawSelect(string columns) => this;
        public Query OtherMethod() => this;
    }

    public class TestClass
    {
        public void TestMethod()
        {
            var q = new Query();
            q.Select(""Id, Name"");
            q.RawSelect(""SELECT Id, Name FROM Users"");
            q.OtherMethod();
        }
    }
}";
            await VerifySelectStarCSFix.VerifyAnalyzerAsync(code);
        }

        [Fact]
        public async Task Select_WithNonLiteralOrNoArgs_DoesNotReportDiagnostic()
        {
            var code = @"
namespace TestNamespace
{
    public class Query
    {
        public Query Select(string columns) => this;
        public Query Select() => this;
    }

    public class TestClass
    {
        public void TestMethod(string cols)
        {
            var q = new Query();
            q.Select(cols);
            q.Select();
        }
    }
}";
            await VerifySelectStarCSFix.VerifyAnalyzerAsync(code);
        }
    }

    public static class VerifySelectStarCSFix
    {
        public static DiagnosticResult Diagnostic(string diagnosticId)
        {
            return CSharpCodeFixVerifier<SelectStarAnalyzer, SelectStarCodeFix, DefaultVerifier>.Diagnostic(diagnosticId);
        }

        public static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
        {
            var test = new CSharpAnalyzerTest<SelectStarAnalyzer, DefaultVerifier>
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
            var test = new CSharpCodeFixTest<SelectStarAnalyzer, SelectStarCodeFix, DefaultVerifier>
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





