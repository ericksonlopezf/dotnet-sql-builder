// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
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
    /// Provides a code fix to convert unsafe string concatenation to safe string interpolation.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnsafeStringConcatenationCodeFix)), Shared]
    public class UnsafeStringConcatenationCodeFix : CodeFixProvider
    {
        /// <inheritdoc />
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(UnsafeStringConcatenationAnalyzer.DiagnosticId);

        /// <inheritdoc />
        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        /// <inheritdoc />
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null) return;
            var diagnostic = context.Diagnostics[0];
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var token = root.FindToken(diagnosticSpan.Start);
            var declaration = token.Parent?.AncestorsAndSelf().OfType<BinaryExpressionSyntax>().FirstOrDefault();
            if (declaration == null) return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Convert to string interpolation",
                    createChangedDocument: c => ConvertToInterpolatedStringAsync(context.Document, declaration, c),
                    equivalenceKey: "InterpolatedStringFix"),
                diagnostic);
        }

        private async Task<Document> ConvertToInterpolatedStringAsync(Document document, BinaryExpressionSyntax addExpr, CancellationToken cancellationToken)
        {
            var content = new List<InterpolatedStringContentSyntax>();
            
            AddInterpolatedContent(addExpr, content);
            
            var interpolatedString = SyntaxFactory.InterpolatedStringExpression(
                SyntaxFactory.Token(SyntaxKind.InterpolatedStringStartToken),
                SyntaxFactory.List(content)
            );
            
            var root = (await document.GetSyntaxRootAsync(cancellationToken))!;
            var newRoot = root.ReplaceNode((SyntaxNode)addExpr, (SyntaxNode)interpolatedString);
            return document.WithSyntaxRoot(newRoot);
        }
        
        private void AddInterpolatedContent(ExpressionSyntax expr, List<InterpolatedStringContentSyntax> content)
        {
            if (expr is BinaryExpressionSyntax binary && binary.IsKind(SyntaxKind.AddExpression))
            {
                AddInterpolatedContent(binary.Left, content);
                AddInterpolatedContent(binary.Right, content);
            }
            else if (expr is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression))
            {
                content.Add(SyntaxFactory.InterpolatedStringText(
                    SyntaxFactory.Token(
                        SyntaxTriviaList.Empty,
                        SyntaxKind.InterpolatedStringTextToken,
                        literal.Token.ValueText,
                        literal.Token.ValueText,
                        SyntaxTriviaList.Empty)));
            }
            else
            {
                content.Add(SyntaxFactory.Interpolation(expr));
            }
        }
    }
}






