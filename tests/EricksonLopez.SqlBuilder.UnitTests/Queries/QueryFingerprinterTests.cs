// Copyright © Erickson Lopez. MIT License.
using System;
using System.Security.Cryptography;
using System.Text;
using AwesomeAssertions;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

public class QueryFingerprinterTests
{
    private static string Hash(params byte[][] data)
    {
        using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var b in data) hasher.AppendData(b);
        return Convert.ToHexString(hasher.GetHashAndReset()).ToLowerInvariant();
    }

    [Fact]
    public void Contribute_String_Null_AppendsZeroByte()
    {
        using var fp = new QueryFingerprinter();
        fp.Contribute((string?)null);
        fp.GetFingerprint().Should().Be(Hash(new[] { (byte)0 }));
    }

    [Fact]
    public void Contribute_String_NonNull_AppendsLengthAndBytes()
    {
        using var fp = new QueryFingerprinter();
        fp.Contribute("test");
        var bytes = Encoding.UTF8.GetBytes("test");
        fp.GetFingerprint().Should().Be(Hash(BitConverter.GetBytes(bytes.Length), bytes));
    }

    [Fact]
    public void Contribute_Int_AppendsBytes()
    {
        using var fp = new QueryFingerprinter();
        fp.Contribute(123);
        fp.GetFingerprint().Should().Be(Hash(BitConverter.GetBytes(123)));
    }

    [Fact]
    public void Contribute_Bool_AppendsOneOrZero()
    {
        using var fp1 = new QueryFingerprinter();
        fp1.Contribute(true);
        fp1.GetFingerprint().Should().Be(Hash(new[] { (byte)1 }));

        using var fp2 = new QueryFingerprinter();
        fp2.Contribute(false);
        fp2.GetFingerprint().Should().Be(Hash(new[] { (byte)0 }));
    }

    [Fact]
    public void Contribute_Type_AppendsFullName()
    {
        using var fp1 = new QueryFingerprinter();
        fp1.Contribute(typeof(string));
        var bytes1 = Encoding.UTF8.GetBytes(typeof(string).FullName!);
        fp1.GetFingerprint().Should().Be(Hash(BitConverter.GetBytes(bytes1.Length), bytes1));

        using var fp2 = new QueryFingerprinter();
        fp2.Contribute((Type?)null);
        fp2.GetFingerprint().Should().Be(Hash(new[] { (byte)0 }));
    }
    
    [Fact]
    public void Dispose_CallsUnderlyingDispose()
    {
        var fp = new QueryFingerprinter();
        fp.Dispose();
        Action act = () => fp.GetFingerprint();
        act.Should().Throw<ObjectDisposedException>();
    }
}


