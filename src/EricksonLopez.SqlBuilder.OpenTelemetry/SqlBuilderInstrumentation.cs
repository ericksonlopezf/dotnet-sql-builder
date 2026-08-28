// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics;
using EricksonLopez.SqlBuilder.Abstractions;
using OpenTelemetry.Trace;

namespace EricksonLopez.SqlBuilder.OpenTelemetry
{
    /// <summary>
    /// Provides OpenTelemetry instrumentation support for SQL query tracing.
    /// </summary>
    public static class SqlBuilderInstrumentation
    {
        /// <summary>
        /// The name of the <see cref="System.Diagnostics.ActivitySource"/> used to emit SQL query traces.
        /// </summary>
        public const string ActivitySourceName = "EricksonLopez.SqlBuilder";
        private static readonly ActivitySource ActivitySource = new ActivitySource(ActivitySourceName);

        /// <summary>
        /// Starts an OpenTelemetry <see cref="Activity"/> representing the execution of the specified SQL query.
        /// </summary>
        /// <param name="query">The SQL query being executed.</param>
        /// <param name="databaseName">The name of the target database. Defaults to <c>"Unknown"</c> when not provided.</param>
        /// <param name="compiler">The optional SQL compiler to deduce the dialect-specific <c>db.system</c> semantic tag.</param>
        /// <returns>
        /// The started <see cref="Activity"/> if there is an active listener for the source;
        /// otherwise, <see langword="null"/>.
        /// </returns>
        public static Activity? StartQueryActivity(ISqlQuery query, string databaseName = "Unknown", ISqlCompiler? compiler = null)
        {
            var activity = ActivitySource.StartActivity("SQL Query", ActivityKind.Client);
            if (activity != null)
            {
                activity.SetTag("db.system", ResolveDbSystem(compiler));
                activity.SetTag("db.name", databaseName);
                if (query.Tag != null)
                {
                    activity.SetTag("db.query.tag", query.Tag);
                }
            }
            return activity;
        }

        /// <summary>
        /// Resolves the OpenTelemetry standard <c>db.system</c> attribute from the compiler implementation.
        /// </summary>
        /// <param name="compiler">The SQL compiler instance.</param>
        /// <returns>The standardized database system identifier (e.g., <c>"mssql"</c>, <c>"postgresql"</c>, <c>"mysql"</c>, <c>"sqlite"</c>, <c>"oracle"</c>).</returns>
        public static string ResolveDbSystem(ISqlCompiler? compiler)
        {
            if (compiler == null)
            {
                return "sql";
            }

            var typeName = compiler.GetType().Name;
            if (typeName.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                return "mssql";
            }

            if (typeName.Contains("PostgreSql", StringComparison.OrdinalIgnoreCase))
            {
                return "postgresql";
            }

            if (typeName.Contains("MySql", StringComparison.OrdinalIgnoreCase))
            {
                return "mysql";
            }

            if (typeName.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                return "sqlite";
            }

            if (typeName.Contains("Oracle", StringComparison.OrdinalIgnoreCase))
            {
                return "oracle";
            }

            return "sql";
        }
    }
}

