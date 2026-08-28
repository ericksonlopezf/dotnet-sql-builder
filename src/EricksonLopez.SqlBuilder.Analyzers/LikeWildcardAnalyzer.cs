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
    /// Analyzes LIKE queries for missing wildcard characters.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class LikeWildcardAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>Gets the diagnostic identifier emitted when LIKE is used without any wildcard character.</summary>
        public const string DiagnosticIdNoWildcard = "ESQL009";
        /// <summary>Gets the diagnostic identifier emitted when LIKE is used with a leading wildcard that prevents index use.</summary>
        public const string DiagnosticIdLeadingWildcard = "ESQL010";

        private static readonly LocalizableString TitleNoWildcard = "Use of LIKE without wildcards";
        private static readonly LocalizableString MessageFormatNoWildcard = "The string '{0}' does not contain wildcards ('%' or '_'). Use '=' for exact searches.";
        private static readonly LocalizableString DescriptionNoWildcard = "Avoid using LIKE if you are not searching for patterns with wildcards.";

        private static readonly LocalizableString TitleLeadingWildcard = "Use of LIKE with leading wildcard";
        private static readonly LocalizableString MessageFormatLeadingWildcard = "The string '{0}' starts with a '%' wildcard. This prevents the use of B-Tree indexes and causes full table scans.";
        private static readonly LocalizableString DescriptionLeadingWildcard = "Consider using Full-Text Search or trigram indexes if you require suffix or leading wildcard searches.";

        private const string Category = "Performance";

        private static readonly DiagnosticDescriptor RuleNoWildcard = new DiagnosticDescriptor(
            DiagnosticIdNoWildcard, TitleNoWildcard, MessageFormatNoWildcard, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: DescriptionNoWildcard);

        private static readonly DiagnosticDescriptor RuleLeadingWildcard = new DiagnosticDescriptor(
            DiagnosticIdLeadingWildcard, TitleLeadingWildcard, MessageFormatLeadingWildcard, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: DescriptionLeadingWildcard);

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(RuleNoWildcard, RuleLeadingWildcard);

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

            if (methodSymbol.Name == "WhereILike" || methodSymbol.Name == "WhereLike")
            {
                if (invocation.ArgumentList.Arguments.Count >= 2)
                {
                    var arg = invocation.ArgumentList.Arguments[1].Expression;
                    if (arg is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression))
                    {
                        var text = literal.Token.ValueText;
                        if (!text.Contains("%") && !text.Contains("_"))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(RuleNoWildcard, arg.GetLocation(), text));
                        }
                        else if (text.StartsWith("%", System.StringComparison.Ordinal))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(RuleLeadingWildcard, arg.GetLocation(), text));
                        }
                    }
                }
            }
        }
    }
}



