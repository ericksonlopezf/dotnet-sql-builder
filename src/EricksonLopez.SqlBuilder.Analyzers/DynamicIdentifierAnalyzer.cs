// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EricksonLopez.SqlBuilder.Analyzers
{
    /// <summary>
    /// Detects usage of dynamic SQL identifiers (table names, column names) constructed via
    /// string concatenation or interpolation without an explicit allowlist, which can
    /// lead to SQL injection vulnerabilities (ELSB004).
    /// </summary>
    /// <remarks>
    /// Safe alternatives:
    /// <list type="bullet">
    ///   <item>Use strongly-typed entity models: <c>Sql.From&lt;T&gt;()</c></item>
    ///   <item>Validate against an explicit allowlist before passing to <c>RawJoin</c>, <c>OrderBy(FormattableString)</c>, etc.</item>
    /// </list>
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DynamicIdentifierAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>The diagnostic identifier for ELSB004.</summary>
        public const string DiagnosticId = "ELSB004";

        private static readonly LocalizableString Title =
            "Dynamic SQL identifier without allowlist";

        private static readonly LocalizableString MessageFormat =
            "The SQL identifier '{0}' is built dynamically without an allowlist check. " +
            "Validate against a known-safe list before passing to SQL APIs to prevent injection.";

        private static readonly LocalizableString Description =
            "Passing dynamically-built table names, column names, or schema names into SQL APIs " +
            "without an explicit allowlist creates SQL injection risk. Use strongly-typed entity " +
            "models (Sql.From<T>()) or validate against a compile-time or runtime allowlist.";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            "Security",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: "https://github.com/ericksonlopezf/dotnet-sql-builder/blob/main/docs/analyzers/ELSB004.md");

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

            // Target methods that accept raw identifier strings
            var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
            if (memberAccess == null) return;

            var methodName = memberAccess.Name.Identifier.Text;

            // The methods that accept raw table/column identifiers
            if (methodName != "InnerJoin" &&
                methodName != "LeftJoin" &&
                methodName != "RightJoin" &&
                methodName != "FullJoin" &&
                methodName != "CrossJoin" &&
                methodName != "RawJoin" &&
                methodName != "GroupBy" &&
                methodName != "From")
            {
                return;
            }

            var symbol = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol as IMethodSymbol;
            var containingNs = symbol?.ContainingType?.ContainingNamespace?.ToDisplayString();
            if (containingNs == null || !containingNs.StartsWith("EricksonLopez.SqlBuilder", StringComparison.Ordinal)) return;

            // Inspect the first string argument — flag if it's built via concatenation or non-constant interpolation
            if (invocation.ArgumentList.Arguments.Count == 0) return;

            var firstArg = invocation.ArgumentList.Arguments[0].Expression;
            if (IsDynamicIdentifier(firstArg, context))
            {
                var location = firstArg.GetLocation();
                context.ReportDiagnostic(Diagnostic.Create(Rule, location, firstArg.ToString()));
            }
        }

        private static bool IsDynamicIdentifier(ExpressionSyntax expression, SyntaxNodeAnalysisContext context)
        {
            // Binary expression with string concatenation: "table_" + userInput
            if (expression is BinaryExpressionSyntax binary &&
                binary.IsKind(SyntaxKind.AddExpression))
            {
                // Check if either operand is not a compile-time constant
                var leftConst = context.SemanticModel.GetConstantValue(binary.Left, context.CancellationToken);
                var rightConst = context.SemanticModel.GetConstantValue(binary.Right, context.CancellationToken);

                // If at least one side is not a constant, it's dynamic
                return !leftConst.HasValue || !rightConst.HasValue;
            }

            // Interpolated string with non-constant holes: $"table_{userTableName}"
            if (expression is InterpolatedStringExpressionSyntax interpolated)
            {
                foreach (var content in interpolated.Contents)
                {
                    if (content is InterpolationSyntax interpolation)
                    {
                        var constValue = context.SemanticModel.GetConstantValue(interpolation.Expression, context.CancellationToken);
                        if (!constValue.HasValue)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}




