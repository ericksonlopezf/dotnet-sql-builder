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
    /// Detects usages of the legacy SqlKata <c>new Query(...)</c> pattern and
    /// suggests migrating them to the equivalent <c>Sql.From(...)</c> call.
    /// </summary>
    /// <remarks>
    /// Diagnostic ID: <c>ESQL025</c>. Severity: Info.
    /// A companion <see cref="SqlKataMigrationCodeFixProvider"/> is available to apply the migration automatically.
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SqlKataMigrationAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>Gets the diagnostic identifier emitted by this analyzer.</summary>
        public const string DiagnosticId = "ESQL025";

        private static readonly LocalizableString Title = "Migrate SqlKata Query to SqlBuilder";
        private static readonly LocalizableString MessageFormat = "Replace 'new Query(...)' with 'Sql.From(...)'";
        private static readonly LocalizableString Description = "Automatically migrate legacy SqlKata Query instantiations to EricksonLopez.SqlBuilder.";
        private const string Category = "Migration";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Info, isEnabledByDefault: true, description: Description);

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ObjectCreationExpression);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var objectCreation = (ObjectCreationExpressionSyntax)context.Node;
            
            if (objectCreation.Type is IdentifierNameSyntax identifier)
            {
                if (identifier.Identifier.Text == "Query")
                {
                    var diagnostic = Diagnostic.Create(Rule, objectCreation.GetLocation());
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}




