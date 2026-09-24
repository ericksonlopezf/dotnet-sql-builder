// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.SqlBuilder.Abstractions.Exceptions;

/// <summary>
/// Exception thrown when an abstract syntax tree contains conflicting, incomplete, or unsupported SQL constructs.
/// </summary>
public class SqlSyntaxException : SqlBuilderException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqlSyntaxException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public SqlSyntaxException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlSyntaxException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public SqlSyntaxException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
