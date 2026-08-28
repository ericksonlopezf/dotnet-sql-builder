// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EricksonLopez.SqlBuilder.Analyzers
{
    /// <summary>
    /// Detects synchronous database execution methods called in contexts where
    /// an asynchronous alternative is available, helping prevent UI thread blocking.
    /// </summary>
    /// <remarks>
    /// Diagnostic ID: <c>ESQL023</c>. Severity: Warning.
    /// Emits a warning when <c>ToResult</c>, <c>ToPagedList</c>, or <c>ToStream</c>
    /// are invoked synchronously. Use the corresponding <c>*Async</c> overloads instead.
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SyncOnUiThreadAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>Gets the diagnostic identifier emitted by this analyzer.</summary>
        public const string DiagnosticId = "ESQL023";
        private static readonly LocalizableString Title = "Synchronous execution on UI thread";
        private static readonly LocalizableString MessageFormat = "Avoid synchronous Dapper execution (like ToResult) on UI threads. Use ToResultAsync instead.";
        private static readonly LocalizableString Description = "Synchronous database queries can block the UI thread and degrade application responsiveness.";
        private const string Category = "Performance";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            var methodSymbol = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol as IMethodSymbol;
            if (methodSymbol == null) return;
            
            if (methodSymbol.ContainingType.Name == "ConnectionSqlExtensions" && 
                (methodSymbol.Name == "ToResult" || methodSymbol.Name == "ToPagedList" || methodSymbol.Name == "ToStream"))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
            }
        }
    }
}




