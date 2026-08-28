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
    /// Warns when a method decorated with <c>[RequiresCapability]</c> is invoked without a compiler
    /// known to support the required dialect capability.
    /// </summary>
    /// <remarks>
    /// Emits diagnostic <c>ESQL020</c> as a warning for each call site. A more precise implementation
    /// may inspect the <c>Build</c> call to determine the actual compiler being used.
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DialectSpecificOverloadAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>Gets the diagnostic identifier emitted by this analyzer.</summary>
        public const string DiagnosticId = "ESQL020";
        private static readonly LocalizableString Title = "Capability requirement might not be met";
        private static readonly LocalizableString MessageFormat = "Method '{0}' requires capability '{1}' which might not be supported by the intended compiler";
        private static readonly LocalizableString Description = "Checks if methods decorated with [RequiresCapability] are used safely.";
        private const string Category = "Correctness";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            var methodSymbol = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol as IMethodSymbol;
            if (methodSymbol == null) return;
            
            var attr = methodSymbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "RequiresCapabilityAttribute");
            if (attr != null && attr.ConstructorArguments.Length > 0)
            {
                // In a full implementation, we'd check if the user is passing a specific compiler to Build() 
                // and see if it supports the capability. For now, we issue an informational warning 
                // that this is a dialect-specific feature.
                var capabilityValue = attr.ConstructorArguments[0].Value?.ToString() ?? "Unknown";
                context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), methodSymbol.Name, capabilityValue));
            }
        }
    }
}



