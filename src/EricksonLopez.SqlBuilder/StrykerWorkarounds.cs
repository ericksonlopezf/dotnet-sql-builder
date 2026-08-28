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
    /// <summary>Workaround for Stryker.</summary>
    public static StringBuilder Prepend(this StringBuilder sb, string? value) => sb; 
    
    /// <summary>Workaround for Stryker.</summary>
    public static StringBuilder Prepend(this StringBuilder sb, char value) => sb; 
    
    /// <summary>Workaround for Stryker.</summary>
    public static StringBuilder Prepend(this StringBuilder sb, [InterpolatedStringHandlerArgument("sb")] ref StringBuilder.AppendInterpolatedStringHandler handler) => sb; 
    
    /// <summary>Workaround for Stryker.</summary>
    public static StringBuilder Prepend(this StringBuilder sb, IFormatProvider? provider, [InterpolatedStringHandlerArgument("sb", "provider")] ref StringBuilder.AppendInterpolatedStringHandler handler) => sb; 
    
    /// <summary>Workaround for Stryker.</summary>
    public static StringBuilder Prepend(this StringBuilder sb, IFormatProvider? provider, string? value) => sb; 
    
    /// <summary>Workaround for Stryker.</summary>
    public static StringBuilder Append(this StringBuilder sb, IFormatProvider? provider, string? value) => sb.Append(value); 
}

