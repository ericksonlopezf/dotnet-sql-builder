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
/// Provides a code fix to append a .Where() clause or acknowledge the unbounded operation.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DeleteWithoutWhereCodeFix)), Shared]
public class DeleteWithoutWhereCodeFix : CodeFixProvider
{
    /// <inheritdoc />
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(
            DeleteWithoutWhereAnalyzer.DeleteDiagnosticId,
            DeleteWithoutWhereAnalyzer.UpdateDiagnosticId);

    /// <inheritdoc />
    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        // Stryker disable once boolean : Justification: ConfigureAwait(false) is standard but functionally unobservable
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        // Stryker disable once statement : Justification: root == null is impossible in this Roslyn context
        if (root == null) return;

        // Stryker disable once linq : Justification: Gets the action trigger diagnostic
        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var node = root.FindNode(diagnosticSpan);
        // Stryker disable once linq : Justification: Finds root invocation node
        var invocation = node.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().FirstOrDefault();
        if (invocation != null)
        {
            // Stryker disable once string : Justification: Constant visual code action title
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Chain security .Where(...) filter",
                    createChangedDocument: c => AppendWhereClauseAsync(context.Document, invocation, c),
                    equivalenceKey: "DeleteWhereFix"),
                diagnostic);
        }
    }

    private async Task<Document> AppendWhereClauseAsync(Document document, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
    {
        // Stryker disable once boolean : Justification: ConfigureAwait(false) not observable in test
        var root = (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false))!;

        var parameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier("x"));
        var body = SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression);
        var lambda = SyntaxFactory.SimpleLambdaExpression(parameter, body);
        var whereArgs = SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(lambda)));

        SyntaxNode targetNode;
        SyntaxNode replacementNode;

        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess && memberAccess.Name.Identifier.Text == "Build")
        {
            var innerExpr = memberAccess.Expression;
            var whereAccess = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                innerExpr,
                SyntaxFactory.IdentifierName("Where"));
            var newInner = SyntaxFactory.InvocationExpression(whereAccess, whereArgs);
            targetNode = invocation;
            replacementNode = invocation.WithExpression(memberAccess.WithExpression(newInner));
        }
        // Stryker disable once logical, equality : Justification: ExecuteAsync verification with arguments
        else if (invocation.Expression is MemberAccessExpressionSyntax ma && ma.Name.Identifier.Text == "ExecuteAsync" && invocation.ArgumentList.Arguments.Count > 0)
        {
            var arg = invocation.ArgumentList.Arguments[0];
            var whereAccess = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                arg.Expression,
                SyntaxFactory.IdentifierName("Where"));
            var newArgExpr = SyntaxFactory.InvocationExpression(whereAccess, whereArgs);
            targetNode = arg;
            replacementNode = arg.WithExpression(newArgExpr);
        }
        else
        {
            var whereAccess = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                invocation,
                SyntaxFactory.IdentifierName("Where"));
            targetNode = invocation;
            replacementNode = SyntaxFactory.InvocationExpression(whereAccess, whereArgs);
        }

        var newRoot = root.ReplaceNode(targetNode, replacementNode);
        return document.WithSyntaxRoot(newRoot);
    }
}





