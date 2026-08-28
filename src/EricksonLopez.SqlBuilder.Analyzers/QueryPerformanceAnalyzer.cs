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
    /// Analyzes IN clauses for excessive argument counts.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class QueryPerformanceAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>Gets the diagnostic identifier emitted when a query exhibits a known performance anti-pattern.</summary>
        public const string DiagnosticId = "ESQL004";
        private static readonly LocalizableString Title = "Use of ToString() or similar in SQL Expressions";
        private static readonly LocalizableString MessageFormat = "The method {0} cannot be translated to SQL natively or affects performance";
        private static readonly LocalizableString Description = "Avoid using ToString(), ToUpper(), ToLower() inside Where or Having lambda expressions.";
        private const string Category = "Performance";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

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
            
            // Check if we are inside a SqlBuilder lambda
            var lambda = invocation.Ancestors().OfType<LambdaExpressionSyntax>().FirstOrDefault();
            if (lambda == null)
            {
                return;
            }

            var methodSymbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            if (methodSymbol == null)
            {
                return;
            }

            var name = methodSymbol.Name;
            if (name == "ToString" || name == "ToUpper" || name == "ToLower")
            {
                // Verify if the lambda belongs to a SqlBuilder method (Where, Having, etc.)
                var parentInvocation = lambda.Ancestors().OfType<InvocationExpressionSyntax>().FirstOrDefault();
                if (parentInvocation != null)
                {
                    var parentMethod = context.SemanticModel.GetSymbolInfo(parentInvocation).Symbol as IMethodSymbol;
                    if (parentMethod != null && parentMethod.ContainingType.ContainingNamespace?.ToDisplayString().StartsWith("EricksonLopez.SqlBuilder") == true && parentMethod.ContainingType.Name.Contains("Query"))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), name));
                    }
                }
            }
        }
    }
}



