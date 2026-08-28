// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.SqlBuilder.SourceGenerators
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    internal record PropertyModel(string Name, string TypeName, bool IsValueType, bool IsReferenceType);
}
