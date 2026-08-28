// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EricksonLopez.SqlBuilder.Analyzers
{
    /// <summary>
    /// Analyzes invocations of Dapper extensions to ensure a compiler is registered.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DapperCompilerAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>Gets the diagnostic identifier emitted when a Dapper execution call uses a compiler that has not been registered.</summary>
        public const string DiagnosticId = "ESQL005";
        private static readonly LocalizableString Title = "Call to Dapper extensions without compiler";
        private static readonly LocalizableString MessageFormat = "Ensure you have registered the compiler with DapperExtensions.RegisterCompiler";
        private static readonly LocalizableString Description = "To use SqlBuilder Dapper extensions you must register your DB compiler.";
        private const string Category = "Usage";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Info, isEnabledByDefault: true, description: Description);

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            var methodSymbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            if (methodSymbol == null)
            {
                return;
            }

            if (methodSymbol.ContainingType.Name == "DapperExtensions" && methodSymbol.Name.Contains("Query"))
            {
                // We could inspect compilation to verify if RegisterCompiler is called, but it is global.
                // Since this is Info, only report a diagnostic.
                context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
            }
        }
    }
}



