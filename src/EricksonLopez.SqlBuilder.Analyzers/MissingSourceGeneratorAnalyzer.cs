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
    /// ESQL021 — Warns when a type is annotated with [SqlEntity] but the Source Generator
    /// package (EricksonLopez.SqlBuilder.SourceGenerators) is not referenced.
    /// Without the generator, the library falls back to reflection-based metadata which:
    /// 1. Breaks NativeAOT publishing.
    /// 2. Degrades startup performance.
    /// 3. May silently produce incorrect column names in trimmed builds.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MissingSourceGeneratorAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>Gets the diagnostic identifier emitted when a <c>[SqlEntity]</c> type is missing the Source Generator package reference.</summary>
        public const string DiagnosticId = "ESQL021";

        private static readonly LocalizableString Title =
            "Source Generator package not referenced";

        private static readonly LocalizableString MessageFormat =
            "Type '{0}' has [SqlEntity] but the Source Generator package is not referenced. " +
            "Add EricksonLopez.SqlBuilder.SourceGenerators to restore AOT-safe code generation " +
            "and eliminate runtime reflection.";

        private static readonly LocalizableString Description =
            "Without EricksonLopez.SqlBuilder.SourceGenerators, entity metadata is resolved " +
            "at runtime via reflection. This is incompatible with NativeAOT and may cause " +
            "incorrect behaviour in trimmed builds. Add the package and mark the class as 'partial'.";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: "https://github.com/ericksonlopezf/dotnet-sql-builder/blob/main/docs/analyzers.md#ESQL021");

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Rule);

        // The FQN of the attribute emitted by the core package
        private const string SqlEntityAttributeFqn =
            "EricksonLopez.SqlBuilder.Annotations.SqlEntityAttribute";

        // The FQN of the interface emitted by the Source Generator
        // When the generator runs, this interface is implemented by the partial class
        private const string StaticMetadataInterfaceFqn =
            "EricksonLopez.SqlBuilder.Abstractions.Metadata.IStaticEntityMetadata`1";

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
        }

        private static void AnalyzeNamedType(SymbolAnalysisContext context)
        {
            var typeSymbol = (INamedTypeSymbol)context.Symbol;

            // Only look at classes decorated with [SqlEntity]
            if (!HasSqlEntityAttribute(typeSymbol, context.Compilation))
                return;

            // Check if the Source Generator has already processed this type.
            // When the generator runs, it makes the class implement IStaticEntityMetadata<T>.
            if (ImplementsStaticMetadataInterface(typeSymbol, context.Compilation))
                return;

            // The type has [SqlEntity] but NOT IStaticEntityMetadata<T> — generator not running.
            // Report on the attribute itself for maximum clarity.
            var location = typeSymbol.Locations[0];

            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                location,
                typeSymbol.Name));
        }

        private static bool HasSqlEntityAttribute(INamedTypeSymbol type, Compilation compilation)
        {
            var sqlEntityAttr = compilation.GetTypeByMetadataName(SqlEntityAttributeFqn);
            if (sqlEntityAttr == null)
                return false;

            return type.GetAttributes().Any(a =>
                SymbolEqualityComparer.Default.Equals(a.AttributeClass, sqlEntityAttr));
        }

        private static bool ImplementsStaticMetadataInterface(
            INamedTypeSymbol type, Compilation compilation)
        {
            var metaInterface = compilation.GetTypeByMetadataName(StaticMetadataInterfaceFqn);
            if (metaInterface == null)
                return false; // Interface doesn't exist = generator package not installed = also bad

            // Check the type AND its base types for the interface implementation
            return type.AllInterfaces.Any(i =>
                i.IsGenericType &&
                SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, metaInterface));
        }
    }
}


