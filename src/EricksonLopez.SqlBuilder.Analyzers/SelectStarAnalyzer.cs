// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EricksonLopez.SqlBuilder.Analyzers;

/// <summary>
/// Analyzes queries for explicit use of '*' which is not recommended.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SelectStarAnalyzer : DiagnosticAnalyzer
{
    /// <summary>Gets the diagnostic identifier emitted when a SELECT * projection is used without an explicit column list.</summary>
    public const string DiagnosticId = "SQL0003";

    private static readonly LocalizableString Title = "Avoid explicit SELECT *";
    private static readonly LocalizableString MessageFormat = "The use of '*' in RawSelect or Select(\"*\") is not recommended for performance and maintainability reasons";
    private static readonly LocalizableString Description = "Explicitly specify the desired columns instead of using '*'.";
    private const string Category = "Performance";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

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
            if (methodName == "RawSelect" || methodName == "Select")
            {
                if (invocation.ArgumentList.Arguments.Count > 0)
                {
                    var arg = invocation.ArgumentList.Arguments[0].Expression;
                    if (arg is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression))
                    {
                        var value = literal.Token.ValueText;
                        if (value.Contains("*"))
                        {
                            var diagnostic = Diagnostic.Create(Rule, literal.GetLocation());
                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                }
            }
        }
    }
}



