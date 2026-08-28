// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests.Infrastructure;

/// <summary>
/// Definition for tests that inspect or modify process-wide OpenTelemetry diagnostics and <see cref="ActivityListener"/>.
/// Disables parallelization for this collection to prevent race conditions in CI.
/// </summary>
[CollectionDefinition("SqlBuilderDiagnosticsCollection", DisableParallelization = true)]
public class SqlBuilderDiagnosticsCollectionDefinition : ICollectionFixture<SqlBuilderDiagnosticsFixture>
{
}

/// <summary>
/// Fixture providing safe setup and teardown for diagnostic activities and listeners.
/// </summary>
public sealed class SqlBuilderDiagnosticsFixture : IDisposable
{
    private readonly object _lock = new();

    public IDisposable CaptureActivities(Action<Activity> onActivityStopped)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == SqlBuilderDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                lock (_lock)
                {
                    onActivityStopped(activity);
                }
            }
        };

        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    public void Dispose()
    {
        SqlBuilderDiagnostics.ReinitializeMetersForTesting();
        SqlBuilderDiagnostics.LoggerFactory = null;
        SqlBuilderDiagnostics.LogParameters = false;
        SqlBuilderDiagnostics.SlowQueryThresholdMs = 1000;
    }
}
