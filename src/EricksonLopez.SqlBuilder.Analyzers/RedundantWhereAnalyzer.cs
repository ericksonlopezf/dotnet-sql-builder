// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EricksonLopez.SqlBuilder.Analyzers;

/// <summary>
/// Analyzes WHERE clauses to detect redundant conditions (e.g. 1 = 1).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RedundantWhereAnalyzer : DiagnosticAnalyzer
{
    /// <summary>Gets the diagnostic identifier emitted when a WHERE clause condition is always true or always false.</summary>
    public const string DiagnosticId = "SQL0004";

    private static readonly LocalizableString Title = "Redundant Where clause";
    private static readonly LocalizableString MessageFormat = "The condition '{0}' in the Where clause appears to be tautological or redundant";
    private static readonly LocalizableString Description = "Avoid statically defined WHERE clauses like '1=1'.";
    private const string Category = "Maintainability";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Info, isEnabledByDefault: true, description: Description);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

    /// <inheritdoc />
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
            if (methodName == "Where" || methodName == "And" || methodName == "Or")
            {
                if (invocation.ArgumentList.Arguments.Count > 0)
                {
                    var arg = invocation.ArgumentList.Arguments[0].Expression;
                    if (arg is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression))
                    {
                        var value = literal.Token.ValueText.Replace(" ", "");
                        if (value == "1=1" || value == "true=true" || value == "0=0")
                        {
                            var diagnostic = Diagnostic.Create(Rule, literal.GetLocation(), literal.Token.ValueText);
                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                }
            }
        }
    }
}



