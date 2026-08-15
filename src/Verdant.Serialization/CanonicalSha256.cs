using System.Security.Cryptography;

namespace Verdant.Serialization;

public static class CanonicalSha256
{
    public static CanonicalDigest Compute(CanonicalValue projection)
    {
        ArgumentNullException.ThrowIfNull(projection);
        var bytes = CanonicalJson.SerializeToUtf8(projection);
        var digest = Convert.ToHexStringLower(SHA256.HashData(bytes));
        return new CanonicalDigest(bytes, digest);
    }
}

public sealed record CanonicalDigest
{
    internal CanonicalDigest(byte[] utf8Bytes, string hexadecimal)
    {
        ArgumentNullException.ThrowIfNull(utf8Bytes);
        ArgumentException.ThrowIfNullOrWhiteSpace(hexadecimal);
        Utf8Bytes = System.Array.AsReadOnly(utf8Bytes.ToArray());
        Hexadecimal = hexadecimal;
    }

    public IReadOnlyList<byte> Utf8Bytes { get; }
    public string Hexadecimal { get; }
}