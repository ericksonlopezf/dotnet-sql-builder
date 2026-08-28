// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EricksonLopez.SqlBuilder.Analyzers
{
    /// <summary>
    /// Provides a code fix that replaces legacy SqlKata <c>new Query(...)</c> instantiations
    /// with the equivalent <c>Sql.From(...)</c> call, as reported by <see cref="SqlKataMigrationAnalyzer"/>.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SqlKataMigrationCodeFixProvider)), Shared]
    public class SqlKataMigrationCodeFixProvider : CodeFixProvider
    {
        /// <summary>Gets the diagnostic IDs this provider can fix.</summary>
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(SqlKataMigrationAnalyzer.DiagnosticId); }
        }

        /// <inheritdoc/>
        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        /// <inheritdoc/>
        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = (await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false))!;

            var diagnostic = context.Diagnostics[0];
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var declaration = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf().OfType<ObjectCreationExpressionSyntax>().FirstOrDefault();
            if (declaration == null) return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Migrate to Sql.From",
                    createChangedDocument: c => MigrateToSqlBuilderAsync(context.Document, declaration, c),
                    equivalenceKey: "Migrate to Sql.From"),
                diagnostic);
        }

        private async Task<Document> MigrateToSqlBuilderAsync(Document document, ObjectCreationExpressionSyntax objectCreation, CancellationToken cancellationToken)
        {
            ExpressionSyntax replacement;

            var args = objectCreation.ArgumentList?.Arguments;
            if (args != null && args.Value.Count > 0)
            {
                // new Query("Users") -> Sql.From("Users")
                var firstArg = args.Value[0].Expression;
                replacement = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("Sql"),
                        SyntaxFactory.IdentifierName("From")))
                    .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(firstArg))));
            }
            else
            {
                // Check if it's new Query().From("Users")
                var parentInvocation = objectCreation.Parent?.Parent as InvocationExpressionSyntax;
                var memberAccess = objectCreation.Parent as MemberAccessExpressionSyntax;
                
                if (memberAccess?.Name.Identifier.Text == "From" && parentInvocation != null)
                {
                    // It is new Query().From("Users")
                    // We replace the entire new Query().From("Users") with Sql.From("Users")
                    var fromArgs = parentInvocation.ArgumentList.Arguments;
                    replacement = SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName("Sql"),
                            SyntaxFactory.IdentifierName("From")))
                        .WithArgumentList(SyntaxFactory.ArgumentList(fromArgs));

                    // keep trivia
                    replacement = replacement.WithLeadingTrivia(parentInvocation.GetLeadingTrivia()).WithTrailingTrivia(parentInvocation.GetTrailingTrivia());

                    var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                    var newRoot = root!.ReplaceNode(parentInvocation, replacement);
                    return document.WithSyntaxRoot(newRoot);
                }

                // fallback new Query() -> Sql.From("Unknown")
                replacement = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("Sql"),
                        SyntaxFactory.IdentifierName("From")))
                    .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal("Unknown"))))));
            }

            // keep trivia
            replacement = replacement.WithLeadingTrivia(objectCreation.GetLeadingTrivia()).WithTrailingTrivia(objectCreation.GetTrailingTrivia());

            var rootBase = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRootBase = rootBase!.ReplaceNode(objectCreation, replacement);

            return document.WithSyntaxRoot(newRootBase);
        }
    }
}






