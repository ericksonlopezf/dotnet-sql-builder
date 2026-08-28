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

namespace EricksonLopez.SqlBuilder.Analyzers;

/// <summary>
/// Provides a code fix to replace '*' with specific columns.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SelectStarCodeFix)), Shared]
public class SelectStarCodeFix : CodeFixProvider
{
    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(SelectStarAnalyzer.DiagnosticId);

    /// <inheritdoc />
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = (await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false))!;

        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var node = root.FindNode(diagnosticSpan);
        var literal = node.DescendantNodesAndSelf().OfType<LiteralExpressionSyntax>().FirstOrDefault();
        if (literal != null)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Replace '*' with specific columns",
                    createChangedDocument: c => ReplaceStarLiteralAsync(context.Document, literal, c),
                    equivalenceKey: "SelectStarLiteralFix"),
                diagnostic);
        }
    }

    private async Task<Document> ReplaceStarLiteralAsync(Document document, LiteralExpressionSyntax literal, CancellationToken cancellationToken)
    {
        var root = (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false))!;
        var newLiteral = SyntaxFactory.LiteralExpression(
            SyntaxKind.StringLiteralExpression,
            SyntaxFactory.Literal(literal.Token.ValueText.Replace("*", "id")));
        
        var newRoot = root.ReplaceNode(literal, newLiteral);
        return document.WithSyntaxRoot(newRoot);
    }
}





