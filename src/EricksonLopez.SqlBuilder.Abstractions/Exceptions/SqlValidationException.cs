// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.SqlBuilder.Abstractions.Exceptions
{
    /// <summary>
    /// Represents the exception thrown when a SQL query Abstract Syntax Tree fails structural validation prior to compilation.
    /// </summary>
    public class SqlValidationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SqlValidationException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public SqlValidationException(string message) : base(message)
        {
        }
    }
}

