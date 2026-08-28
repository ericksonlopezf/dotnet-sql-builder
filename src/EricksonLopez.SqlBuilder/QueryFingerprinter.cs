// Copyright © Erickson Lopez. MIT License.
using System;
using System.Security.Cryptography;
using System.Text;
using EricksonLopez.SqlBuilder.Abstractions;

namespace EricksonLopez.SqlBuilder;

internal sealed class QueryFingerprinter : IQueryFingerprinter, IDisposable
{
    private readonly IncrementalHash _hasher;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryFingerprinter"/> class.
    /// </summary>
    public QueryFingerprinter()
    {
        _hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    }

    /// <summary>
    /// Contributes the specified string value to the cryptographic hash.
    /// </summary>
    /// <param name="value">The string value to include in the fingerprint calculation.</param>
    public void Contribute(string? value)
    {
        if (value == null)
        {
            _hasher.AppendData(new[] { (byte)0 });
            return;
        }
        
        var bytes = Encoding.UTF8.GetBytes(value);
        _hasher.AppendData(BitConverter.GetBytes(bytes.Length));
        _hasher.AppendData(bytes);
    }

    /// <summary>
    /// Contributes the specified integer value to the cryptographic hash.
    /// </summary>
    /// <param name="value">The integer value to include in the fingerprint calculation.</param>
    public void Contribute(int value)
    {
        _hasher.AppendData(BitConverter.GetBytes(value));
    }

    /// <summary>
    /// Contributes the specified boolean value to the cryptographic hash.
    /// </summary>
    /// <param name="value">The boolean value to include in the fingerprint calculation.</param>
    public void Contribute(bool value)
    {
        _hasher.AppendData(new[] { value ? (byte)1 : (byte)0 });
    }

    /// <summary>
    /// Contributes the specified type's full name to the cryptographic hash.
    /// </summary>
    /// <param name="type">The type to include in the fingerprint calculation.</param>
    public void Contribute(Type? type)
    {
        Contribute(type?.FullName);
    }

    /// <summary>
    /// Computes the final cryptographic hash representing the query's structural shape and resets the hasher state.
    /// </summary>
    /// <returns>A lower-case hexadecimal string representation of the computed hash.</returns>
    public string GetFingerprint()
    {
        var hashBytes = _hasher.GetHashAndReset();
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Releases the cryptographic resources used by this instance.
    /// </summary>
    public void Dispose()
    {
        _hasher.Dispose();
    }
}
