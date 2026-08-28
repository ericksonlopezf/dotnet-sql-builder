// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace EricksonLopez.SqlBuilder;

/// <summary>
/// Provides the diagnostic activity source and metrics for OpenTelemetry instrumentation.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class SqlBuilderDiagnostics
{
    /// <summary>
    /// The name of the diagnostic source.
    /// </summary>
    public const string SourceName = "EricksonLopez.SqlBuilder";

    /// <summary>
    /// Gets the primary <see cref="System.Diagnostics.ActivitySource"/> used to emit diagnostic traces.
    /// </summary>
    public static readonly ActivitySource ActivitySource = new ActivitySource(SourceName, "1.0.0.0");
    
    /// <summary>
    /// Gets the primary <see cref="System.Diagnostics.Metrics.Meter"/> used to emit diagnostic metrics.
    /// </summary>
    public static Meter Meter { get; private set; } = new Meter(SourceName, "1.0.0");

    /// <summary>
    /// Gets the counter metric for the total number of SQL queries executed.
    /// </summary>
    public static Counter<long> QueryExecutionCounter { get; private set; } = Meter.CreateCounter<long>(
        "sql_builder.query.count", 
        "queries", 
        "Total number of SQL queries executed.");

    /// <summary>
    /// Gets the histogram metric for tracking the duration of SQL queries in milliseconds.
    /// </summary>
    public static Histogram<double> QueryDurationHistogram { get; private set; } = Meter.CreateHistogram<double>(
        "sql_builder.query.duration", 
        "ms", 
        "Duration of SQL queries in milliseconds.");

    /// <summary>
    /// Gets the counter metric for the total number of slow SQL queries executed.
    /// </summary>
    public static Counter<long> SlowQueryCounter { get; private set; } = Meter.CreateCounter<long>(
        "sql_builder.query.slow.count", 
        "queries", 
        "Total number of slow SQL queries executed.");

    /// <summary>
    /// Gets the counter metric for the total number of SQL query execution errors.
    /// </summary>
    public static Counter<long> ErrorQueryCounter { get; private set; } = Meter.CreateCounter<long>(
        "sql_builder.query.error.count", 
        "errors", 
        "Total number of SQL query execution errors.");

    internal static void ReinitializeMetersForTesting()
    {
        Meter = new Meter(SourceName, "1.0.0");
        QueryExecutionCounter = Meter.CreateCounter<long>("sql_builder.query.count", "queries", "Total number of SQL queries executed.");
        QueryDurationHistogram = Meter.CreateHistogram<double>("sql_builder.query.duration", "ms", "Duration of SQL queries in milliseconds.");
        SlowQueryCounter = Meter.CreateCounter<long>("sql_builder.query.slow.count", "queries", "Total number of slow SQL queries executed.");
        ErrorQueryCounter = Meter.CreateCounter<long>("sql_builder.query.error.count", "errors", "Total number of SQL query execution errors.");
    }
    
    /// <summary>
    /// Gets or sets a value indicating whether parameter values are included in telemetry tags.
    /// </summary>
    /// <remarks>
    /// Default is <see langword="false"/> to prevent logging sensitive data (PII).
    /// </remarks>
    public static bool LogParameters { get; set; } = false;

    /// <summary>
    /// Gets or sets the optional logger factory used for slow queries and error logging.
    /// </summary>
    public static Microsoft.Extensions.Logging.ILoggerFactory? LoggerFactory { get; set; }

    /// <summary>
    /// Gets or sets the threshold in milliseconds to consider a query as slow.
    /// </summary>
    /// <remarks>
    /// Default is 500 ms.
    /// </remarks>
    public static int SlowQueryThresholdMs { get; set; } = 500;
}
