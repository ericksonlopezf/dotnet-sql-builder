// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;

namespace EricksonLopez.SqlBuilder.Abstractions;

/// <summary>
/// Represents the final, immutable SQL result ready to be consumed by Dapper or another execution engine.
/// </summary>
/// <param name="Sql">The raw SQL command text.</param>
/// <param name="Parameters">A read-only dictionary of parameters associated with the SQL command.</param>
public sealed record SqlResult(string Sql, IReadOnlyDictionary<string, object?> Parameters);


