// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace EricksonLopez.SqlBuilder.SourceGenerators;

/// <summary>
/// Generates strongly-typed filter extension methods for SQL entities.
/// </summary>
[Generator]
public class FilterGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider.ForAttributeWithMetadataName(
            "EricksonLopez.SqlBuilder.Annotations.SqlEntityAttribute",
            predicate: (node, _) => node is Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax,
            transform: (ctx, _) => 
            {
                var symbol = (INamedTypeSymbol)ctx.TargetSymbol;

                var properties = symbol.GetMembers().OfType<IPropertySymbol>()
                    .Where(p => !p.IsStatic && p.GetMethod != null && p.GetMethod.DeclaredAccessibility == Accessibility.Public && p.SetMethod != null && p.SetMethod.DeclaredAccessibility == Accessibility.Public)
                    .Select(p => new PropertyModel(p.Name, p.Type.ToDisplayString(), p.Type.IsValueType, p.Type.IsReferenceType))
                    .ToList();
                    
                return new FilterModel(
                    symbol.Name,
                    symbol.ContainingNamespace.ToDisplayString(),
                    properties
                );
            })
            .Where(x => x is not null);

        context.RegisterSourceOutput(provider, (ctx, model) =>
        {
            var className = model!.ClassName;
            var namespaceName = model.NamespaceName;
            
            var sb = new StringBuilder();
            sb.AppendLine("#nullable enable");
            sb.AppendLine("using System;");
            sb.AppendLine("using EricksonLopez.SqlBuilder;");
            sb.AppendLine();
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");
            sb.AppendLine("    [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]");
            sb.AppendLine($"    public static partial class {className}Filters");
            sb.AppendLine("    {");
            
            foreach(var member in model.Properties)
            {
                var snakeCase = string.Concat(member.Name.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToLower();
                var typeName = member.TypeName;
                
                // Equal
                sb.AppendLine($"        public static SelectQuery<{className}> Where{member.Name}Eq(this SelectQuery<{className}> query, {typeName} value)");
                sb.AppendLine($"            => query.Where((System.FormattableString)$\"{snakeCase} = {{value}}\");");
                
                // Not Equal
                sb.AppendLine($"        public static SelectQuery<{className}> Where{member.Name}NotEq(this SelectQuery<{className}> query, {typeName} value)");
                sb.AppendLine($"            => query.Where((System.FormattableString)$\"{snakeCase} != {{value}}\");");
                
                if (typeName == "int" || typeName == "long" || typeName == "decimal" || typeName == "double" || typeName == "System.DateTime" || typeName == "System.DateOnly")
                {
                    sb.AppendLine($"        public static SelectQuery<{className}> Where{member.Name}Gt(this SelectQuery<{className}> query, {typeName} value)");
                    sb.AppendLine($"            => query.Where((System.FormattableString)$\"{snakeCase} > {{value}}\");");
                    
                    sb.AppendLine($"        public static SelectQuery<{className}> Where{member.Name}Lt(this SelectQuery<{className}> query, {typeName} value)");
                    sb.AppendLine($"            => query.Where((System.FormattableString)$\"{snakeCase} < {{value}}\");");
                }
                
                if (typeName == "string")
                {
                    sb.AppendLine($"        public static SelectQuery<{className}> Where{member.Name}Contains(this SelectQuery<{className}> query, {typeName} value)");
                    sb.AppendLine("        {");
                    sb.AppendLine("            var escaped = value.Replace(\"\\\\\", \"\\\\\\\\\").Replace(\"%\", \"\\\\%\").Replace(\"_\", \"\\\\_\");");
                    sb.AppendLine("            var pattern = \"%\" + escaped + \"%\";");
                    sb.AppendLine($"            return query.Where((System.FormattableString)$\"{snakeCase} LIKE {{pattern}} ESCAPE '\\\\' \");");
                    sb.AppendLine("        }");
                    
                    sb.AppendLine($"        public static SelectQuery<{className}> Where{member.Name}StartsWith(this SelectQuery<{className}> query, {typeName} value)");
                    sb.AppendLine("        {");
                    sb.AppendLine("            var escaped = value.Replace(\"\\\\\", \"\\\\\\\\\").Replace(\"%\", \"\\\\%\").Replace(\"_\", \"\\\\_\");");
                    sb.AppendLine("            var pattern = escaped + \"%\";");
                    sb.AppendLine($"            return query.Where((System.FormattableString)$\"{snakeCase} LIKE {{pattern}} ESCAPE '\\\\' \");");
                    sb.AppendLine("        }");
                }
            }
            sb.AppendLine("    }");
            sb.AppendLine();

            // Generate Strongly Typed Filter DTO class implementing ISqlFilter<T> without reflection
            sb.AppendLine("    [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]");
            sb.AppendLine($"    public class {className}Filter : EricksonLopez.SqlBuilder.Filters.ISqlFilter<{className}>");
            sb.AppendLine("    {");
            foreach(var member in model.Properties)
            {
                var typeName = member.TypeName;
                var propertyType = typeName.EndsWith("?") ? typeName : typeName + "?";
                sb.AppendLine($"        public {propertyType} {member.Name}Eq {{ get; set; }}");
                if (typeName == "int" || typeName == "long" || typeName == "decimal" || typeName == "double" || typeName == "System.DateTime" || typeName == "System.DateOnly")
                {
                    sb.AppendLine($"        public {propertyType} {member.Name}Gt {{ get; set; }}");
                    sb.AppendLine($"        public {propertyType} {member.Name}Lt {{ get; set; }}");
                }
                if (typeName == "string")
                {
                    sb.AppendLine($"        public string? {member.Name}Contains {{ get; set; }}");
                    sb.AppendLine($"        public string? {member.Name}StartsWith {{ get; set; }}");
                }
            }
            sb.AppendLine();
            sb.AppendLine($"        public SelectQuery<{className}> Apply(SelectQuery<{className}> query)");
            sb.AppendLine("        {");
            foreach(var member in model.Properties)
            {
                var typeName = member.TypeName;
                var isValueType = member.IsValueType;

                if (isValueType)
                {
                    sb.AppendLine($"            if ({member.Name}Eq != null) query = query.Where{member.Name}Eq({member.Name}Eq.Value);");
                }
                else
                {
                    sb.AppendLine($"            if ({member.Name}Eq != null) query = query.Where{member.Name}Eq({member.Name}Eq!);");
                }

                if (typeName == "string")
                {
                    sb.AppendLine($"            if (!string.IsNullOrEmpty({member.Name}Contains)) query = query.Where{member.Name}Contains({member.Name}Contains!);");
                    sb.AppendLine($"            if (!string.IsNullOrEmpty({member.Name}StartsWith)) query = query.Where{member.Name}StartsWith({member.Name}StartsWith!);");
                }
                else if (typeName == "int" || typeName == "long" || typeName == "decimal" || typeName == "double" || typeName == "System.DateTime" || typeName == "System.DateOnly")
                {
                    sb.AppendLine($"            if ({member.Name}Gt != null) query = query.Where{member.Name}Gt({member.Name}Gt.Value);");
                    sb.AppendLine($"            if ({member.Name}Lt != null) query = query.Where{member.Name}Lt({member.Name}Lt.Value);");
                }
            }
            sb.AppendLine("            return query;");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            
            ctx.AddSource($"{className}_Filters.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        });
    }
}
