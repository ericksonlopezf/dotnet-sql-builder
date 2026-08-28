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
    /// Detects when a Polly resilience pipeline's <c>ExecuteAsync</c> lambda contains a direct
    /// call to <c>CommitAsync</c> on an <c>IUnitOfWork</c>, which is an anti-pattern that can
    /// cause data corruption on retry (ESQL005).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The correct pattern is to wrap the <em>entire</em> transactional unit (BeginUnitOfWork →
    /// Execute → Commit) inside the retry lambda. Wrapping only the commit is dangerous because
    /// a retry can re-execute the commit on a transaction that was already partially committed.
    /// </para>
    /// <para>See ADR-016 for the correct pattern.</para>
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RetryInsideTransactionAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>The diagnostic identifier for ESQL012.</summary>
        public const string DiagnosticId = "ESQL012";

        private static readonly LocalizableString Title =
            "Retry pipeline wraps transaction commit";

        private static readonly LocalizableString MessageFormat =
            "The resilience pipeline ExecuteAsync lambda contains a CommitAsync call. " +
            "This pattern can cause data corruption. Wrap the entire transaction (BeginUnitOfWork → Execute → Commit) inside the retry lambda instead.";

        private static readonly LocalizableString Description =
            "A Polly retry pipeline must never wrap only the commit of a transaction. " +
            "On retry, the transaction would be re-attempted from mid-state, causing " +
            "duplicate inserts or corrupted data. Place the entire transactional unit " +
            "(begin, execute, commit) inside the pipeline.ExecuteAsync lambda. See ADR-016.";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: "https://github.com/ericksonlopezf/dotnet-sql-builder/blob/main/docs/analyzers/esql012.md");

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

            // Detect calls to pipeline.ExecuteAsync(...)
            var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
            if (memberAccess == null)
            {
                return;
            }

            var methodName = memberAccess.Name.Identifier.Text;
            if (methodName != "ExecuteAsync" && methodName != "Execute")
            {
                return;
            }

            // Verify it's a resilience pipeline (ResiliencePipeline or IResiliencePipeline)
            var symbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            if (symbol == null)
            {
                return;
            }

            var containingTypeName = symbol.ContainingType?.ToDisplayString();
            bool isResiliencePipeline = containingTypeName != null && (
                containingTypeName.Contains("ResiliencePipeline") ||
                containingTypeName.Contains("Polly"));

            if (!isResiliencePipeline)
            {
                return;
            }

            // Look for CommitAsync calls inside the lambda arguments
            foreach (var argument in invocation.ArgumentList.Arguments)
            {
                var lambda = argument.Expression as LambdaExpressionSyntax;
                if (lambda == null)
                {
                    continue;
                }

                var commitCalls = lambda.DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
                    .Where(inv =>
                    {
                        var ma = inv.Expression as MemberAccessExpressionSyntax;
                        return ma?.Name.Identifier.Text == "CommitAsync"
                            || ma?.Name.Identifier.Text == "Commit";
                    });

                foreach (var commitCall in commitCalls)
                {
                    // Verify CommitAsync belongs to IUnitOfWork
                    var commitSymbol = context.SemanticModel.GetSymbolInfo(commitCall).Symbol as IMethodSymbol;
                    if (commitSymbol == null)
                    {
                        // Emit warning even without full resolution — the pattern is suspicious
                        context.ReportDiagnostic(Diagnostic.Create(Rule, commitCall.GetLocation()));
                        continue;
                    }

                    var commitTypeName = commitSymbol.ContainingType?.ToDisplayString();
                    if (commitTypeName != null && commitTypeName.Contains("UnitOfWork"))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Rule, commitCall.GetLocation()));
                    }
                }
            }
        }
    }
}



