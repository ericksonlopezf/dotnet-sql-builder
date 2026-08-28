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
    /// Detects usage of the unsafe <c>Sql.Raw(string sql, ...)</c> overload and warns to use
    /// <c>Sql.Raw(FormattableString)</c> instead to prevent SQL injection vulnerabilities (ESQL011).
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RawStringOverloadAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>The diagnostic identifier for ESQL011.</summary>
        public const string DiagnosticId = "ESQL011";

        private static readonly LocalizableString Title =
            "Unsafe Sql.Raw(string) overload";

        private static readonly LocalizableString MessageFormat =
            "Use Sql.Raw(FormattableString) instead of Sql.Raw(string) to prevent SQL injection. " +
            "The string overload does not parameterize interpolated values.";

        private static readonly LocalizableString Description =
            "The Sql.Raw(string, object?) overload is marked deprecated and unsafe. " +
            "Replace with Sql.Raw($\"...\") (FormattableString) so that all interpolated " +
            "values become named parameters automatically.";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            "Security",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: "https://github.com/ericksonlopezf/dotnet-sql-builder/blob/main/docs/analyzers/esql011.md");

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

            // Check that the method being called is named "Raw"
            var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
            var identifierName = invocation.Expression as IdentifierNameSyntax;

            string? methodName = memberAccess != null ? memberAccess.Name.Identifier.Text : identifierName?.Identifier.Text;
            if (methodName != "Raw")
            {
                return;
            }

            // Resolve the method symbol to confirm it's EricksonLopez.SqlBuilder.Sql.Raw
            var symbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            if (symbol == null)
            {
                return;
            }

            // Only flag if the containing type is EricksonLopez.SqlBuilder.Sql
            var containingType = symbol.ContainingType?.ToDisplayString();
            if (containingType != "EricksonLopez.SqlBuilder.Sql")
            {
                return;
            }

            // Check if the first parameter type is string (not FormattableString)
            if (symbol.Parameters.Length == 0)
            {
                return;
            }

            var firstParam = symbol.Parameters[0];
            var firstParamType = firstParam.Type.ToDisplayString();

            // FormattableString overload is safe; string overload is not
            if (firstParamType == "string")
            {
                var location = invocation.GetLocation();
                context.ReportDiagnostic(Diagnostic.Create(Rule, location));
            }
        }
    }
}




