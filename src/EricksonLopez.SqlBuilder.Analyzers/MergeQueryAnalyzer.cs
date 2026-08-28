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
    /// Detects usage of <c>Sql.Merge&lt;T&gt;()</c> or <c>MergeQuery&lt;T&gt;</c> and warns to use
    /// dialect-native <c>OnConflict()</c> or <c>Sql.Raw()</c> instead (ESQL026 / ADR-025).
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MergeQueryAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>The diagnostic identifier for ESQL026.</summary>
        public const string DiagnosticId = "ESQL026";

        private static readonly LocalizableString Title =
            "Generic Sql.Merge<T>() is removed in v2.0";

        private static readonly LocalizableString MessageFormat =
            "Sql.Merge<T>() has been removed in v2.0. Use dialect-native .OnConflict() (PostgreSQL, MySQL, SQLite) or Sql.Raw() (SQL Server, Oracle) instead.";

        private static readonly LocalizableString Description =
            "Generic cross-dialect MERGE statements suffer from major semantic differences and subtle concurrency bugs across providers. " +
            "Use dialect-specific OnConflict APIs for PostgreSQL, MySQL, and SQLite, or Sql.Raw() for SQL Server and Oracle.";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            "Design",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: "https://github.com/ericksonlopezf/dotnet-sql-builder/blob/main/docs/analyzers/ESQL026.md");

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Rule);

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
            var simpleName = invocation.Expression as SimpleNameSyntax;

            string? methodName = memberAccess != null ? memberAccess.Name.Identifier.Text : simpleName?.Identifier.Text;
            if (methodName != "Merge")
            {
                return;
            }

            var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken);
            var methodSymbol = symbolInfo.Symbol as IMethodSymbol ?? symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();

            if (methodSymbol == null)
            {
                return;
            }

            var fullName = methodSymbol.ContainingType?.ToDisplayString();
            if (fullName == "EricksonLopez.SqlBuilder.Sql" || fullName?.StartsWith("EricksonLopez.SqlBuilder.MergeQuery") == true)
            {
                var diagnostic = Diagnostic.Create(Rule, invocation.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}



