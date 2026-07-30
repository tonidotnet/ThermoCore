using System.Security.Cryptography;
using System.Text;

namespace ThermoCore.Persistence;

/// <summary>SHA-256 content hashing for immutable configuration versions.</summary>
public static class ContentHasher
{
    public static string Sha256Hex(string content)
    {
        ArgumentNullException.ThrowIfNull(content);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
