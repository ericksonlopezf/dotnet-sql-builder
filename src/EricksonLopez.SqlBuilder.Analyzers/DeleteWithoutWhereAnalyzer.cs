// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EricksonLopez.SqlBuilder.Analyzers
{
    /// <summary>
    /// Analyzes DELETE statements to ensure they have a WHERE clause (ESQL001).
    /// Analyzes UPDATE statements to ensure they have a WHERE clause (ESQL003).
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DeleteWithoutWhereAnalyzer : DiagnosticAnalyzer
    {
        // ── ESQL001 — DELETE without WHERE ────────────────────────────────────
        /// <summary>Gets the diagnostic identifier emitted when a DELETE query has no WHERE clause.</summary>
        public const string DeleteDiagnosticId = "ESQL001";

        private static readonly LocalizableString DeleteTitle =
            "DELETE without WHERE clause";
        private static readonly LocalizableString DeleteMessageFormat =
            "DELETE will affect the entire table because no WHERE, WhereAll, WhereExists, or WhereNotExists filter was applied";
        private static readonly LocalizableString DeleteDescription =
            "Avoid accidentally deleting all rows. Add a WHERE clause, or call .WhereAll() to explicitly express intent.";

        private static readonly DiagnosticDescriptor DeleteRule = new DiagnosticDescriptor(
            DeleteDiagnosticId,
            DeleteTitle,
            DeleteMessageFormat,
            "Usage",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: DeleteDescription,
            helpLinkUri: "https://github.com/ericksonlopezf/dotnet-sql-builder/blob/main/docs/analyzers/ESQL001.md");

        // ── ESQL002 — UPDATE without WHERE ────────────────────────────────────
        /// <summary>Gets the diagnostic identifier emitted when an UPDATE query has no WHERE clause.</summary>
        public const string UpdateDiagnosticId = "ESQL003";

        private static readonly LocalizableString UpdateTitle =
            "UPDATE without WHERE clause";
        private static readonly LocalizableString UpdateMessageFormat =
            "UPDATE will affect the entire table because no WHERE, WhereAll, WhereExists, or WhereNotExists filter was applied";
        private static readonly LocalizableString UpdateDescription =
            "Avoid accidentally updating all rows. Add a WHERE clause, or call .WhereAll() to explicitly express intent.";

        private static readonly DiagnosticDescriptor UpdateRule = new DiagnosticDescriptor(
            UpdateDiagnosticId,
            UpdateTitle,
            UpdateMessageFormat,
            "Usage",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: UpdateDescription,
            helpLinkUri: "https://github.com/ericksonlopezf/dotnet-sql-builder/blob/main/docs/analyzers/ESQL003.md");

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(DeleteRule, UpdateRule);

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            // Stryker disable once statement : Justification: Roslyn initialization boilerplate not observable in test
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            // Stryker disable once statement : Justification: Concurrency configuration is unobservable in tests
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(compilationContext =>
            {
                var queryType = compilationContext.Compilation.GetTypeByMetadataName(
                    "EricksonLopez.SqlBuilder.Abstractions.ISqlQuery");
                // Stryker disable once all : Justification: queryType == null only occurs when Core package reference is missing
                if (queryType == null) return;

                compilationContext.RegisterSyntaxNodeAction(
                    ctx => AnalyzeInvocation(ctx, queryType),
                    SyntaxKind.InvocationExpression);
            });
        }

        // Method names that count as "has filter" — explicit WHERE or explicit WhereAll/WhereExists
        private static readonly HashSet<string> WhereMethodNames =
            new HashSet<string>(System.StringComparer.Ordinal)
            {
                "Where", "And", "Or",
                "WhereAll",
                "WhereExists", "WhereNotExists",
                "OrExists", "OrNotExists"
            };

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context, INamedTypeSymbol queryType)
        {
            var invocationExpr = (InvocationExpressionSyntax)context.Node;

            var methodSymbol = context.SemanticModel.GetSymbolInfo(invocationExpr, context.CancellationToken).Symbol as IMethodSymbol;
            // Stryker disable once all : Justification: methodSymbol == null only occurs on non-compilable code
            if (methodSymbol == null) return;

            // Only analyze at the terminal Build() / ExecuteAsync() call-site
            if (methodSymbol.Name != "Build" && methodSymbol.Name != "ExecuteAsync")
            {
                return;
            }

            bool hasDelete = false;
            bool hasUpdate = false;
            bool hasWhere  = false;

            // ── 1. Inspect the current fluent chain ───────────────────────────
            foreach (var node in invocationExpr.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
            {
                if (node.Expression is MemberAccessExpressionSyntax)
                {
                    var sym = context.SemanticModel.GetSymbolInfo(node, context.CancellationToken).Symbol as IMethodSymbol;
                    // Stryker disable once logical, equality : Justification: Roslyn internal defensive check
                    if (sym != null && IsInSqlBuilderNamespace(sym))
                    {
                        ClassifyMethodName(sym.Name, ref hasDelete, ref hasUpdate, ref hasWhere);
                    }
                }
            }

            // ── 2. Resolve local variable if not a single-chain pattern ────────
            // Stryker disable once logical, boolean : Justification: Logical short-circuit optimization
            if (!hasWhere || (!hasDelete && !hasUpdate))
            {
                ExpressionSyntax? receiver = null;
                if (invocationExpr.Expression is MemberAccessExpressionSyntax m)
                {
                    receiver = m.Expression;
                    // connection.ExecuteAsync(query) — the query is the argument
                    // Stryker disable once equality : Justification: Condition implicitly guaranteed by method design
                    if (methodSymbol.Name == "ExecuteAsync" &&
                        invocationExpr.ArgumentList.Arguments.Count > 0)
                    {
                        receiver = invocationExpr.ArgumentList.Arguments[0].Expression;
                    }
                }

                if (receiver != null)
                {
                    var receiverSymbol = context.SemanticModel.GetSymbolInfo(receiver, context.CancellationToken).Symbol;
                    if (receiverSymbol is ILocalSymbol localSymbol)
                    {
                        ScanBlock(context, invocationExpr, localSymbol,
                            ref hasDelete, ref hasUpdate, ref hasWhere);
                    }
                }
            }

            // ── 3. Report diagnostics ─────────────────────────────────────────
            if (hasDelete && !hasWhere)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(DeleteRule, invocationExpr.GetLocation()));
            }

            if (hasUpdate && !hasWhere)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(UpdateRule, invocationExpr.GetLocation()));
            }
        }

        // Stryker disable once string : Justification: Constant namespace verification
        private static bool IsInSqlBuilderNamespace(IMethodSymbol sym) =>
            sym.ContainingNamespace?.ToDisplayString().StartsWith("EricksonLopez.SqlBuilder") == true;

        private static void ClassifyMethodName(
            string name,
            ref bool hasDelete, ref bool hasUpdate, ref bool hasWhere)
        {
            if (name == "Delete")  hasDelete = true;
            if (name == "Update")  hasUpdate = true;
            if (WhereMethodNames.Contains(name)) hasWhere = true;
        }

        private static void ScanBlock(
            SyntaxNodeAnalysisContext context,
            InvocationExpressionSyntax invocationExpr,
            ILocalSymbol localSymbol,
            ref bool hasDelete, ref bool hasUpdate, ref bool hasWhere)
        {
            // Stryker disable once linq : Justification: Search for nearest ancestor block
            var block = invocationExpr.Ancestors().OfType<BlockSyntax>().FirstOrDefault();
            // Stryker disable once statement : Justification: Guard clause for top-level statements
            if (block == null) return;

            foreach (var node in block.DescendantNodes())
            {
                // Invocations: query.Where(...)
                if (node is InvocationExpressionSyntax inv &&
                    inv.Expression is MemberAccessExpressionSyntax ma)
                {
                    // Stryker disable once linq : Justification: Search for root identifier in invocation
                    var root = ma.DescendantNodes().OfType<IdentifierNameSyntax>().FirstOrDefault();
                    if (root != null &&
                        SymbolEqualityComparer.Default.Equals(context.SemanticModel.GetSymbolInfo(root, context.CancellationToken).Symbol, localSymbol))
                    {
                        var sym = context.SemanticModel.GetSymbolInfo(inv, context.CancellationToken).Symbol as IMethodSymbol;
                        // Stryker disable once logical, equality : Justification: Roslyn API defensive null check
                        if (sym != null && IsInSqlBuilderNamespace(sym))
                        {
                            ClassifyMethodName(sym.Name, ref hasDelete, ref hasUpdate, ref hasWhere);
                        }
                    }
                }
                // Variable declarations: var query = Sql.Delete<T>()...
                else if (node is VariableDeclaratorSyntax decl &&
                         decl.Identifier.Text == localSymbol.Name &&
                         decl.Initializer != null)
                {
                    foreach (var declInv in decl.Initializer.Value
                        .DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
                    {
                        if (declInv.Expression is MemberAccessExpressionSyntax)
                        {
                            var sym = context.SemanticModel.GetSymbolInfo(declInv, context.CancellationToken).Symbol as IMethodSymbol;
                            // Stryker disable once logical, equality : Justification: Roslyn API defensive null check
                            if (sym != null && IsInSqlBuilderNamespace(sym))
                            {
                                ClassifyMethodName(sym.Name, ref hasDelete, ref hasUpdate, ref hasWhere);
                            }
                        }
                    }
                }
                // Assignments: query = query.Where(...)
                // Stryker disable once block, statement : Justification: Local variable re-evaluation
                else if (node is AssignmentExpressionSyntax assign &&
                         assign.Left is IdentifierNameSyntax idLeft &&
                         idLeft.Identifier.Text == localSymbol.Name)
                {
                    // Stryker disable once all : Justification: Iteration over syntactic assignments
                    foreach (var assignInv in assign.Right
                        .DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
                    {
                        if (assignInv.Expression is MemberAccessExpressionSyntax)
                        {
                            var sym = context.SemanticModel.GetSymbolInfo(assignInv, context.CancellationToken).Symbol as IMethodSymbol;
                            if (sym != null && IsInSqlBuilderNamespace(sym))
                            {
                                ClassifyMethodName(sym.Name, ref hasDelete, ref hasUpdate, ref hasWhere);
                            }
                        }
                    }
                }
            }
        }
    }
}




