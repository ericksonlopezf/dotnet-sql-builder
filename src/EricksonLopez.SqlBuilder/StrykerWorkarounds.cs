// Copyright © Erickson Lopez. MIT License.
using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace EricksonLopez.SqlBuilder;

/// <summary>
/// Workarounds to avoid false positives during Stryker mutation testing.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
internal static class StrykerWorkarounds 
{ 
    /// <summary>
    /// Prepends the specified string value to the string builder for testing compatibility.
    /// </summary>
    /// <param name="sb">The target string builder.</param>
    /// <param name="value">The string value to prepend.</param>
    /// <returns>The string builder instance.</returns>
    public static StringBuilder Prepend(this StringBuilder sb, string? value) => sb; 
    
    /// <summary>
    /// Prepends the specified character to the string builder for testing compatibility.
    /// </summary>
    /// <param name="sb">The target string builder.</param>
    /// <param name="value">The character to prepend.</param>
    /// <returns>The string builder instance.</returns>
    public static StringBuilder Prepend(this StringBuilder sb, char value) => sb; 
    
    /// <summary>
    /// Prepends the interpolated string handler content to the string builder for testing compatibility.
    /// </summary>
    /// <param name="sb">The target string builder.</param>
    /// <param name="handler">The interpolated string handler.</param>
    /// <returns>The string builder instance.</returns>
    public static StringBuilder Prepend(this StringBuilder sb, [InterpolatedStringHandlerArgument("sb")] ref StringBuilder.AppendInterpolatedStringHandler handler) => sb; 
    
    /// <summary>
    /// Prepends formatted interpolated content to the string builder for testing compatibility.
    /// </summary>
    /// <param name="sb">The target string builder.</param>
    /// <param name="provider">The format provider to apply.</param>
    /// <param name="handler">The interpolated string handler.</param>
    /// <returns>The string builder instance.</returns>
    public static StringBuilder Prepend(this StringBuilder sb, IFormatProvider? provider, [InterpolatedStringHandlerArgument("sb", "provider")] ref StringBuilder.AppendInterpolatedStringHandler handler) => sb; 
    
    /// <summary>
    /// Prepends formatted string content to the string builder for testing compatibility.
    /// </summary>
    /// <param name="sb">The target string builder.</param>
    /// <param name="provider">The format provider to apply.</param>
    /// <param name="value">The string value to prepend.</param>
    /// <returns>The string builder instance.</returns>
    public static StringBuilder Prepend(this StringBuilder sb, IFormatProvider? provider, string? value) => sb; 
    
    /// <summary>
    /// Appends formatted string content to the string builder for testing compatibility.
    /// </summary>
    /// <param name="sb">The target string builder.</param>
    /// <param name="provider">The format provider to apply.</param>
    /// <param name="value">The string value to append.</param>
    /// <returns>The string builder instance.</returns>
    public static StringBuilder Append(this StringBuilder sb, IFormatProvider? provider, string? value) => sb.Append(value); 
}

