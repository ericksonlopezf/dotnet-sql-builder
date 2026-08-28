// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EricksonLopez.SqlBuilder.Analyzers;

/// <summary>
/// Analyzes queries for missing columns in strongly-typed entities.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MissingColumnAnalyzer : DiagnosticAnalyzer
{
    /// <summary>Gets the diagnostic identifier emitted when a SELECT references a column that does not exist on the entity.</summary>
    public const string DiagnosticId = "SQL0009";

    private static readonly LocalizableString Title = "Non-existent column in entity";
    private static readonly LocalizableString MessageFormat = "The column '{0}' does not exist as a property in entity '{1}'";
    private static readonly LocalizableString Description = "Columns specified in Select, OrderBy or GroupBy must correspond to properties of the mapped entity.";
    private const string Category = "Usage";

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
            if (methodName == "Select" || methodName == "OrderBy" || methodName == "OrderByDescending" || methodName == "GroupBy")
            {
                var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
                if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
                {
                    // Find the type parameter T of SelectQuery<T>
                    var containingType = methodSymbol.ContainingType;
                    if (containingType == null || !containingType.Name.Contains("Query"))
                    {
                        return;
                    }

                    var typeArgument = containingType.TypeArguments.FirstOrDefault();
                    if (typeArgument != null && typeArgument.TypeKind != TypeKind.TypeParameter)
                    {
                        var properties = typeArgument.GetMembers().OfType<IPropertySymbol>();
                        var validNames = properties.Select(p => p.Name).ToList();
                        // Also add snake_case versions
                        var snakeNames = validNames.Select(ToSnakeCase).ToList();
                        validNames.AddRange(snakeNames);

                        foreach (var arg in invocation.ArgumentList.Arguments)
                        {
                            if (arg.Expression is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression))
                            {
                                var colName = literal.Token.ValueText;
                                if (colName == "*")
                                {
                                    continue;
                                }

                                if (!validNames.Contains(colName, System.StringComparer.OrdinalIgnoreCase))
                                {
                                    var diagnostic = Diagnostic.Create(Rule, literal.GetLocation(), colName, typeArgument.Name);
                                    context.ReportDiagnostic(diagnostic);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private static string ToSnakeCase(string str)
    {
        return string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToLower();
    }
}




