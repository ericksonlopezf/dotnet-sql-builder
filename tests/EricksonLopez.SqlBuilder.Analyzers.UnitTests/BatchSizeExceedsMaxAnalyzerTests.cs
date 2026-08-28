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
    /// <summary>
    /// Tests for ELSB006 — BatchSizeExceedsMaxAnalyzer.
    /// Verifies that <c>.WithBatchSize(n)</c> with n &gt; 420 triggers a Performance warning.
    /// The threshold of 420 is derived from SQL Server's 2100-parameter limit divided by
    /// a conservative 5 columns per row.
    /// </summary>
    public class BatchSizeExceedsMaxAnalyzerTests
    {
        // ─── Positive cases (should warn) ────────────────────────────────────

        [Fact]
        public async Task WithBatchSize_AboveThreshold_ReportsDiagnostic()
        {
            var code = @"
namespace EricksonLopez.SqlBuilder.Builders
{
    public class BulkBuilder<T>
    {
        public BulkBuilder<T> WithBatchSize(int size) => this;
    }
}

namespace TestNamespace
{
    using EricksonLopez.SqlBuilder.Builders;

    public class TestClass
    {
        public void Test()
        {
            var bulk = new BulkBuilder<object>();
            bulk.WithBatchSize({|#0:1000|});
        }
    }
}";
            var test = new CSharpAnalyzerTest<BatchSizeExceedsMaxAnalyzer, DefaultVerifier>
            {
                TestCode = code,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
                CompilerDiagnostics = CompilerDiagnostics.None,
            };
            test.TestState.Sources.Add("global using System;\nglobal using System.Collections.Generic;\nglobal using System.Linq;\nglobal using System.Linq.Expressions;\nglobal using System.Threading;\nglobal using System.Threading.Tasks;\n");
            
            
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(BatchSizeExceedsMaxAnalyzer.DiagnosticId, Microsoft.CodeAnalysis.DiagnosticSeverity.Warning)
                    .WithLocation(0));
            await test.RunAsync();
        }

        [Fact]
        public async Task WithBatchSize_ExactlyAtLimit_ReportsDiagnostic()
        {
            // threshold is floor(2100/5) = 420; values > 420 warn
            var code = @"
namespace EricksonLopez.SqlBuilder.Builders
{
    public class BulkBuilder<T>
    {
        public BulkBuilder<T> WithBatchSize(int size) => this;
    }
}

namespace TestNamespace
{
    using EricksonLopez.SqlBuilder.Builders;

    public class TestClass
    {
        public void Test()
        {
            var bulk = new BulkBuilder<object>();
            bulk.WithBatchSize({|#0:421|});
        }
    }
}";
            var test = new CSharpAnalyzerTest<BatchSizeExceedsMaxAnalyzer, DefaultVerifier>
            {
                TestCode = code,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
                CompilerDiagnostics = CompilerDiagnostics.None,
            };
            test.TestState.Sources.Add("global using System;\nglobal using System.Collections.Generic;\nglobal using System.Linq;\nglobal using System.Linq.Expressions;\nglobal using System.Threading;\nglobal using System.Threading.Tasks;\n");
            
            
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(BatchSizeExceedsMaxAnalyzer.DiagnosticId, Microsoft.CodeAnalysis.DiagnosticSeverity.Warning)
                    .WithLocation(0));
            await test.RunAsync();
        }

        [Fact]
        public async Task WithBatchSize_LargeValue_ReportsDiagnostic()
        {
            var code = @"
namespace EricksonLopez.SqlBuilder.Builders
{
    public class BulkBuilder<T>
    {
        public BulkBuilder<T> WithBatchSize(int size) => this;
    }
}

namespace TestNamespace
{
    using EricksonLopez.SqlBuilder.Builders;

    public class TestClass
    {
        public void Test()
        {
            var bulk = new BulkBuilder<object>();
            bulk.WithBatchSize({|#0:5000|});
        }
    }
}";
            var test = new CSharpAnalyzerTest<BatchSizeExceedsMaxAnalyzer, DefaultVerifier>
            {
                TestCode = code,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
                CompilerDiagnostics = CompilerDiagnostics.None,
            };
            test.TestState.Sources.Add("global using System;\nglobal using System.Collections.Generic;\nglobal using System.Linq;\nglobal using System.Linq.Expressions;\nglobal using System.Threading;\nglobal using System.Threading.Tasks;\n");
            
            
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(BatchSizeExceedsMaxAnalyzer.DiagnosticId, Microsoft.CodeAnalysis.DiagnosticSeverity.Warning)
                    .WithLocation(0));
            await test.RunAsync();
        }

        // ─── Negative cases (should NOT warn) ────────────────────────────────

        [Fact]
        public async Task WithBatchSize_BelowThreshold_DoesNotReportDiagnostic()
        {
            var code = @"
namespace EricksonLopez.SqlBuilder.Builders
{
    public class BulkBuilder<T>
    {
        public BulkBuilder<T> WithBatchSize(int size) => this;
    }
}

namespace TestNamespace
{
    using EricksonLopez.SqlBuilder.Builders;

    public class TestClass
    {
        public void Test()
        {
            var bulk = new BulkBuilder<object>();
            bulk.WithBatchSize(100);
        }
    }
}";
            var test = new CSharpAnalyzerTest<BatchSizeExceedsMaxAnalyzer, DefaultVerifier>
            {
                TestCode = code,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
                CompilerDiagnostics = CompilerDiagnostics.None,
            };
            test.TestState.Sources.Add("global using System;\nglobal using System.Collections.Generic;\nglobal using System.Linq;\nglobal using System.Linq.Expressions;\nglobal using System.Threading;\nglobal using System.Threading.Tasks;\n");
            
            
            await test.RunAsync(); // no diagnostics
        }

        [Fact]
        public async Task WithBatchSize_AtBoundary_DoesNotReportDiagnostic()
        {
            // exactly 420 should NOT warn (threshold is strictly > 420)
            var code = @"
namespace EricksonLopez.SqlBuilder.Builders
{
    public class BulkBuilder<T>
    {
        public BulkBuilder<T> WithBatchSize(int size) => this;
    }
}

namespace TestNamespace
{
    using EricksonLopez.SqlBuilder.Builders;

    public class TestClass
    {
        public void Test()
        {
            var bulk = new BulkBuilder<object>();
            bulk.WithBatchSize(420);
        }
    }
}";
            var test = new CSharpAnalyzerTest<BatchSizeExceedsMaxAnalyzer, DefaultVerifier>
            {
                TestCode = code,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
                CompilerDiagnostics = CompilerDiagnostics.None,
            };
            test.TestState.Sources.Add("global using System;\nglobal using System.Collections.Generic;\nglobal using System.Linq;\nglobal using System.Linq.Expressions;\nglobal using System.Threading;\nglobal using System.Threading.Tasks;\n");
            
            
            await test.RunAsync(); // no diagnostics at boundary
        }

        [Fact]
        public async Task WithBatchSize_DynamicValue_DoesNotReportDiagnostic()
        {
            // runtime variables can't be statically evaluated — should not warn
            var code = @"
namespace EricksonLopez.SqlBuilder.Builders
{
    public class BulkBuilder<T>
    {
        public BulkBuilder<T> WithBatchSize(int size) => this;
    }
}

namespace TestNamespace
{
    using EricksonLopez.SqlBuilder.Builders;

    public class TestClass
    {
        public void Test(int runtimeBatchSize)
        {
            var bulk = new BulkBuilder<object>();
            bulk.WithBatchSize(runtimeBatchSize);
        }
    }
}";
            var test = new CSharpAnalyzerTest<BatchSizeExceedsMaxAnalyzer, DefaultVerifier>
            {
                TestCode = code,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
                CompilerDiagnostics = CompilerDiagnostics.None,
            };
            test.TestState.Sources.Add("global using System;\nglobal using System.Collections.Generic;\nglobal using System.Linq;\nglobal using System.Linq.Expressions;\nglobal using System.Threading;\nglobal using System.Threading.Tasks;\n");
            
            
            await test.RunAsync(); // no diagnostics — not a compile-time constant
        }

        [Fact]
        public async Task WithBatchSize_WrongType_DoesNotReportDiagnostic()
        {
            // Method with same name but not in EricksonLopez namespace should not warn
            var code = @"
namespace ThirdParty
{
    public class BatchProcessor
    {
        public BatchProcessor WithBatchSize(int size) => this;
    }
}

namespace TestNamespace
{
    using EricksonLopez.SqlBuilder.Builders;
    using ThirdParty;

    public class TestClass
    {
        public void Test()
        {
            var proc = new BatchProcessor();
            proc.WithBatchSize(9999); // should not warn — wrong type
        }
    }
}";
            var test = new CSharpAnalyzerTest<BatchSizeExceedsMaxAnalyzer, DefaultVerifier>
            {
                TestCode = code,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
                CompilerDiagnostics = CompilerDiagnostics.None,
            };
            test.TestState.Sources.Add("global using System;\nglobal using System.Collections.Generic;\nglobal using System.Linq;\nglobal using System.Linq.Expressions;\nglobal using System.Threading;\nglobal using System.Threading.Tasks;\n");
            
            
            await test.RunAsync(); // no diagnostics
        }

        [Fact]
        public void Rule_Metadata_IsConfiguredProperly()
        {
            var analyzer = new BatchSizeExceedsMaxAnalyzer();
            var rule = analyzer.SupportedDiagnostics[0];
            AwesomeAssertions.AssertionExtensions.Should(rule.Id).Be("ELSB006");
            AwesomeAssertions.AssertionExtensions.Should(rule.Title.ToString()).Be("Batch size exceeds provider parameter limit");
            AwesomeAssertions.AssertionExtensions.Should(rule.MessageFormat.ToString()).Contain("The batch size of {0} may exceed the parameter limit");
            AwesomeAssertions.AssertionExtensions.Should(rule.Category).Be("Performance");
            AwesomeAssertions.AssertionExtensions.Should(rule.DefaultSeverity).Be(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning);
            AwesomeAssertions.AssertionExtensions.Should(rule.IsEnabledByDefault).BeTrue();
            AwesomeAssertions.AssertionExtensions.Should(rule.Description.ToString()).Contain("SQL providers have a maximum number of parameters");
            AwesomeAssertions.AssertionExtensions.Should(rule.HelpLinkUri).Be("https://github.com/ericksonlopezf/dotnet-sql-builder/blob/main/docs/analyzers/ELSB006.md");
        }

        [Fact]
        public async Task DirectInvocation_DoesNotReportDiagnostic()
        {
            var code = @"
public class TestClass
{
    public static void WithBatchSize(int size) { }
    public void Test()
    {
        WithBatchSize(1000);
    }
}";
            var test = new CSharpAnalyzerTest<BatchSizeExceedsMaxAnalyzer, DefaultVerifier>
            {
                TestCode = code,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
                CompilerDiagnostics = CompilerDiagnostics.None,
            };
            test.TestState.Sources.Add("global using System;\nglobal using System.Collections.Generic;\nglobal using System.Linq;\nglobal using System.Linq.Expressions;\nglobal using System.Threading;\nglobal using System.Threading.Tasks;\n");
            
            
            await test.RunAsync();
        }

        [Fact]
        public async Task ZeroArguments_DoesNotReportDiagnostic()
        {
            var code = @"
namespace EricksonLopez.SqlBuilder.Builders
{
    public class BulkBuilder<T>
    {
        public BulkBuilder<T> WithBatchSize() => this;
    }
}
namespace TestNamespace
{
    using EricksonLopez.SqlBuilder.Builders;
    public class TestClass
    {
        public void Test()
        {
            var bulk = new BulkBuilder<object>();
            bulk.WithBatchSize();
        }
    }
}";
            var test = new CSharpAnalyzerTest<BatchSizeExceedsMaxAnalyzer, DefaultVerifier>
            {
                TestCode = code,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
                CompilerDiagnostics = CompilerDiagnostics.None,
            };
            test.TestState.Sources.Add("global using System;\nglobal using System.Collections.Generic;\nglobal using System.Linq;\nglobal using System.Linq.Expressions;\nglobal using System.Threading;\nglobal using System.Threading.Tasks;\n");
            
            
            await test.RunAsync();
        }

        [Fact]
        public async Task NonIntConstant_DoesNotReportDiagnostic()
        {
            var code = @"
namespace EricksonLopez.SqlBuilder.Builders
{
    public class BulkBuilder<T>
    {
        public BulkBuilder<T> WithBatchSize(string size) => this;
    }
}
namespace TestNamespace
{
    using EricksonLopez.SqlBuilder.Builders;
    public class TestClass
    {
        public void Test()
        {
            var bulk = new BulkBuilder<object>();
            bulk.WithBatchSize(""1000"");
        }
    }
}";
            var test = new CSharpAnalyzerTest<BatchSizeExceedsMaxAnalyzer, DefaultVerifier>
            {
                TestCode = code,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
                CompilerDiagnostics = CompilerDiagnostics.None,
            };
            test.TestState.Sources.Add("global using System;\nglobal using System.Collections.Generic;\nglobal using System.Linq;\nglobal using System.Linq.Expressions;\nglobal using System.Threading;\nglobal using System.Threading.Tasks;\n");
            
            
            await test.RunAsync();
        }

        [Fact]
        public async Task OtherMethod_DoesNotReportDiagnostic()
        {
            var code = @"
namespace EricksonLopez.SqlBuilder.Builders
{
    public class BulkBuilder<T>
    {
        public BulkBuilder<T> WithOtherSetting(int size) => this;
    }
}
namespace TestNamespace
{
    using EricksonLopez.SqlBuilder.Builders;
    public class TestClass
    {
        public void Test()
        {
            var bulk = new BulkBuilder<object>();
            bulk.WithOtherSetting(5000);
        }
    }
}";
            var test = new CSharpAnalyzerTest<BatchSizeExceedsMaxAnalyzer, DefaultVerifier>
            {
                TestCode = code,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
                CompilerDiagnostics = CompilerDiagnostics.None,
            };
            test.TestState.Sources.Add("global using System;\nglobal using System.Collections.Generic;\nglobal using System.Linq;\nglobal using System.Linq.Expressions;\nglobal using System.Threading;\nglobal using System.Threading.Tasks;\n");
            
            
            await test.RunAsync();
        }
        [Fact]
        public async Task OtherBuilderInSameNamespace_DoesNotReportDiagnostic()
        {
            var code = @"
namespace EricksonLopez.SqlBuilder.Builders
{
    public class OtherBuilder<T>
    {
        public OtherBuilder<T> WithBatchSize(int size) => this;
    }
}
namespace TestNamespace
{
    using EricksonLopez.SqlBuilder.Builders;
    public class TestClass
    {
        public void Test()
        {
            var other = new OtherBuilder<object>();
            other.WithBatchSize(1000);
        }
    }
}";
            var test = new CSharpAnalyzerTest<BatchSizeExceedsMaxAnalyzer, DefaultVerifier>
            {
                TestCode = code,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
                CompilerDiagnostics = CompilerDiagnostics.None,
            };
            test.TestState.Sources.Add("global using System;\nglobal using System.Collections.Generic;\nglobal using System.Linq;\nglobal using System.Linq.Expressions;\nglobal using System.Threading;\nglobal using System.Threading.Tasks;\n");
            
            
            await test.RunAsync();
        }

        [Fact]
        public async Task BulkBuilderInOtherNamespace_DoesNotReportDiagnostic()
        {
            var code = @"
namespace OtherNamespace.Builders
{
    public class BulkBuilder<T>
    {
        public BulkBuilder<T> WithBatchSize(int size) => this;
    }
}
namespace TestNamespace
{
    using EricksonLopez.SqlBuilder.Builders;
    using OtherNamespace.Builders;
    public class TestClass
    {
        public void Test()
        {
            var other = new BulkBuilder<object>();
            other.WithBatchSize(1000);
        }
    }
}";
            var test = new CSharpAnalyzerTest<BatchSizeExceedsMaxAnalyzer, DefaultVerifier>
            {
                TestCode = code,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
                CompilerDiagnostics = CompilerDiagnostics.None,
            };
            test.TestState.Sources.Add("global using System;\nglobal using System.Collections.Generic;\nglobal using System.Linq;\nglobal using System.Linq.Expressions;\nglobal using System.Threading;\nglobal using System.Threading.Tasks;\n");
            
            
            await test.RunAsync();
        }

        [Fact]
        public async Task DoubleConstant_DoesNotReportDiagnostic()
        {
            var code = @"
namespace EricksonLopez.SqlBuilder.Builders
{
    public class BulkBuilder<T>
    {
        public BulkBuilder<T> WithBatchSize(double size) => this;
    }
}
namespace TestNamespace
{
    using EricksonLopez.SqlBuilder.Builders;
    public class TestClass
    {
        public void Test()
        {
            var bulk = new BulkBuilder<object>();
            bulk.WithBatchSize(1000.5);
        }
    }
}";
            var test = new CSharpAnalyzerTest<BatchSizeExceedsMaxAnalyzer, DefaultVerifier>
            {
                TestCode = code,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
                CompilerDiagnostics = CompilerDiagnostics.None,
            };
            test.TestState.Sources.Add("global using System;\nglobal using System.Collections.Generic;\nglobal using System.Linq;\nglobal using System.Linq.Expressions;\nglobal using System.Threading;\nglobal using System.Threading.Tasks;\n");
            
            
            await test.RunAsync();
        }
    }
}





