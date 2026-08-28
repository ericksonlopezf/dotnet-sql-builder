// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;

namespace EricksonLopez.SqlBuilder.Builders.Bulk;

/// <summary>
/// Represents configuration options for native bulk insert operations.
/// </summary>
public sealed class BulkOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the database-generated identity values
    /// should be read back and populated on the inserted entities.
    /// </summary>
    /// <remarks>
    /// When <see langword="true"/>, the operation will execute an additional query to
    /// retrieve the generated identity values and fill them into the returned entities.
    /// Default is <see langword="false"/>.
    /// </remarks>
    public bool ReturnIdentities { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum number of rows to process in a single batch.
    /// </summary>
    /// <remarks>
    /// A value of <c>0</c> means process all rows in a single server round-trip.
    /// Default is <c>0</c> (single batch).
    /// </remarks>
    public int BatchSize { get; set; } = 0;

    /// <summary>
    /// Gets or sets the number of seconds for the operation to complete before it times out.
    /// </summary>
    /// <remarks>
    /// A value of <c>0</c> means no timeout. Default is <c>30</c> seconds.
    /// </remarks>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets the default <see cref="BulkOptions"/> instance with all default values.
    /// </summary>
    public static BulkOptions Default { get; } = new BulkOptions();
}


