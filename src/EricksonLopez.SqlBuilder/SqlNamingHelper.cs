// Copyright © Erickson Lopez. MIT License.
using System;
using System.Text;

namespace EricksonLopez.SqlBuilder;

internal static class SqlNamingHelper
{
    /// <summary>
    /// Converts a string to its snake_case representation.
    /// </summary>
    /// <param name="input">The string to convert.</param>
    /// <returns>The snake_case converted string.</returns>
    public static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        int extraSpaces = 0;
        for (int i = 1; i < input.Length; i++)
        {
            if (char.IsUpper(input[i]))
            {
                extraSpaces++;
            }
        }
        
        if (extraSpaces == 0)
        {
            return input.ToLowerInvariant();
        }

        return string.Create(input.Length + extraSpaces, input, (span, state) =>
        {
            int spanIndex = 0;
            for (int i = 0; i < state.Length; i++)
            {
                char c = state[i];
                if (i > 0 && char.IsUpper(c))
                {
                    span[spanIndex++] = '_';
                }
                span[spanIndex++] = char.ToLowerInvariant(c);
            }
        });
    }

    private static readonly System.Collections.Generic.HashSet<string> AllowedOperators = new(System.StringComparer.OrdinalIgnoreCase)
    {
        "=", "<>", "!=", "<", ">", "<=", ">=", "LIKE", "ILIKE", "NOT LIKE", "NOT ILIKE", "IS", "IS NOT"
    };

    private static readonly System.Collections.Generic.HashSet<string> ReservedKeywords = new(System.StringComparer.OrdinalIgnoreCase)
    {
        "all", "analyse", "analyze", "and", "any", "array", "as", "asc", "asymmetric", "authorization",
        "binary", "both", "case", "cast", "check", "collate", "collation", "column", "concurrently",
        "constraint", "create", "cross", "current_catalog", "current_date", "current_role", "current_schema",
        "current_time", "current_timestamp", "current_user", "default", "deferrable", "desc", "distinct",
        "do", "else", "end", "except", "false", "fetch", "for", "foreign", "freeze", "from", "full",
        "grant", "group", "having", "ilike", "in", "initially", "inner", "intersect", "into", "is",
        "isnull", "join", "lateral", "leading", "left", "like", "limit", "localtime", "localtimestamp",
        "natural", "not", "notnull", "null", "offset", "on", "only", "or", "order", "outer", "overlaps",
        "placing", "primary", "references", "returning", "right", "select", "session_user", "similar",
        "some", "symmetric", "table", "tablesample", "then", "to", "trailing", "true", "union",
        "unique", "user", "using", "variadic", "verbose", "when", "where", "window", "with"
    };

    /// <summary>
    /// Determines whether the specified identifier is a reserved SQL or PostgreSQL keyword.
    /// </summary>
    /// <param name="identifier">The identifier to inspect.</param>
    /// <returns><see langword="true"/> if the identifier is a reserved keyword; otherwise, <see langword="false"/>.</returns>
    public static bool IsReservedKeyword(string identifier) => !string.IsNullOrEmpty(identifier) && ReservedKeywords.Contains(identifier);

    /// <summary>
    /// Validates that an identifier does not contain illegal characters or SQL injection tokens.
    /// </summary>
    /// <param name="identifier">The identifier to validate.</param>
    /// <param name="paramName">The name of the parameter associated with the identifier.</param>
    /// <exception cref="ArgumentException"><paramref name="identifier"/> is null, whitespace, or contains invalid characters</exception>
    public static void ValidateIdentifier(string identifier, string paramName)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            throw new ArgumentException("Identifier cannot be null or whitespace.", paramName);

        for (int i = 0; i < identifier.Length; i++)
        {
            char c = identifier[i];
            if (char.IsWhiteSpace(c) || c is ';' or '\'' or '"' or '`' or '/' or '\\' or '-' or '\0' or '(' or ')' or '=')
            {
                throw new ArgumentException($"Identifier '{identifier}' contains invalid characters.", paramName);
            }
        }
    }

    /// <summary>
    /// Validates that an operator matches an approved SQL comparison operator.
    /// </summary>
    /// <param name="op">The SQL operator string to validate.</param>
    /// <param name="paramName">The name of the parameter associated with the operator.</param>
    /// <exception cref="ArgumentException"><paramref name="op"/> is null, whitespace, or unsupported</exception>
    public static void ValidateOperator(string op, string paramName)
    {
        if (string.IsNullOrWhiteSpace(op))
            throw new ArgumentException("Operator cannot be null or whitespace.", paramName);

        if (!AllowedOperators.Contains(op.Trim()))
            throw new ArgumentException($"Operator '{op}' is not supported or contains invalid characters.", paramName);
    }
}


