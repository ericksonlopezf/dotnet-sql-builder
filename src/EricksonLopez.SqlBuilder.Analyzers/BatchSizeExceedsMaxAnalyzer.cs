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
    /// Detects when <c>.WithBatchSize(n)</c> is called with a value that exceeds the
    /// known maximum for the target database provider (ELSB006).
    /// </summary>
    /// <remarks>
    /// <para>
    /// SQL Server limits the total number of parameters to 2100 per statement.
    /// For a table with <em>N</em> columns, the safe maximum batch size is floor(2100 / N).
    /// Other providers have different limits:
    /// <list type="bullet">
    ///   <item>SQL Server: 2100 parameters total</item>
    ///   <item>MySQL/MariaDB: 65535 parameters total</item>
    ///   <item>PostgreSQL: 65535 parameters total</item>
    ///   <item>SQLite: 999 parameters total (default, configurable)</item>
    /// </list>
    /// </para>
    /// <para>
    /// This analyzer flags statically-detectable violations. Dynamic batch sizes require
    /// a runtime guard.
    /// </para>
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class BatchSizeExceedsMaxAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>The diagnostic identifier for ELSB006.</summary>
        public const string DiagnosticId = "ELSB006";

        // SQL Server's hard limit of 2100 parameters per statement.
        // This is the most restrictive common provider limit and the default warning threshold.
        private const int SqlServerMaxParameters = 2100;

        // SQLite's default limit (SQLITE_MAX_VARIABLE_NUMBER)
        private const int SqliteMaxBatchSize = 999;

        private static readonly LocalizableString Title =
            "Batch size exceeds provider parameter limit";

        private static readonly LocalizableString MessageFormat =
            "The batch size of {0} may exceed the parameter limit for common database providers (SQL Server: 2100 params, SQLite: 999 params). Consider reducing the batch size or using a native bulk strategy (SqlBulkCopyStrategy, NpgsqlCopyStrategy, MySqlBatchStrategy).";

        private static readonly LocalizableString Description =
            "SQL providers have a maximum number of parameters per statement. If the batch size multiplied by the number of columns exceeds this limit, the query will fail at runtime. For SQL Server the limit is 2100 parameters. Use a native bulk strategy that bypasses the parameter limit for large datasets.";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            "Performance",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: "https://github.com/ericksonlopezf/dotnet-sql-builder/blob/main/docs/analyzers/ELSB006.md");

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Rule);

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            // Stryker disable once statement : Justification: Roslyn initialization boilerplate not observable in test
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            // Stryker disable once statement : Justification: Concurrency configuration is unobservable in tests
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            // Only interested in .WithBatchSize(n)
            var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
            if (memberAccess == null) return;

            if (memberAccess.Name.Identifier.Text != "WithBatchSize") return;

            // Confirm the method belongs to our BulkBuilder<T>
            if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol symbol) return;

            // Stryker disable once string : Justification: Fallback string.Empty asegurado para Contains posterior
            var containingTypeName = symbol.ContainingType?.ToDisplayString() ?? string.Empty;
            // Stryker disable once logical, equality : Justification: Strongly-coupled namespace and class validation
            if (!containingTypeName.Contains("BulkBuilder") ||
                !containingTypeName.Contains("EricksonLopez.SqlBuilder")) return;

            // Evaluate the argument
            if (invocation.ArgumentList.Arguments.Count == 0) return;

            var argExpr = invocation.ArgumentList.Arguments[0].Expression;
            if (context.SemanticModel.GetConstantValue(argExpr, context.CancellationToken).Value is not int batchSize) return;

            // Warn if the batch size is large enough to risk hitting SQL Server's 2100 limit
            // when combined with even a few columns (assume a conservative 5-column minimum).
            // Exact threshold: 2100 / 5 = 420 rows per batch.
            const int conservativeThreshold = SqlServerMaxParameters / 5;

            if (batchSize > conservativeThreshold)
            {
                var location = argExpr.GetLocation();
                context.ReportDiagnostic(Diagnostic.Create(Rule, location, batchSize));
            }
        }
    }
}




