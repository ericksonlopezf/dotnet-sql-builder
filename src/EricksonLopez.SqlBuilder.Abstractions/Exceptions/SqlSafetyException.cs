// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.SqlBuilder.Abstractions.Exceptions;

/// <summary>
/// Exception thrown when a query violates safety guardrails (e.g. unconstrained mutations or injection attempts).
/// </summary>
public class SqlSafetyException : SqlBuilderException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqlSafetyException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public SqlSafetyException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlSafetyException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public SqlSafetyException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
