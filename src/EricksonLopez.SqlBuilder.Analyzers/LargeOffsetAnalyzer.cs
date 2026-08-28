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
    /// Analyzes queries for large OFFSET values which degrade performance.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class LargeOffsetAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>Gets the diagnostic identifier emitted when a query uses a large OFFSET value that may degrade performance.</summary>
        public const string DiagnosticId = "ESQL008";
        private static readonly LocalizableString Title = "Large Offset detected";
        private static readonly LocalizableString MessageFormat = "The Offset {0} is greater than 10,000. Consider using keyset pagination (Seek) for better performance.";
        private static readonly LocalizableString Description = "Avoid using very large OFFSETs, as the database must process and discard rows.";
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
            var methodSymbol = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol as IMethodSymbol;
            if (methodSymbol == null)
            {
                return;
            }

            if (methodSymbol.Name == "Offset" && methodSymbol.ContainingType.Name.Contains("SelectQuery"))
            {
                if (invocation.ArgumentList.Arguments.Count >= 1)
                {
                    var arg = invocation.ArgumentList.Arguments[0].Expression;
                    if (arg is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.NumericLiteralExpression))
                    {
                        if (literal.Token.Value is int offsetValue && offsetValue > 10000)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(Rule, arg.GetLocation(), offsetValue));
                        }
                    }
                }
            }
        }
    }
}



