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
}


