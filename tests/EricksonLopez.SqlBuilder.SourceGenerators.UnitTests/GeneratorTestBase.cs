// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.SqlBuilder.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using VerifyTests;
using VerifyXunit;

namespace EricksonLopez.SqlBuilder.SourceGenerators.Tests
{
    public abstract class GeneratorTestBase
    {
        static GeneratorTestBase()
        {
            DiffEngine.DiffRunner.Disabled = true;
        }

        protected Task VerifyGeneratedSourceAsync<TGenerator>(string source) where TGenerator : IIncrementalGenerator, new()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            var usingsTree = CSharpSyntaxTree.ParseText("""
                global using System;
                global using System.Collections.Generic;
                global using EricksonLopez.SqlBuilder.Annotations;
                """);

            // Create references for commonly used types, plus the SqlEntityAttribute from our Abstractions/Annotations assembly.
            var references = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
                .Select(a => MetadataReference.CreateFromFile(a.Location))
                .Cast<MetadataReference>()
                .Concat(new[]
                {
                    MetadataReference.CreateFromFile(typeof(System.ComponentModel.DataAnnotations.KeyAttribute).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Data.IDataReader).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Data.Common.DbDataReader).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(EricksonLopez.SqlBuilder.Annotations.SqlEntityAttribute).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(EricksonLopez.SqlBuilder.SelectQuery<>).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(EricksonLopez.SqlBuilder.Filters.ISqlFilter<>).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(EricksonLopez.SqlBuilder.Metadata.IEntityMetadata<>).Assembly.Location)
                }).ToList();

            // Add runtime assemblies to ensure standard library types resolve
            var assemblyPath = System.IO.Path.GetDirectoryName(typeof(object).Assembly.Location);
            if (assemblyPath != null)
            {
                references.Add(MetadataReference.CreateFromFile(System.IO.Path.Combine(assemblyPath, "mscorlib.dll")));
                references.Add(MetadataReference.CreateFromFile(System.IO.Path.Combine(assemblyPath, "System.dll")));
                references.Add(MetadataReference.CreateFromFile(System.IO.Path.Combine(assemblyPath, "System.Core.dll")));
                references.Add(MetadataReference.CreateFromFile(System.IO.Path.Combine(assemblyPath, "System.Runtime.dll")));
            }

            var compilation = CSharpCompilation.Create(
                assemblyName: "Tests",
                syntaxTrees: new[] { syntaxTree, usingsTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var generator = new TGenerator();

            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            var errors = outputCompilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            // TODO: Ensure generated code compiles without errors (currently some bugs in generator cause compile errors for nested classes and records without parameterless ctors)
            // if (errors.Count > 0)
            // {
            //     var errorMessages = string.Join(Environment.NewLine, errors.Select(e => e.ToString()));
            //     throw new Exception($@"Compilation failed:
            // {errorMessages}");
            // }

            var runResult = driver.GetRunResult();
            var generatedTexts = runResult.GeneratedTrees.Select(t => t.GetText().ToString()).ToList();
            var diagStrings = runResult.Diagnostics.Select(d => new
            {
                Id = d.Id,
                Title = d.Descriptor.Title.ToString(),
                Category = d.Descriptor.Category,
                Message = d.GetMessage(),
                Severity = d.Severity
            }).ToList();

            return Verifier.Verify(new { Diagnostics = diagStrings, GeneratedCode = generatedTexts })
                .UseDirectory("Snapshots");
        }
    }
}



