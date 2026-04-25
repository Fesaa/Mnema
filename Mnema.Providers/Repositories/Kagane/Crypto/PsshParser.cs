/*
 * The code in the crypto namespace has been adapted from https://github.com/keiyoushi/extensions-source
 * for use in C#. Massive thank you to the developers there.
 *
 * Modified by Amelia, 2026. Original code by keiyoushi
 */
using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace Mnema.Providers.Kagane.Crypto;

public sealed class ProtectionSystemHeaderBox
{
    public string Type { get; init; } = default!;
    public int Version { get; init; }
    public int Flags { get; init; }
    public Guid SystemId { get; init; }
    public IReadOnlyList<Guid>? KeyIds { get; init; }
    public byte[] InitData { get; init; } = default!;
}

public sealed class RawBox
{
    public string Type { get; init; } = default!;
    public byte[] Data { get; init; } = default!;
}

public sealed class Pssh
{
    public int Offset { get; init; }
    public string Type { get; init; } = default!;
    public object Content { get; init; } = default!; // ProtectionSystemHeaderBox | RawBox
    public int End { get; init; }
}

public static class PsshParser
{
    public static Pssh Parse(byte[] data)
    {
        var span = data.AsSpan();
        var pos = 0;

        var size = BinaryPrimitives.ReadInt32BigEndian(span[pos..]); pos += 4;
        var type = System.Text.Encoding.ASCII.GetString(span[pos..(pos + 4)]); pos += 4;

        object content;
        if (type == "pssh")
        {
            (content, pos) = ParsePsshBox(type, span, pos);
        }
        else
        {
            var remaining = span[pos..].ToArray();
            pos += remaining.Length;
            content = new RawBox { Type = type, Data = remaining };
        }

        return new Pssh { Offset = 0, Type = type, Content = content, End = pos };
    }

    private static (ProtectionSystemHeaderBox box, int pos) ParsePsshBox(string type, ReadOnlySpan<byte> span, int pos)
    {
        var version = span[pos++];

        var flags = ((span[pos] & 0xFF) << 16) |
                    ((span[pos + 1] & 0xFF) << 8) |
                    (span[pos + 2] & 0xFF);
        pos += 3;

        var systemIdBytes = span[pos..(pos + 16)].ToArray(); pos += 16;
        var systemId = ToGuid(systemIdBytes);

        IReadOnlyList<Guid>? keyIds = null;
        if (version == 1)
        {
            var count = BinaryPrimitives.ReadInt32BigEndian(span[pos..]); pos += 4;
            var ids = new Guid[count];
            for (int i = 0; i < count; i++)
            {
                ids[i] = ToGuid(span[pos..(pos + 16)].ToArray());
                pos += 16;
            }
            keyIds = ids;
        }

        var initDataSize = BinaryPrimitives.ReadInt32BigEndian(span[pos..]); pos += 4;
        var initData = span[pos..(pos + initDataSize)].ToArray(); pos += initDataSize;

        return (new ProtectionSystemHeaderBox
        {
            Type = type,
            Version = version,
            Flags = flags,
            SystemId = systemId,
            KeyIds = keyIds,
            InitData = initData,
        }, pos);
    }

    /// <summary>
    /// Converts 16 bytes (big-endian UUID) to a .NET Guid.
    /// Mirrors Java's ByteBuffer.order(BIG_ENDIAN).getLong() x2 → UUID.
    /// </summary>
    private static Guid ToGuid(byte[] bytes)
    {
        if (bytes.Length != 16) throw new ArgumentException("UUID must be 16 bytes");
        // .NET Guid layout is mixed-endian; we want a straight big-endian UUID string round-trip.
        // Easiest: format as UUID string and parse.
        var hex = Convert.ToHexString(bytes);
        return Guid.Parse($"{hex[..8]}-{hex[8..12]}-{hex[12..16]}-{hex[16..20]}-{hex[20..]}");
    }
}
