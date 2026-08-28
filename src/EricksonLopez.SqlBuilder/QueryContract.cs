// Copyright © Erickson Lopez. MIT License.
using System.Collections.Generic;

namespace EricksonLopez.SqlBuilder;

/// <summary>
/// Represents the verifiable shape of a compiled query.
/// </summary>
/// <param name="Fingerprint">The unique cryptographic hash identifying the structural shape of the query.</param>
/// <param name="Tables">The collection of table names referenced in the query.</param>
/// <param name="Columns">The collection of column names projected by the query.</param>
public sealed record QueryContract(
    string Fingerprint,
    IReadOnlyList<string> Tables,
    IReadOnlyList<string> Columns
);
