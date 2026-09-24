// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.SqlBuilder.Abstractions.Exceptions;

/// <summary>
/// Represents the base exception type for all errors raised within the SqlBuilder ecosystem.
/// </summary>
public class SqlBuilderException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqlBuilderException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public SqlBuilderException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlBuilderException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public SqlBuilderException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
