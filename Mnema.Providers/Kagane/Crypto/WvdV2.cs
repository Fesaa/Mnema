/*
 * The code in the crypto namespace has been adapted from https://github.com/keiyoushi/extensions-source
 * for use in C#. Massive thank you to the developers there.
 *
 * Modified by Amelia, 2026. Original code by keiyoushi
 */
using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace Mnema.Providers.Kagane.Crypto;

public enum DeviceType
{
    Chrome = 1,
    Android = 2,
}

/// <summary>
/// Parsed representation of a .wvd (Widevine Device) v2 file.
/// </summary>
public sealed class WvdV2
{
    public byte Version { get; init; }
    public DeviceType Type { get; init; }
    public byte[] PrivateKey { get; init; } = default!;
    public byte[] ClientId { get; init; } = default!;

    private const string Magic = "WVD";

    public static WvdV2 ParseStream(byte[] stream)
    {
        var span = stream.AsSpan();
        var pos = 0;

        // 3-byte magic
        var sig = Encoding.ASCII.GetString(span[..Magic.Length]); pos += Magic.Length;
        if (sig != Magic)
            throw new InvalidDataException($"Bad magic: expected 'WVD', got '{sig}'");

        var version = span[pos++];
        if (version != 2)
            throw new InvalidDataException($"Unsupported version: {version}");

        var typeValue = span[pos++];
        if (!Enum.IsDefined(typeof(DeviceType), (int)typeValue))
            throw new InvalidDataException($"Unknown device type: {typeValue}");
        var deviceType = (DeviceType)(int)typeValue;

        var _securityLevel = span[pos++]; // consumed but not stored
        var flagsByte = span[pos++];
        byte? flags = flagsByte != 0 ? flagsByte : null;

        var privateKeyLen = BinaryPrimitives.ReadInt16BigEndian(span[pos..]); pos += 2;
        var privateKey = span[pos..(pos + privateKeyLen)].ToArray(); pos += privateKeyLen;

        var clientIdLen = BinaryPrimitives.ReadInt16BigEndian(span[pos..]); pos += 2;
        var clientId = span[pos..(pos + clientIdLen)].ToArray();

        return new WvdV2
        {
            Version = version,
            Type = deviceType,
            PrivateKey = privateKey,
            ClientId = clientId,
        };
    }
}
