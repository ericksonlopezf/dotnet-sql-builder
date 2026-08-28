// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;

namespace EricksonLopez.SqlBuilder.SourceGenerators;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
internal sealed record SqlEntityPropertyModel(
    string Name,
    string TypeName,
    bool IsInsertable,
    bool IsIndexed,
    bool IsPrimaryKey,
    string ReaderMethod,
    string CastType);
