/*
 * The code in the crypto namespace has been adapted from https://github.com/keiyoushi/extensions-source
 * for use in C#. Massive thank you to the developers there.
 *
 * Modified by Amelia, 2026. Original code by keiyoushi
 */
using System;
using System.Buffers.Binary;
using System.Linq;
using System.Security.Cryptography;
using Google.Protobuf;

namespace Mnema.Providers.Kagane.Crypto;

public sealed class Cdm
{
    private DeviceType DeviceType { get; }
    private ClientIdentification ClientId { get; }
    private RSA RsaKey { get; }

    private Cdm(DeviceType deviceType, ClientIdentification clientId, RSA rsaKey)
    {
        DeviceType = deviceType;
        ClientId = clientId;
        RsaKey = rsaKey;
    }

    /// <summary>
    /// Builds a base64-encoded Widevine license challenge for the given PSSH box.
    /// </summary>
    public string GetLicenseChallenge(
        ProtectionSystemHeaderBox pssh,
        LicenseType licenseType = LicenseType.Streaming)
    {
        byte[] requestId;

        if (DeviceType == DeviceType.Android)
        {
            // 4 random bytes + 4 zero bytes + 8-byte little-endian session counter (hardcoded 1)
            var randomBytes = CryptoHelpers.GenerateRandomBytes(4);
            const int sessionNumber = 1;
            var counterBytes = new byte[8];
            for (var i = 0; i < counterBytes.Length; i++)
                counterBytes[i] = (byte)((sessionNumber >> (i * 8)) & 0xFF);

            requestId = Concat(randomBytes, new byte[4], counterBytes);
            requestId = System.Text.Encoding.ASCII.GetBytes(requestId.ToHexString().ToUpperInvariant());
        }
        else
        {
            requestId = CryptoHelpers.GenerateRandomBytes(16);
        }

        var psshData = new LicenseRequest.Types.ContentIdentification.Types.WidevinePsshData
        {
            LicenseType = licenseType,
            RequestId = ByteString.CopyFrom(requestId),
        };
        psshData.PsshData.Add(ByteString.CopyFrom(pssh.InitData));

        var licenseRequest = new LicenseRequest
        {
            ClientId = ClientId,
            ContentId = new LicenseRequest.Types.ContentIdentification
            {
                WidevinePsshData = psshData,
            },
            Type = LicenseRequest.Types.RequestType.New,
            RequestTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ProtocolVersion = ProtocolVersion.Version21,
            KeyControlNonce = (uint)Random.Shared.NextInt64(1, uint.MaxValue + 1L),
        };

        var licenseRequestBytes = licenseRequest.ToByteArray();

        var signed = new SignedMessage
        {
            Type = SignedMessage.Types.MessageType.LicenseRequest,
            Msg = ByteString.CopyFrom(licenseRequestBytes),
            Signature = ByteString.CopyFrom(CryptoHelpers.Sign(licenseRequestBytes, RsaKey)),
        };

        return signed.ToByteArray().ToBase64();
    }

    /// <summary>
    /// Constructs a Cdm from a base64-encoded .wvd string.
    /// </summary>
    public static Cdm FromData(string wvdBase64)
    {
        var parsed = WvdV2.ParseStream(CryptoHelpers.DecodeBase64(wvdBase64));
        var clientId = ClientIdentification.Parser.ParseFrom(parsed.ClientId);
        var rsaKey = CryptoHelpers.GetKey(parsed.PrivateKey);

        return new Cdm(parsed.Type, clientId, rsaKey);
    }

    private static byte[] Concat(params byte[][] arrays)
    {
        var result = new byte[arrays.Sum(a => a.Length)];
        var offset = 0;
        foreach (var a in arrays)
        {
            Buffer.BlockCopy(a, 0, result, offset, a.Length);
            offset += a.Length;
        }
        return result;
    }

    public static byte[] GetPssh(byte[] t)
    {
        var e = Convert.FromBase64String("7e+LqXnWSs6jyCfc1R0h7Q==");
        var zeroes = new byte[4];

        // i = [0x12, t.Length, ...t]
        var i = new byte[] { 0x12, (byte)t.Length }.Concat(t).ToArray();

        var s = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(s, i.Length);

        var innerBox = Concat(zeroes, e, s, i);

        var outerSize = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(outerSize, innerBox.Length + 8);

        var psshHeader = "pssh"u8.ToArray();

        return Concat(outerSize, psshHeader, innerBox);
    }
}
