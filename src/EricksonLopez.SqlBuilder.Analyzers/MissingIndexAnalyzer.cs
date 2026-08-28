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
    /// Analyzes WHERE clauses to warn if filtering by an unindexed column.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MissingIndexAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>Gets the diagnostic identifier emitted when a WHERE clause filters on a column that lacks a database index.</summary>
        public const string DiagnosticId = "ESQL007";
        private static readonly LocalizableString Title = "OrderBy on unindexed column";
        private static readonly LocalizableString MessageFormat = "The property '{0}' does not have the [Indexed] attribute. Sorting by this column can cause a full table scan and affect performance.";
        private static readonly LocalizableString Description = "Avoid sorting by columns that are not indexed in the database.";
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

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                var methodName = memberAccess.Name.Identifier.Text;
                if (methodName == "OrderBy" || methodName == "OrderByDescending" || methodName == "ThenBy" || methodName == "ThenByDescending")
                {
                    if (invocation.ArgumentList.Arguments.Count > 0)
                    {
                        var arg = invocation.ArgumentList.Arguments[0].Expression;
                        
                        // Look for the lambda expression: x => x.Property
                        if (arg is SimpleLambdaExpressionSyntax lambda && lambda.Body is MemberAccessExpressionSyntax lambdaBody)
                        {
                            var symbolInfo = context.SemanticModel.GetSymbolInfo(lambdaBody, context.CancellationToken);
                            if (symbolInfo.Symbol is IPropertySymbol propertySymbol)
                            {
                                bool isIndexed = propertySymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "IndexedAttribute");
                                
                                if (!isIndexed)
                                {
                                    var diagnostic = Diagnostic.Create(Rule, arg.GetLocation(), propertySymbol.Name);
                                    context.ReportDiagnostic(diagnostic);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}




