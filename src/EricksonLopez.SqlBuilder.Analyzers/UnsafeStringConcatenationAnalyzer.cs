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
    /// Analyzes Raw SQL queries for unsafe string concatenation that could lead to SQL injection.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UnsafeStringConcatenationAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>Gets the diagnostic identifier emitted when raw string concatenation is used to build a SQL fragment, risking injection.</summary>
        public const string DiagnosticId = "ESQL002";
        
        private static readonly LocalizableString Title = "Unsafe string concatenation in SQL";
        private static readonly LocalizableString MessageFormat = "Use of '+' concatenation instead of safe '$' interpolation in SQL method";
        private static readonly LocalizableString Description = "Detects the use of string concatenation in Raw methods which could lead to SQL Injection.";
        private const string Category = "Security";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocationExpr = (InvocationExpressionSyntax)context.Node;
            var methodSymbol = context.SemanticModel.GetSymbolInfo(invocationExpr, context.CancellationToken).Symbol as IMethodSymbol;
            if (methodSymbol == null)
            {
                return;
            }

            // Check if method is RawWhere, RawSelect or Raw
            if (methodSymbol.Name != "RawWhere" && methodSymbol.Name != "RawSelect" && methodSymbol.Name != "Raw")
            {
                return;
            }

            var argument = invocationExpr.ArgumentList.Arguments.FirstOrDefault();
            if (argument == null)
            {
                return;
            }

            // If the argument is a string concatenation
            if (argument.Expression.IsKind(SyntaxKind.AddExpression))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, argument.Expression.GetLocation()));
            }
        }
    }
}



