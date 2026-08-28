// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EricksonLopez.SqlBuilder.Analyzers
{
    /// <summary>
    /// Detects usages of <c>SqlMapper.AddTypeMap</c> and reminds consumers to verify
    /// that all required Dapper type maps are registered at application startup.
    /// </summary>
    /// <remarks>
    /// Diagnostic ID: <c>ESQL022</c>. Severity: Info.
    /// Missing type map registrations can cause runtime mapping errors when reading
    /// custom value types from query results.
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TypeMapRegistrationAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>Gets the diagnostic identifier emitted by this analyzer.</summary>
        public const string DiagnosticId = "ESQL022";
        private static readonly LocalizableString Title = "Verify Dapper Type Maps registration on startup";
        private static readonly LocalizableString MessageFormat = "Ensure you have registered required Dapper Type Maps for your custom value types";
        private static readonly LocalizableString Description = "Missing Type Maps can lead to runtime mapping errors.";
        private const string Category = "Usage";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Info, isEnabledByDefault: true, description: Description);

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
            var methodSymbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            if (methodSymbol == null) return;
            
            // Dummy logic: Warn if they use SqlMapper.AddTypeMap but missing standard ones
            // As an info diagnostic, this could trigger on startup initialization methods.
            if (methodSymbol.ContainingType.Name == "SqlMapper" && methodSymbol.Name == "AddTypeMap")
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
            }
        }
    }
}




