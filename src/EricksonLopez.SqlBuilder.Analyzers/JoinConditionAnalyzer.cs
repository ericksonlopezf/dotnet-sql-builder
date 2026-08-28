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
    /// Analyzes JOIN clauses to ensure conditions reference both tables.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class JoinConditionAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>Gets the diagnostic identifier emitted when a JOIN clause is missing its ON condition.</summary>
        public const string DiagnosticId = "ESQL006";
        private static readonly LocalizableString Title = "Incompatible types in Join";
        private static readonly LocalizableString MessageFormat = "The types of the properties compared in the JOIN do not match ({0} vs {1}). This can cause execution errors or performance issues.";
        private static readonly LocalizableString Description = "Ensure that the columns compared in a JOIN are of the same type.";
        private const string Category = "Correctness";

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
                if (methodName == "Join" || methodName == "LeftJoin" || methodName == "RightJoin" || methodName == "InnerJoin")
                {
                    if (invocation.ArgumentList.Arguments.Count > 0)
                    {
                        var arg = invocation.ArgumentList.Arguments.Last().Expression;
                        
                        // Buscamos: (a, b) => a.Id == b.AuthorId
                        if (arg is ParenthesizedLambdaExpressionSyntax lambda && lambda.Body is BinaryExpressionSyntax binary)
                        {
                            if (binary.IsKind(SyntaxKind.EqualsExpression))
                            {
                                var leftSymbol = context.SemanticModel.GetSymbolInfo(binary.Left, context.CancellationToken).Symbol as IPropertySymbol;
                                var rightSymbol = context.SemanticModel.GetSymbolInfo(binary.Right, context.CancellationToken).Symbol as IPropertySymbol;
                                
                                if (leftSymbol != null && rightSymbol != null)
                                {
                                    var leftType = UnwrapType(leftSymbol.Type);
                                    var rightType = UnwrapType(rightSymbol.Type);
                                    
                                    if (!SymbolEqualityComparer.Default.Equals(leftType, rightType))
                                    {
                                        var diagnostic = Diagnostic.Create(Rule, binary.GetLocation(), leftType.ToDisplayString(), rightType.ToDisplayString());
                                        context.ReportDiagnostic(diagnostic);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static ITypeSymbol UnwrapType(ITypeSymbol type)
        {
            if (type is INamedTypeSymbol namedType && namedType.IsGenericType && namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                return namedType.TypeArguments[0];
            }
            return type.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
        }
    }
}




