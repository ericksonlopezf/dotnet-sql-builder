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
    public class RetryInsideTransactionAnalyzerTests
    {
        [Fact]
        public async Task ResiliencePipelineExecuteAsync_WithCommitInsideLambda_ReportsDiagnostic()
        {
            var code = @"

namespace Polly
{
    public class ResiliencePipeline
    {
        public Task ExecuteAsync(Func<Task> callback) => Task.CompletedTask;
        public void Execute(Action callback) { }
    }
}

namespace EricksonLopez.SqlBuilder
{
    public interface IUnitOfWork
    {
        Task CommitAsync();
        void Commit();
    }
}

namespace TestNamespace
{
    using Polly;
    using EricksonLopez.SqlBuilder;

    public class TestClass
    {
        public async Task TestMethod(ResiliencePipeline pipeline, IUnitOfWork uow)
        {
            await pipeline.ExecuteAsync(async () =>
            {
                await {|#0:uow.CommitAsync()|};
            });

            pipeline.Execute(() =>
            {
                {|#1:uow.Commit()|};
            });
        }
    }
}";

            var expected0 = VerifyRetryInsideTransaction.Diagnostic("ESQL012").WithLocation(0);
            var expected1 = VerifyRetryInsideTransaction.Diagnostic("ESQL012").WithLocation(1);

            await VerifyRetryInsideTransaction.VerifyAnalyzerAsync(code, expected0, expected1);
        }

        [Fact]
        public async Task ResiliencePipelineExecuteAsync_WithoutCommitInsideLambda_DoesNotReportDiagnostic()
        {
            var code = @"

namespace Polly
{
    public class ResiliencePipeline
    {
        public Task ExecuteAsync(Func<Task> callback) => Task.CompletedTask;
        public Task ExecuteAsync(Func<Task> callback, string context) => Task.CompletedTask;
    }
}

namespace OtherNamespace
{
    public class OtherRunner
    {
        public Task ExecuteAsync(Func<Task> callback) => Task.CompletedTask;
    }

    public class OtherService
    {
        public Task CommitAsync() => Task.CompletedTask;
    }
}

namespace EricksonLopez.SqlBuilder
{
    public interface IUnitOfWork
    {
        Task CommitAsync();
    }
}

namespace TestNamespace
{
    using OtherNamespace;

    public class TestClass
    {
        public async Task TestMethod(ResiliencePipeline pipeline, OtherRunner runner, IUnitOfWork uow, OtherService otherService, Func<Task> action)
        {
            await pipeline.ExecuteAsync(async () =>
            {
                await Task.Delay(10);
                await otherService.CommitAsync();
            });

            await pipeline.ExecuteAsync(action);

            await runner.ExecuteAsync(async () =>
            {
                await uow.CommitAsync();
            });
        }
    }
}";
            await VerifyRetryInsideTransaction.VerifyAnalyzerAsync(code);
        }
    }

    public static class VerifyRetryInsideTransaction
    {
        public static DiagnosticResult Diagnostic(string diagnosticId)
        {
            return CSharpAnalyzerVerifier<RetryInsideTransactionAnalyzer, DefaultVerifier>.Diagnostic(diagnosticId);
        }

        public static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
        {
            var test = new CSharpAnalyzerTest<RetryInsideTransactionAnalyzer, DefaultVerifier>
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



