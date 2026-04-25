/*
 * The code in the crypto namespace has been adapted from https://github.com/keiyoushi/extensions-source
 * for use in C#. Massive thank you to the developers there.
 *
 * Modified by Amelia, 2026. Original code by keiyoushi
 */
using System;
using System.Security.Cryptography;

namespace Mnema.Providers.Kagane.Crypto;

public static class CryptoHelpers
{
    public static byte[] DecodeBase64(string input) => Convert.FromBase64String(input);

    extension(byte[] bytes)
    {
        public string ToBase64() => Convert.ToBase64String(bytes);
        public string ToHexString() => Convert.ToHexString(bytes).ToUpperInvariant();
    }

    public static byte[] GenerateRandomBytes(int size)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);

        var bytes = new byte[size];
        RandomNumberGenerator.Fill(bytes);
        return bytes;
    }

    /// <summary>
    /// Signs a message using RSA-PSS with SHA-1 / MGF1-SHA1, salt length 20.
    /// </summary>
    public static byte[] Sign(byte[] message, RSA privateKey)
    {
        return privateKey.SignData(
            message,
            HashAlgorithmName.SHA1,
            RSASignaturePadding.Pss
        );
    }

    /// <summary>
    /// Loads an RSA private key from either PKCS#8 or PKCS#1 DER bytes.
    /// </summary>
    public static RSA GetKey(byte[] bytes)
    {
        var rsa = RSA.Create();
        try
        {
            rsa.ImportPkcs8PrivateKey(bytes, out _);
            return rsa;
        }
        catch (CryptographicException)
        {
            // Fall back: wrap PKCS#1 inside a PKCS#8 envelope, same as the Kotlin buildPkcs8KeyFromPkcs1Key
            var wrapped = BuildPkcs8FromPkcs1(bytes);
            rsa.ImportPkcs8PrivateKey(wrapped, out _);
            return rsa;
        }
    }

    private static byte[] BuildPkcs8FromPkcs1(byte[] innerKey)
    {
        // The 26-byte PKCS#8 header for RSA (OID 1.2.840.113549.1.1.1, no params)
        var header = Convert.FromBase64String("MIIEvAIBADANBgkqhkiG9w0BAQEFAASCBKY=");
        var result = new byte[innerKey.Length + 26];
        Array.Copy(header, 0, result, 0, 26);

        // Patch total length at bytes 2-3
        var totalLen = BitConverter.GetBytes((short)(result.Length - 4));
        if (BitConverter.IsLittleEndian) Array.Reverse(totalLen);
        Array.Copy(totalLen, 0, result, 2, 2);

        // Patch inner key length at bytes 24-25
        var innerLen = BitConverter.GetBytes((short)innerKey.Length);
        if (BitConverter.IsLittleEndian) Array.Reverse(innerLen);
        Array.Copy(innerLen, 0, result, 24, 2);

        Array.Copy(innerKey, 0, result, 26, innerKey.Length);
        return result;
    }
}
