// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace EricksonLopez.SqlBuilder.Testing;

/// <summary>
/// Scoped test fixture for capturing OpenTelemetry and System.Diagnostics Activities during testing.
/// </summary>
public sealed class DiagnosticActivityScope : IDisposable
{
    private readonly ActivityListener _listener;
    private readonly ConcurrentBag<Activity> _activities = new();

    /// <summary>
    /// Gets all activities captured during the lifetime of this scope.
    /// </summary>
    public IReadOnlyCollection<Activity> Activities => _activities;

    private DiagnosticActivityScope(string sourceName)
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == sourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => _activities.Add(activity)
        };
        ActivitySource.AddActivityListener(_listener);
    }

    /// <summary>
    /// Starts capturing activities from the specified ActivitySource name.
    /// </summary>
    /// <param name="sourceName">The name of the ActivitySource to listen to.</param>
    /// <returns>An active <see cref="DiagnosticActivityScope"/> instance.</returns>
    public static DiagnosticActivityScope Start(string sourceName = "EricksonLopez.SqlBuilder")
    {
        return new DiagnosticActivityScope(sourceName);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _listener.Dispose();
    }
}
