// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
namespace EricksonLopez.SqlBuilder.Abstractions;

/// <summary>
/// Defines a node within the SQL Abstract Syntax Tree (AST).
/// </summary>
public interface ISqlNode
{
    /// <summary>
    /// Accepts a visitor to perform double-dispatch traversal of the AST.
    /// </summary>
    /// <param name="visitor">The visitor instance handling the node.</param>
    void Accept(ISqlVisitor visitor);

    /// <summary>
    /// Contributes the structure of this node to a deterministic cryptographic fingerprint.
    /// Values and parameter arguments are ignored to ensure the fingerprint only reflects the AST shape.
    /// </summary>
    /// <param name="fingerprinter">The fingerprinter instance.</param>
    void ContributeToFingerprint(IQueryFingerprinter fingerprinter)
    {
        fingerprinter.Contribute(GetType().Name);
    }
}





