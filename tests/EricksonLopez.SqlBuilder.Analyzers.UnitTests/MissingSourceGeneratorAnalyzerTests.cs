// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;

namespace EricksonLopez.SqlBuilder.Analyzers.Tests
{
    public class MissingSourceGeneratorAnalyzerTests
    {
        [Fact]
        public async Task SqlEntity_WithoutStaticMetadataInterface_ReportsDiagnostic()
        {
            var code = @"

namespace EricksonLopez.SqlBuilder.Annotations
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SqlEntityAttribute : Attribute
    {
        public SqlEntityAttribute(string? tableName = null) { }
    }
}

namespace EricksonLopez.SqlBuilder.Abstractions.Metadata
{
    public interface IStaticEntityMetadata<T> { }
}

namespace TestNamespace
{
    using EricksonLopez.SqlBuilder.Annotations;

    [SqlEntity(""Users"")]
    public class {|#0:User|}
    {
        public int Id { get; set; }
    }
}";

            var expected = VerifyMissingSourceGenerator.Diagnostic("ESQL021")
                .WithLocation(0)
                .WithArguments("User");

            await VerifyMissingSourceGenerator.VerifyAnalyzerAsync(code, expected);
        }

        [Fact]
        public async Task SqlEntity_WhenMetadataInterfaceMissingFromCompilation_ReportsDiagnostic()
        {
            var code = @"

namespace EricksonLopez.SqlBuilder.Annotations
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SqlEntityAttribute : Attribute
    {
        public SqlEntityAttribute(string? tableName = null) { }
    }
}

namespace TestNamespace
{
    using EricksonLopez.SqlBuilder.Annotations;

    [SqlEntity]
    public class {|#0:Customer|}
    {
        public int Id { get; set; }
    }
}";

            var expected = VerifyMissingSourceGenerator.Diagnostic("ESQL021")
                .WithLocation(0)
                .WithArguments("Customer");

            await VerifyMissingSourceGenerator.VerifyAnalyzerAsync(code, expected);
        }

        [Fact]
        public async Task SqlEntity_WithStaticMetadataInterface_DoesNotReportDiagnostic()
        {
            var code = @"

namespace EricksonLopez.SqlBuilder.Annotations
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SqlEntityAttribute : Attribute
    {
        public SqlEntityAttribute(string? tableName = null) { }
    }
}

namespace EricksonLopez.SqlBuilder.Abstractions.Metadata
{
    public interface IStaticEntityMetadata<T> { }
}

namespace TestNamespace
{
    using EricksonLopez.SqlBuilder.Abstractions.Metadata;

    [SqlEntity(""Users"")]
    public class User : IStaticEntityMetadata<User>
    {
        public int Id { get; set; }
    }

    public class NormalClass
    {
        public int Id { get; set; }
    }
}";
            await VerifyMissingSourceGenerator.VerifyAnalyzerAsync(code);
        }
    }

    public static class VerifyMissingSourceGenerator
    {
        public static DiagnosticResult Diagnostic(string diagnosticId)
        {
            return CSharpAnalyzerVerifier<MissingSourceGeneratorAnalyzer, DefaultVerifier>.Diagnostic(diagnosticId);
        }

        public static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
        {
            var test = new CSharpAnalyzerTest<MissingSourceGeneratorAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                CompilerDiagnostics = CompilerDiagnostics.None,
            };
            test.TestState.Sources.Add("global using System;\nglobal using System.Collections.Generic;\nglobal using System.Linq;\nglobal using System.Linq.Expressions;\nglobal using System.Threading;\nglobal using System.Threading.Tasks;\n");
            
            
            test.ExpectedDiagnostics.AddRange(expected);
            return test.RunAsync();
        }
    }
}



