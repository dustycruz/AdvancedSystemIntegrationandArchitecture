using System.Security.Cryptography;
using System.Text;

namespace Store.Messaging;

/// <summary>Creates message ids for events.</summary>
public static class MessageIds
{
    /// <summary>A new random message id, for events that start a flow (for example OrderPlaced).</summary>
    public static Guid New() => Guid.NewGuid();

    /// <summary>
    /// A deterministic id (name-based UUID, version 5) for an event produced while handling
    /// <paramref name="causationMessageId"/>. Handling the same message twice yields the same id,
    /// so the downstream idempotency check drops the duplicate.
    /// </summary>
    public static Guid Derive(Guid causationMessageId, string purpose)
    {
        Span<byte> namespaceBytes = stackalloc byte[16];
        causationMessageId.TryWriteBytes(namespaceBytes, bigEndian: true, out _);

        var nameBytes = Encoding.UTF8.GetBytes(purpose);
        var input = new byte[16 + nameBytes.Length];
        namespaceBytes.CopyTo(input);
        nameBytes.CopyTo(input, 16);

        var hash = SHA1.HashData(input);
        hash[6] = (byte)((hash[6] & 0x0F) | 0x50); // version 5
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80); // RFC 4122 variant

        return new Guid(hash.AsSpan(0, 16), bigEndian: true);
    }
}
