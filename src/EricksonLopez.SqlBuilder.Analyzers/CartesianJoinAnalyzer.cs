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
    /// Detects join clauses that are missing an ON condition, which would produce a Cartesian product.
    /// </summary>
    /// <remarks>
    /// Emits diagnostic <c>ESQL024</c> when a <c>RawJoin</c> call or a strongly-typed join method
    /// receives an empty or whitespace ON clause. CROSS JOIN patterns are intentionally excluded.
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CartesianJoinAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>Gets the diagnostic identifier emitted by this analyzer.</summary>
        public const string DiagnosticId = "ESQL024";

        private static readonly LocalizableString Title = "Potential Cartesian Join (Missing ON Condition)";
        private static readonly LocalizableString MessageFormat = "The Join clause '{0}' appears to be missing an ON condition, which could lead to a Cartesian Join";
        private static readonly LocalizableString Description = "Ensure that JOIN clauses include an ON condition to avoid Cartesian products.";
        private const string Category = "Correctness";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.InvocationExpression);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                var methodName = memberAccess.Name.Identifier.Text;
                
                if (methodName == "RawJoin")
                {
                    if (invocation.ArgumentList.Arguments.Count > 0)
                    {
                        var arg = invocation.ArgumentList.Arguments[0].Expression;
                        if (arg is InterpolatedStringExpressionSyntax interpolated)
                        {
                            var text = string.Concat(interpolated.Contents.OfType<InterpolatedStringTextSyntax>().Select(t => t.TextToken.ValueText));
                            CheckRawJoinCondition(context, invocation, text);
                        }
                        else if (arg is LiteralExpressionSyntax literal)
                        {
                            CheckRawJoinCondition(context, invocation, literal.Token.ValueText);
                        }
                    }
                }
                else if (methodName == "Join" || methodName == "LeftJoin" || methodName == "RightJoin" || methodName == "InnerJoin" || methodName == "FullJoin")
                {
                    var methodSymbol = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol as IMethodSymbol;
                    if (methodSymbol == null || methodSymbol.ContainingType.Name != "SelectQuery") return;

                    var parameters = methodSymbol.Parameters;
                    int count = Math.Min(parameters.Length, invocation.ArgumentList.Arguments.Count);
                    for (int i = 0; i < count; i++)
                    {
                        if (parameters[i].Name == "on")
                        {
                            var arg = invocation.ArgumentList.Arguments[i].Expression;
                            if (arg is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression))
                            {
                                var text = literal.Token.ValueText;
                                if (string.IsNullOrWhiteSpace(text))
                                {
                                    var diagnostic = Diagnostic.Create(Rule, arg.GetLocation(), methodName);
                                    context.ReportDiagnostic(diagnostic);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void CheckRawJoinCondition(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation, string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            
            var upper = text.ToUpperInvariant();
            
            if (upper.Contains("CROSS JOIN")) return;
            
            if (!upper.Contains(" ON ") && !upper.EndsWith(" ON"))
            {
                var diagnostic = Diagnostic.Create(Rule, invocation.GetLocation(), "RawJoin");
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}



