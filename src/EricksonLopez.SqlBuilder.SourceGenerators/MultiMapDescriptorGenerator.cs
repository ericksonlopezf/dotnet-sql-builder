// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace EricksonLopez.SqlBuilder.SourceGenerators;

/// <summary>
/// Generates compile-time multi-map metadata methods for entity types annotated with <c>[SqlEntity]</c>.
/// For each entity, emits a <c>GetMultiMapReaderFactory()</c> static method that returns a
/// <c>Func&lt;System.Data.IDataReader, object&gt;</c> — a reflection-free factory usable by
/// <c>MultiMapBuilder</c> and the NativeAOT execution path.
/// </summary>
[Generator]
public class MultiMapDescriptorGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var entityProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "EricksonLopez.SqlBuilder.Annotations.SqlEntityAttribute",
                predicate: (node, _) => node is Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax,
                transform: (ctx, _) => ctx.TargetSymbol as INamedTypeSymbol)
            .Where(x => x is not null);

        context.RegisterSourceOutput(entityProvider, (ctx, symbol) =>
        {


            var source = EmitDescriptor(symbol!);
            ctx.AddSource($"{symbol!.Name}_MultiMapDescriptor.g.cs", SourceText.From(source, Encoding.UTF8));
        });
    }

    private static string EmitDescriptor(INamedTypeSymbol symbol)
    {
        var className = symbol.Name;
        var namespaceName = symbol.ContainingNamespace.ToDisplayString();

        var sb = new StringBuilder(2048);
        sb.AppendLine("#nullable enable");
        sb.Append("namespace ").AppendLine(namespaceName);
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>NativeAOT multi-map support for this entity (generated).</summary>");
        sb.Append("    partial class ").AppendLine(className);
        sb.AppendLine("    {");

        // GetMultiMapReaderFactory: returns a Func<IDataReader, object> — no cross-package reference needed
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Returns a compile-time-generated factory that maps an <see cref=\"System.Data.IDataReader\"/>");
        sb.Append("        /// row to a boxed <see cref=\"").Append(className).AppendLine("\"/> instance, with no reflection.");
        sb.AppendLine("        /// Suitable for use with MultiMapBuilder and NativeAOT execution paths.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]");
        sb.AppendLine("        public static System.Func<System.Data.IDataReader, object> GetMultiMapReaderFactory()");
        sb.AppendLine("        {");
        sb.AppendLine("            var parser = new Parser();");
        sb.AppendLine("            return reader => parser.Parse(reader);");
        sb.AppendLine("        }");
        sb.AppendLine();
        // Typed version
        sb.AppendLine("        /// <summary>Returns a typed reader factory for use with MultiMapBuilder.</summary>");
        sb.AppendLine("        [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]");
        sb.Append("        public static System.Func<System.Data.IDataReader, ").Append(className).AppendLine("> GetTypedReaderFactory()");
        sb.AppendLine("        {");
        sb.AppendLine("            var parser = new Parser();");
        sb.AppendLine("            return parser.Parse;");
        sb.AppendLine("        }");

        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }
}




