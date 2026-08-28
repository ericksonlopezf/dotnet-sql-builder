// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

#pragma warning disable CA1806
namespace EricksonLopez.SqlBuilder.SourceGenerators;

/// <summary>
/// Generates boilerplate AOT metadata, hydration logic, and column definitions for SQL entities.
/// </summary>
[Generator]
public class SqlEntityGenerator : IIncrementalGenerator
{
    private static string ToSnakeCase(string name)
    {
        int count = 0;
        for (int i = 1; i < name.Length; i++)
        {
            if (char.IsUpper(name[i])) count++;
        }
        
        if (count == 0) return name.ToLowerInvariant();
        
        var chars = new char[name.Length + count];
        int j = 0;
        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]))
            {
                chars[j++] = '_';
            }
            chars[j++] = char.ToLowerInvariant(name[i]);
        }
        return new string(chars);
    }
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider.ForAttributeWithMetadataName(
            "EricksonLopez.SqlBuilder.Annotations.SqlEntityAttribute",
            predicate: (node, _) => node is Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax,
            transform: TransformNode)
            .Where(x => x is not null);

        context.RegisterSourceOutput(provider, (ctx, model) =>
        {
            if (!model!.IsPartial)
            {
                var descriptor = new DiagnosticDescriptor(
                    "ESQL005", "Class must be partial", 
                    $"The class {model.ClassName} must be marked as partial to use [SqlEntity]", 
                    "Usage", DiagnosticSeverity.Error, isEnabledByDefault: true);
                ctx.ReportDiagnostic(Diagnostic.Create(descriptor, Location.None));
                return;
            }

            var className = model.ClassName;
            var tableName = model.TableName;
            var namespaceName = model.NamespaceName;
            
            var sb = new StringBuilder(4096);
            sb.AppendLine("#nullable enable");
            sb.Append("namespace ").AppendLine(namespaceName);
            sb.AppendLine("{");
            var typeKeyword = model.IsRecord 
                ? (model.IsStruct ? "record struct" : "record") 
                : (model.IsStruct ? "struct" : "class");
                
            sb.AppendLine("    [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]");
            sb.Append("    partial ").Append(typeKeyword).Append(" ").Append(className).Append(" : EricksonLopez.SqlBuilder.Annotations.ISqlEntity, EricksonLopez.SqlBuilder.Metadata.IEntityMetadataProvider<").Append(className).Append(">, EricksonLopez.SqlBuilder.Abstractions.IBulkSerializer<").Append(className).Append(">, EricksonLopez.SqlBuilder.Abstractions.Metadata.IStaticEntityMetadata<").Append(className).AppendLine(">");
            sb.AppendLine("    {");
            
            sb.Append("        public const string TableName = \"").Append(tableName).AppendLine("\";");
            sb.AppendLine("        [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]");
            sb.AppendLine("        public static class Columns");
            sb.AppendLine("        {");
            
            foreach(var member in model.Properties)
            {
                 var snakeCase = ToSnakeCase(member.Name);
                 sb.Append("            public const string ").Append(member.Name).Append(" = \"").Append(snakeCase).AppendLine("\";");
            }
            sb.AppendLine("        }");
            
            sb.Append("        public static readonly string SelectAllTemplate = $\"SELECT ");
            sb.Append(string.Join(", ", model.Properties.Select(p => $"{{Columns.{p.Name}}}")));
            sb.AppendLine(" FROM {TableName}\";");
            
            sb.AppendLine("        public static readonly System.Collections.Generic.IReadOnlyDictionary<string, string> PropertyMap = new System.Collections.Generic.Dictionary<string, string>");
            sb.AppendLine("        {");
            foreach(var member in model.Properties)
            {
                 sb.Append("            { nameof(").Append(member.Name).Append("), Columns.").Append(member.Name).AppendLine(" },");
            }
            sb.AppendLine("        };");
            
            // ISqlEntity Implementation
            sb.AppendLine("        public string GetTableName() => TableName;");
            
            var insertableProperties = model.Properties.Where(p => p.IsInsertable).ToList();
            
            sb.Append("        public string[] GetColumnNames() => new[] { ");
            for (int i = 0; i < insertableProperties.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }

                sb.Append("Columns.").Append(insertableProperties[i].Name);
            }
            sb.AppendLine(" };");
            
            sb.Append("        public object?[] GetValues() => new object?[] { ");
            for (int i = 0; i < insertableProperties.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }

                sb.Append("this.").Append(insertableProperties[i].Name);
            }
            sb.AppendLine(" };");
            
            sb.Append("        public string[] GetAllColumnNames() => new[] { ");
            for (int i = 0; i < model.Properties.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }

                sb.Append("Columns.").Append(model.Properties[i].Name);
            }
            sb.AppendLine(" };");
            
            sb.Append("        public object?[] GetAllValues() => new object?[] { ");
            for (int i = 0; i < model.Properties.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }

                sb.Append("this.").Append(model.Properties[i].Name);
            }
            sb.AppendLine(" };");
            
            var indexedProperties = model.Properties.Where(p => p.IsIndexed).ToList();
            
            sb.AppendLine("        public System.Collections.Generic.IReadOnlyDictionary<string, string> GetPropertyMap() => PropertyMap;");
            
            if (indexedProperties.Count == 0)
            {
                sb.AppendLine("        public string[] GetIndexedColumns() => System.Array.Empty<string>();");
            }
            else
            {
                sb.Append("        public string[] GetIndexedColumns() => new string[] { ");
                for (int i = 0; i < indexedProperties.Count; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append("Columns.").Append(indexedProperties[i].Name);
                }
                sb.AppendLine(" };");
            }
            
            // Alias Generator
            sb.AppendLine("        [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]");
            sb.AppendLine("        public class SqlAlias");
            sb.AppendLine("        {");
            sb.AppendLine("            public string TableAlias { get; }");
            sb.AppendLine("            public SqlAlias(string alias) { TableAlias = alias; }");
            foreach(var member in model.Properties)
            {
                 sb.Append("            public string ").Append(member.Name).Append(" => $\"{TableAlias}.{Columns.").Append(member.Name).AppendLine("}\";");
            }
            sb.AppendLine("        }");
            
            // Parser class for O(1) hydration
            sb.AppendLine("        public class Parser");
            sb.AppendLine("        {");
            sb.AppendLine("            private bool _initialized;");
            foreach(var member in model.Properties)
            {
                sb.Append("            private int _ordinal_").Append(member.Name).AppendLine(";");
            }

            sb.AppendLine("            public void Initialize(System.Data.IDataReader reader)");
            sb.AppendLine("            {");
            sb.AppendLine("                if (_initialized) return;");
            foreach(var member in model.Properties)
            {
                 sb.Append("                _ordinal_").Append(member.Name).Append(" = reader.GetOrdinal(Columns.").Append(member.Name).AppendLine(");");
            }
            sb.AppendLine("                _initialized = true;");
            sb.AppendLine("            }");
            sb.Append("            public ").Append(className).AppendLine(" Parse(System.Data.IDataReader reader)");
            sb.AppendLine("            {");
            sb.AppendLine("                Initialize(reader);");
            sb.Append("                var entity = new ").Append(className).AppendLine("();");
            foreach(var member in model.Properties)
            {
                 sb.Append("                if (!reader.IsDBNull(_ordinal_").Append(member.Name).AppendLine("))");
                 sb.Append("                    entity.").Append(member.Name).Append(" = ").Append(member.CastType).Append("reader.").Append(member.ReaderMethod).Append("(_ordinal_").Append(member.Name).AppendLine(");");
            }
            sb.AppendLine("                return entity;");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            
            // Projection Helper
            sb.Append("        public static System.Func<System.Data.IDataReader, ").Append(className).AppendLine("> GetReaderParser()");
            sb.AppendLine("        {");
            sb.AppendLine("            var parser = new Parser();");
            sb.AppendLine("            return parser.Parse;");
            sb.AppendLine("        }");
            
            sb.Append("        public static ").Append(className).AppendLine(" FromReader(System.Data.IDataReader reader)");
            sb.AppendLine("        {");
            sb.AppendLine("            var parser = new Parser();");
            sb.AppendLine("            return parser.Parse(reader);");
            sb.AppendLine("        }");
            
            // IStaticEntityMetadata Explicit Implementation
            sb.Append("        static string EricksonLopez.SqlBuilder.Abstractions.Metadata.IStaticEntityMetadata<").Append(className).AppendLine(">.TableName => TableName;");
            sb.Append("        static int EricksonLopez.SqlBuilder.Abstractions.Metadata.IStaticEntityMetadata<").Append(className).Append(">.ColumnCount => ").Append(model.Properties.Count).AppendLine(";");
            sb.Append("        static System.ReadOnlySpan<EricksonLopez.SqlBuilder.Abstractions.Metadata.ColumnMetadata> EricksonLopez.SqlBuilder.Abstractions.Metadata.IStaticEntityMetadata<").Append(className).AppendLine(">.GetColumns() => AotMetadata.StaticColumns;");
            sb.Append("        static bool EricksonLopez.SqlBuilder.Abstractions.Metadata.IStaticEntityMetadata<").Append(className).Append(">.IsNull(").Append(className).AppendLine(" entity, int columnIndex) => AotMetadata.Instance.IsNull(entity, columnIndex);");
            sb.Append("        static bool EricksonLopez.SqlBuilder.Abstractions.Metadata.IStaticEntityMetadata<").Append(className).Append(">.IsDefault(").Append(className).AppendLine(" entity, int columnIndex) => AotMetadata.Instance.IsDefault(entity, columnIndex);");
            sb.Append("        static bool EricksonLopez.SqlBuilder.Abstractions.Metadata.IStaticEntityMetadata<").Append(className).Append(">.AreEqual(").Append(className).Append(" entity, ").Append(className).AppendLine(" snapshot, int columnIndex)");
            sb.AppendLine("        {");
            sb.AppendLine("            return columnIndex switch");
            sb.AppendLine("            {");
            for (int i = 0; i < model.Properties.Count; i++)
            {
                var member = model.Properties[i];
                sb.Append("                ").Append(i).Append(" => System.Collections.Generic.EqualityComparer<").Append(member.TypeName).Append(">.Default.Equals(entity.").Append(member.Name).Append(", snapshot.").Append(member.Name).AppendLine("),");
            }
            sb.AppendLine("                _ => throw new System.ArgumentOutOfRangeException(nameof(columnIndex))");
            sb.AppendLine("            };");
            sb.AppendLine("        }");
            sb.Append("        static string EricksonLopez.SqlBuilder.Abstractions.Metadata.IStaticEntityMetadata<").Append(className).AppendLine(">.GetColumnName(int columnIndex)");
            sb.AppendLine("        {");
            sb.AppendLine("            return columnIndex switch");
            sb.AppendLine("            {");
            for (int i = 0; i < model.Properties.Count; i++)
            {
                var member = model.Properties[i];
                sb.Append("                ").Append(i).Append(" => Columns.").Append(member.Name).AppendLine(",");
            }
            sb.AppendLine("                _ => throw new System.ArgumentOutOfRangeException(nameof(columnIndex))");
            sb.AppendLine("            };");
            sb.AppendLine("        }");
            sb.Append("        static string EricksonLopez.SqlBuilder.Abstractions.Metadata.IStaticEntityMetadata<").Append(className).Append(">.BindParameter(").Append(className).AppendLine(" entity, int columnIndex, EricksonLopez.SqlBuilder.Abstractions.IParameterManager parameters)");
            sb.AppendLine("        {");
            sb.AppendLine("            return columnIndex switch");
            sb.AppendLine("            {");
            for (int i = 0; i < model.Properties.Count; i++)
            {
                var member = model.Properties[i];
                sb.Append("                ").Append(i).Append(" => parameters.Add(entity.").Append(member.Name).AppendLine("),");
            }
            sb.AppendLine("                _ => throw new System.ArgumentOutOfRangeException(nameof(columnIndex))");
            sb.AppendLine("            };");
            sb.AppendLine("        }");
            sb.Append("        static void EricksonLopez.SqlBuilder.Abstractions.Metadata.IStaticEntityMetadata<").Append(className).Append(">.ExtractColumnArrays(System.ReadOnlySpan<").Append(className).AppendLine("> entities, System.ReadOnlySpan<bool> activeColumns, EricksonLopez.SqlBuilder.Abstractions.IParameterManager parameters) => AotMetadata.Instance.ExtractColumnArrays(entities, activeColumns, parameters);");
            sb.Append("        static System.Func<System.Data.IDataReader, ").Append(className).Append("> EricksonLopez.SqlBuilder.Abstractions.Metadata.IStaticEntityMetadata<").Append(className).AppendLine(">.GetReaderParser() => GetReaderParser();");
            sb.Append("        static ").Append(className).Append(" EricksonLopez.SqlBuilder.Abstractions.Metadata.IStaticEntityMetadata<").Append(className).AppendLine(">.FromReader(System.Data.IDataReader reader) => FromReader(reader);");

            // IEntityMetadataProvider Implementation
            sb.Append("        public static EricksonLopez.SqlBuilder.Metadata.IEntityMetadata<").Append(className).AppendLine("> Metadata => AotMetadata.Instance;");
            
            sb.Append("        private sealed class AotMetadata : EricksonLopez.SqlBuilder.Metadata.IEntityMetadata<").Append(className).AppendLine(">");
            sb.AppendLine("        {");
            sb.AppendLine("            public static readonly AotMetadata Instance = new();");
            sb.Append("            public string TableName => ").Append(className).AppendLine(".TableName;");
            
            sb.AppendLine("            private static readonly EricksonLopez.SqlBuilder.Metadata.ColumnMetadata[] _columns;");
            sb.AppendLine("            public static readonly EricksonLopez.SqlBuilder.Abstractions.Metadata.ColumnMetadata[] StaticColumns;");
            sb.AppendLine("            static AotMetadata()");
            sb.AppendLine("            {");
            sb.AppendLine("                _columns = new EricksonLopez.SqlBuilder.Metadata.ColumnMetadata[]");
            sb.AppendLine("                {");
            foreach (var member in model.Properties)
            {
                var flags = "EricksonLopez.SqlBuilder.Metadata.ColumnFlags.None";
                var flagList = new List<string>();
                if (member.IsPrimaryKey)
                {
                    flagList.Add("EricksonLopez.SqlBuilder.Metadata.ColumnFlags.PrimaryKey");
                }

                if (!member.IsInsertable)
                {
                    flagList.Add("EricksonLopez.SqlBuilder.Metadata.ColumnFlags.Generated");
                }

                if (flagList.Count > 0)
                {
                    flags = string.Join(" | ", flagList);
                }
                
                sb.Append("                new EricksonLopez.SqlBuilder.Metadata.ColumnMetadata(").Append(className).Append(".Columns.").Append(member.Name).Append(", \"").Append(member.Name).Append("\", ").Append(flags).AppendLine("),");
            }
            sb.AppendLine("                };");
            
            sb.AppendLine("                StaticColumns = new EricksonLopez.SqlBuilder.Abstractions.Metadata.ColumnMetadata[]");
            sb.AppendLine("                {");
            for (int i = 0; i < model.Properties.Count; i++)
            {
                var member = model.Properties[i];
                var flags = "EricksonLopez.SqlBuilder.Abstractions.Metadata.ColumnFlags.None";
                var flagList = new List<string>();
                if (member.IsPrimaryKey)
                {
                    flagList.Add("EricksonLopez.SqlBuilder.Abstractions.Metadata.ColumnFlags.PrimaryKey");
                }

                if (!member.IsInsertable)
                {
                    flagList.Add("EricksonLopez.SqlBuilder.Abstractions.Metadata.ColumnFlags.Identity");
                }

                if (flagList.Count > 0)
                {
                    flags = string.Join(" | ", flagList);
                }
                
                sb.Append("                new EricksonLopez.SqlBuilder.Abstractions.Metadata.ColumnMetadata(").Append(i).Append(", ").Append(className).Append(".Columns.").Append(member.Name).Append(", ").Append(flags).AppendLine("),");
            }
            sb.AppendLine("                };");
            sb.AppendLine("            }");
            sb.AppendLine("            public System.ReadOnlySpan<EricksonLopez.SqlBuilder.Metadata.ColumnMetadata> Columns => _columns;");
            
            // IsNull
            sb.Append("            public bool IsNull(").Append(className).AppendLine(" entity, int columnIndex)");
            sb.AppendLine("            {");
            sb.AppendLine("                return columnIndex switch");
            sb.AppendLine("                {");
            for (int i = 0; i < model.Properties.Count; i++)
            {
                var member = model.Properties[i];
                if (!member.TypeName.EndsWith("?") && member.TypeName != "string")
                {
                    sb.Append("                    ").Append(i).AppendLine(" => false,");
                }
                else
                {
                    sb.Append("                    ").Append(i).Append(" => entity.").Append(member.Name).AppendLine(" is null,");
                }
            }
            sb.AppendLine("                    _ => throw new System.ArgumentOutOfRangeException(nameof(columnIndex))");
            sb.AppendLine("                };");
            sb.AppendLine("            }");
            
            // IsDefault
            sb.Append("            public bool IsDefault(").Append(className).AppendLine(" entity, int columnIndex)");
            sb.AppendLine("            {");
            sb.AppendLine("                return columnIndex switch");
            sb.AppendLine("                {");
            for (int i = 0; i < model.Properties.Count; i++)
            {
                var member = model.Properties[i];
                sb.Append("                    ").Append(i).Append(" => System.Collections.Generic.EqualityComparer<").Append(member.TypeName).Append(">.Default.Equals(entity.").Append(member.Name).AppendLine(", default!),");
            }
            sb.AppendLine("                    _ => throw new System.ArgumentOutOfRangeException(nameof(columnIndex))");
            sb.AppendLine("                };");
            sb.AppendLine("            }");
            
            // GetBoxedValue
            sb.Append("            public object? GetBoxedValue(").Append(className).AppendLine(" entity, int columnIndex)");
            sb.AppendLine("            {");
            sb.AppendLine("                return columnIndex switch");
            sb.AppendLine("                {");
            for (int i = 0; i < model.Properties.Count; i++)
            {
                var member = model.Properties[i];
                sb.Append("                    ").Append(i).Append(" => entity.").Append(member.Name).AppendLine(",");
            }
            sb.AppendLine("                    _ => throw new System.ArgumentOutOfRangeException(nameof(columnIndex))");
            sb.AppendLine("                };");
            sb.AppendLine("            }");
            
            // ExtractColumnArrays
            sb.Append("            public void ExtractColumnArrays(System.ReadOnlySpan<").Append(className).AppendLine("> entities, System.ReadOnlySpan<bool> activeColumns, EricksonLopez.SqlBuilder.Abstractions.IParameterManager parameters)");
            sb.AppendLine("            {");
            for (int i = 0; i < model.Properties.Count; i++)
            {
                var member = model.Properties[i];
                sb.Append("                ").Append(member.TypeName).Append("[]? array_").Append(i).AppendLine(" = null;");
                sb.Append("                if (activeColumns[").Append(i).Append("]) array_").Append(i).Append(" = new ").Append(member.TypeName).AppendLine("[entities.Length];");
            }
            sb.AppendLine("                for (int i = 0; i < entities.Length; i++)");
            sb.AppendLine("                {");
            sb.AppendLine("                    var entity = entities[i];");
            for (int i = 0; i < model.Properties.Count; i++)
            {
                var member = model.Properties[i];
                sb.Append("                    if (activeColumns[").Append(i).Append("]) array_").Append(i).Append("![i] = entity.").Append(member.Name).AppendLine(";");
            }
            sb.AppendLine("                }");
            for (int i = 0; i < model.Properties.Count; i++)
            {
                var member = model.Properties[i];
                sb.Append("                if (activeColumns[").Append(i).Append("]) parameters.AddNamed(\"C").Append(i).Append("\", array_").Append(i).AppendLine(");");
            }
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            
            sb.AppendLine("        public void Serialize(" + className + " entity, object?[] values)");
            sb.AppendLine("        {");
            for (int i = 0; i < model.Properties.Count; i++)
            {
                sb.Append("            values[").Append(i).Append("] = entity.").Append(model.Properties[i].Name).AppendLine(";");
            }
            sb.AppendLine("        }");

            sb.AppendLine("    }");
            sb.AppendLine("}");

            
            ctx.AddSource($"{className}_SqlMetadata.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        });
    }

    private static SqlEntityModel? TransformNode(GeneratorAttributeSyntaxContext ctx, CancellationToken ct)
    {
        var symbol = (INamedTypeSymbol)ctx.TargetSymbol;
        var attribute = ctx.Attributes[0];

        var tableName = attribute.ConstructorArguments.FirstOrDefault().Value as string;
        tableName = tableName ?? symbol.Name.ToLower() + "s";
        
        var properties = symbol.GetMembers().OfType<IPropertySymbol>()
            .Where(p => !p.IsStatic && p.GetMethod != null && p.GetMethod.DeclaredAccessibility == Accessibility.Public && p.SetMethod != null && p.SetMethod.DeclaredAccessibility == Accessibility.Public)
            .Select(p => ExtractPropertyModel(p))
            .ToList();

        var isPartial = ((Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax)ctx.TargetNode).Modifiers.Any(m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword));

        return new SqlEntityModel(
            symbol.Name,
            tableName,
            symbol.ContainingNamespace.ToDisplayString(),
            symbol.IsRecord,
            symbol.TypeKind == TypeKind.Struct,
            isPartial,
            properties
        );
    }

    private static SqlEntityPropertyModel ExtractPropertyModel(IPropertySymbol p)
    {
        var isDbGen = false;
        var isIndexed = false;
        var hasKeyAttr = false;

        foreach (var a in p.GetAttributes())
        {
            var attrClass = a.AttributeClass?.ToDisplayString() ?? string.Empty;
            if (attrClass == "EricksonLopez.SqlBuilder.Annotations.DatabaseGeneratedAttribute" || 
                attrClass == "EricksonLopez.SqlBuilder.Annotations.GeneratedColumnAttribute")
            {
                isDbGen = true;
            }
            else if (attrClass == "EricksonLopez.SqlBuilder.Annotations.IndexedAttribute")
            {
                isIndexed = true;
            }
            else if (attrClass == "EricksonLopez.SqlBuilder.Annotations.KeyAttribute" || 
                     attrClass == "EricksonLopez.SqlBuilder.Annotations.PrimaryKeyAttribute" ||
                     attrClass == "System.ComponentModel.DataAnnotations.KeyAttribute")
            {
                hasKeyAttr = true;
            }
        }
            
        // Determine reader method and cast
        var readerMethod = "GetValue";
        var cast = $"({p.Type.ToDisplayString()})";
        var actualType = p.Type;
        
        if (actualType is INamedTypeSymbol namedType && namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            actualType = namedType.TypeArguments[0];
        }
        
        switch (actualType.SpecialType)
        {
            case SpecialType.System_Int32: readerMethod = "GetInt32"; cast = ""; break;
            case SpecialType.System_Int64: readerMethod = "GetInt64"; cast = ""; break;
            case SpecialType.System_Int16: readerMethod = "GetInt16"; cast = ""; break;
            case SpecialType.System_Byte: readerMethod = "GetByte"; cast = ""; break;
            case SpecialType.System_Boolean: readerMethod = "GetBoolean"; cast = ""; break;
            case SpecialType.System_String: readerMethod = "GetString"; cast = ""; break;
            case SpecialType.System_DateTime: readerMethod = "GetDateTime"; cast = ""; break;
            case SpecialType.System_Decimal: readerMethod = "GetDecimal"; cast = ""; break;
            case SpecialType.System_Double: readerMethod = "GetDouble"; cast = ""; break;
            case SpecialType.System_Single: readerMethod = "GetFloat"; cast = ""; break;
            case SpecialType.System_Char: readerMethod = "GetChar"; cast = ""; break;
        }
        
        if (actualType.TypeKind == TypeKind.Enum)
        {
            var underlying = ((INamedTypeSymbol)actualType).EnumUnderlyingType;
            if (underlying != null)
            {
                switch (underlying.SpecialType)
                {
                    case SpecialType.System_Int32: readerMethod = "GetInt32"; break;
                    case SpecialType.System_Int64: readerMethod = "GetInt64"; break;
                    case SpecialType.System_Int16: readerMethod = "GetInt16"; break;
                    case SpecialType.System_Byte: readerMethod = "GetByte"; break;
                }
            }
        }
        
        if (actualType.ToDisplayString() == "System.Guid")
        {
            readerMethod = "GetGuid"; cast = "";
        }

        var isPrimaryKey = p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) || hasKeyAttr;

        return new SqlEntityPropertyModel(p.Name, p.Type.ToDisplayString(), !isDbGen, isIndexed, isPrimaryKey, readerMethod, cast);
    }
}
