// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.SqlBuilder.Abstractions;

/// <summary>
/// Defines a contract for contributing Abstract Syntax Tree (AST) components to a deterministic query fingerprint.
/// </summary>
public interface IQueryFingerprinter
{
    /// <summary>
    /// Contributes a string component to the query fingerprint.
    /// </summary>
    /// <param name="value">The string value to hash.</param>
    void Contribute(string? value);

    /// <summary>
    /// Contributes a numeric value to the query fingerprint.
    /// </summary>
    void Contribute(int value);

    /// <summary>
    /// Contributes a boolean value to the query fingerprint.
    /// </summary>
    void Contribute(bool value);

    /// <summary>
    /// Contributes the type name to the query fingerprint.
    /// </summary>
    void Contribute(Type? type);
}
