// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using System;
using EricksonLopez.SqlBuilder.Abstractions;

namespace EricksonLopez.SqlBuilder;

/// <summary>
/// Provides extension methods for calculating deterministic query fingerprints.
/// </summary>
public static class SqlQueryFingerprintExtensions
{
    /// <summary>
    /// Computes a deterministic cryptographic fingerprint of the query's Abstract Syntax Tree (AST).
    /// </summary>
    /// <param name="query">The SQL query.</param>
    /// <returns>A SHA256 hexadecimal string representing the query structure.</returns>
    public static string GetFingerprint(this ISqlQuery query)
    {
        if (query is not IAstQuery astQuery)
        {
            // For non-AST queries (like RawQuery strings), we just hash the type name.
            using var rawHasher = new QueryFingerprinter();
            rawHasher.Contribute(query.GetType());
            return rawHasher.GetFingerprint();
        }

        using var fingerprinter = new QueryFingerprinter();
        fingerprinter.Contribute(query.GetType());
        
        foreach (var node in astQuery.Nodes)
        {
            node.ContributeToFingerprint(fingerprinter);
        }
        
        return fingerprinter.GetFingerprint();
    }
}




