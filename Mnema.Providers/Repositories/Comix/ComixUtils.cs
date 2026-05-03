using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mnema.Models.DTOs.UI;

namespace Mnema.Providers.Comix;

public static class ComixUtils
{

    public static readonly List<FormControlOption> Genres = [
        FormControlOption.Option("Action", "6"),
        FormControlOption.Option("Adult", "87264"),
        FormControlOption.Option("Adventure", "7"),
        FormControlOption.Option("Aliens", "31"),
        FormControlOption.Option("Animals", "32"),
        FormControlOption.Option("Anthology", "93165"),
        FormControlOption.Option("Adaptation", "93167"),
        FormControlOption.Option("Award Winning", "93166"),
        FormControlOption.Option("Boys Love", "8"),
        FormControlOption.Option("Comedy", "9"),
        FormControlOption.Option("Cooking", "33"),
        FormControlOption.Option("Crime", "10"),
        FormControlOption.Option("Crossdressing", "34"),
        FormControlOption.Option("Delinquents", "35"),
        FormControlOption.Option("Demons", "36"),
        FormControlOption.Option("Doujinshi", "93168"),
        FormControlOption.Option("Drama", "11"),
        FormControlOption.Option("Ecchi", "87265"),
        FormControlOption.Option("Fantasy", "12"),
        FormControlOption.Option("Full Color", "93172"),
        FormControlOption.Option("Genderswap", "37"),
        FormControlOption.Option("Ghosts", "38"),
        FormControlOption.Option("Girls Love", "13"),
        FormControlOption.Option("Gyaru", "39"),
        FormControlOption.Option("Harem", "40"),
        FormControlOption.Option("Hentai", "87266"),
        FormControlOption.Option("Historical", "14"),
        FormControlOption.Option("Horror", "15"),
        FormControlOption.Option("Incest", "41"),
        FormControlOption.Option("Isekai", "16"),
        FormControlOption.Option("Loli", "42"),
        FormControlOption.Option("Long Strip", "93170"),
        FormControlOption.Option("Mafia", "43"),
        FormControlOption.Option("Magic", "44"),
        FormControlOption.Option("Magical Girls", "17"),
        FormControlOption.Option("Martial Arts", "45"),
        FormControlOption.Option("Mature", "87267"),
        FormControlOption.Option("Mecha", "18"),
        FormControlOption.Option("Medical", "19"),
        FormControlOption.Option("Military", "46"),
        FormControlOption.Option("Monster Girls", "47"),
        FormControlOption.Option("Monsters", "48"),
        FormControlOption.Option("Music", "49"),
        FormControlOption.Option("Mystery", "20"),
        FormControlOption.Option("Ninja", "50"),
        FormControlOption.Option("Office Workers", "51"),
        FormControlOption.Option("Oneshot", "93169"),
        FormControlOption.Option("Philosophical", "21"),
        FormControlOption.Option("Police", "52"),
        FormControlOption.Option("Post-Apocalyptic", "53"),
        FormControlOption.Option("Psychological", "22"),
        FormControlOption.Option("Reincarnation", "54"),
        FormControlOption.Option("Reverse Harem", "55"),
        FormControlOption.Option("Romance", "23"),
        FormControlOption.Option("Samurai", "56"),
        FormControlOption.Option("Sci-Fi", "24"),
        FormControlOption.Option("School Life", "57"),
        FormControlOption.Option("Shota", "58"),
        FormControlOption.Option("Slice of Life", "25"),
        FormControlOption.Option("Smut", "87268"),
        FormControlOption.Option("Sports", "26"),
        FormControlOption.Option("Superhero", "27"),
        FormControlOption.Option("Supernatural", "59"),
        FormControlOption.Option("Survival", "60"),
        FormControlOption.Option("Thriller", "28"),
        FormControlOption.Option("Time Travel", "61"),
        FormControlOption.Option("Traditional Games", "62"),
        FormControlOption.Option("Tragedy", "29"),
        FormControlOption.Option("Vampires", "63"),
        FormControlOption.Option("Video Games", "64"),
        FormControlOption.Option("Villainess", "65"),
        FormControlOption.Option("Virtual Reality", "66"),
        FormControlOption.Option("Web Comic", "93171"),
        FormControlOption.Option("Wuxia", "30"),
        FormControlOption.Option("Zombies", "67"),
        FormControlOption.Option("4-Koma", "93164"),
    ];

    private static readonly string[] Keys =
    [
        "13YDu67uDgFczo3DnuTIURqas4lfMEPADY6Jaeqky+w=", // 0  RC4 key  round 1
        "yEy7wBfBc+gsYPiQL/4Dfd0pIBZFzMwrtlRQGwMXy3Q=", // 1  mutKey   round 1
        "yrP+EVA1Dw==",                                 // 2  prefKey  round 1
        "vZ23RT7pbSlxwiygkHd1dhToIku8SNHPC6V36L4cnwM=", // 3  RC4 key  round 2
        "QX0sLahOByWLcWGnv6l98vQudWqdRI3DOXBdit9bxCE=", // 4  mutKey   round 2
        "WJwgqCmf",                                     // 5  prefKey  round 2
        "BkWI8feqSlDZKMq6awfzWlUypl88nz65KVRmpH0RWIc=", // 6  RC4 key  round 3
        "v7EIpiQQjd2BGuJzMbBA0qPWDSS+wTJRQ7uGzZ6rJKs=", // 7  mutKey   round 3
        "1SUReYlCRA==",                                 // 8  prefKey  round 3
        "RougjiFHkSKs20DZ6BWXiWwQUGZXtseZIyQWKz5eG34=", // 9  RC4 key  round 4
        "LL97cwoDoG5cw8QmhI+KSWzfW+8VehIh+inTxnVJ2ps=", // 10 mutKey   round 4
        "52iDqjzlqe8=",                                 // 11 prefKey  round 4
        "U9LRYFL2zXU4TtALIYDj+lCATRk/EJtH7/y7qYYNlh8=", // 12 RC4 key  round 5
        "e/GtffFDTvnw7LBRixAD+iGixjqTq9kIZ1m0Hj+s6fY=", // 13 mutKey   round 5
        "xb2XwHNB"                                      // 14 prefKey  round 5
    ];

    private static int[] GetKeyBytes(int index)
    {
        if (index < 0 || index >= Keys.Length)
            return Array.Empty<int>();
        try
        {
            return Convert.FromBase64String(Keys[index])
                .Select(b => (int)b)
                .ToArray();
        }
        catch
        {
            return Array.Empty<int>();
        }
    }

    private static int[] Rc4(int[] key, int[] data)
    {
        if (key.Length == 0) return data;

        var s = Enumerable.Range(0, 256).ToArray();
        int j = 0;
        for (int i = 0; i < 256; i++)
        {
            j = (j + s[i] + key[i % key.Length]) % 256;
            (s[i], s[j]) = (s[j], s[i]);
        }

        int ii = 0, jj = 0;
        var output = new int[data.Length];
        for (int k = 0; k < data.Length; k++)
        {
            ii = (ii + 1) % 256;
            jj = (jj + s[ii]) % 256;
            (s[ii], s[jj]) = (s[jj], s[ii]);
            output[k] = data[k] ^ s[(s[ii] + s[jj]) % 256];
        }
        return output;
    }

    private static int MutS(int e) => (e + 143) % 256;
    private static int MutL(int e) => ((e >> 1) | (e << 7)) & 255;
    private static int MutC(int e) => (e + 115) % 256;
    private static int MutM(int e) => e ^ 177;
    private static int MutF(int e) => (e - 188 + 256) % 256;
    private static int MutG(int e) => ((e << 2) | (e >> 6)) & 255;
    private static int MutH(int e) => (e - 42 + 256) % 256;
    private static int MutDollar(int e) => ((e << 4) | (e >> 4)) & 255;
    private static int MutB(int e) => (e - 12 + 256) % 256;
    private static int MutUnderscore(int e) => (e - 20 + 256) % 256;
    private static int MutY(int e) => ((e >> 1) | (e << 7)) & 255;
    private static int MutK(int e) => (e - 241 + 256) % 256;
    private static int GetMutKey(int[] mk, int idx) =>
        mk.Length > 0 && (idx % 32) < mk.Length ? mk[idx % 32] : 0;

    private static int[] Round1(int[] data)
    {
        var enc = Rc4(GetKeyBytes(0), data);
        var mutKey = GetKeyBytes(1);
        var prefKey = GetKeyBytes(2);
        var out_ = new List<int>();
        for (int i = 0; i < enc.Length; i++)
        {
            if (i < 7 && i < prefKey.Length) out_.Add(prefKey[i]);
            int v = enc[i] ^ GetMutKey(mutKey, i);
            v = (i % 10) switch
            {
                0 or 9 => MutC(v),
                1      => MutB(v),
                2      => MutY(v),
                3      => MutDollar(v),
                4 or 6 => MutH(v),
                5      => MutS(v),
                7      => MutK(v),
                8      => MutL(v),
                _      => v,
            };
            out_.Add(v & 255);
        }
        return out_.ToArray();
    }

    private static int[] Round2(int[] data)
    {
        var enc = Rc4(GetKeyBytes(3), data);
        var mutKey = GetKeyBytes(4);
        var prefKey = GetKeyBytes(5);
        var out_ = new List<int>();
        for (int i = 0; i < enc.Length; i++)
        {
            if (i < 6 && i < prefKey.Length) out_.Add(prefKey[i]);
            int v = enc[i] ^ GetMutKey(mutKey, i);
            v = (i % 10) switch
            {
                0 or 8 => MutC(v),
                1      => MutB(v),
                2 or 6 => MutDollar(v),
                3      => MutH(v),
                4 or 9 => MutS(v),
                5      => MutK(v),
                7      => MutUnderscore(v),
                _      => v,
            };
            out_.Add(v & 255);
        }
        return out_.ToArray();
    }

    private static int[] Round3(int[] data)
    {
        var enc = Rc4(GetKeyBytes(6), data);
        var mutKey = GetKeyBytes(7);
        var prefKey = GetKeyBytes(8);
        var out_ = new List<int>();
        for (int i = 0; i < enc.Length; i++)
        {
            if (i < 7 && i < prefKey.Length) out_.Add(prefKey[i]);
            int v = enc[i] ^ GetMutKey(mutKey, i);
            v = (i % 10) switch
            {
                0      => MutC(v),
                1      => MutF(v),
                2 or 8 => MutS(v),
                3      => MutG(v),
                4      => MutY(v),
                5      => MutM(v),
                6      => MutDollar(v),
                7      => MutK(v),
                9      => MutB(v),
                _      => v,
            };
            out_.Add(v & 255);
        }
        return out_.ToArray();
    }

    private static int[] Round4(int[] data)
    {
        var enc = Rc4(GetKeyBytes(9), data);
        var mutKey = GetKeyBytes(10);
        var prefKey = GetKeyBytes(11);
        var out_ = new List<int>();
        for (int i = 0; i < enc.Length; i++)
        {
            if (i < 8 && i < prefKey.Length) out_.Add(prefKey[i]);
            int v = enc[i] ^ GetMutKey(mutKey, i);
            v = (i % 10) switch
            {
                0      => MutB(v),
                1 or 9 => MutM(v),
                2 or 7 => MutL(v),
                3 or 5 => MutS(v),
                4 or 6 => MutUnderscore(v),
                8      => MutY(v),
                _      => v,
            };
            out_.Add(v & 255);
        }
        return out_.ToArray();
    }

    private static int[] Round5(int[] data)
    {
        var enc = Rc4(GetKeyBytes(12), data);
        var mutKey = GetKeyBytes(13);
        var prefKey = GetKeyBytes(14);
        var out_ = new List<int>();
        for (int i = 0; i < enc.Length; i++)
        {
            if (i < 6 && i < prefKey.Length) out_.Add(prefKey[i]);
            int v = enc[i] ^ GetMutKey(mutKey, i);
            v = (i % 10) switch
            {
                0      => MutUnderscore(v),
                1 or 7 => MutS(v),
                2      => MutC(v),
                3 or 5 => MutM(v),
                4      => MutB(v),
                6      => MutF(v),
                8      => MutDollar(v),
                9      => MutG(v),
                _      => v,
            };
            out_.Add(v & 255);
        }
        return out_.ToArray();
    }

    /// <summary>
    /// Generates a hash for an API request.
    /// </summary>
    /// <param name="path">API path, e.g. "/manga/some-hash/chapters"</param>
    /// <param name="bodySize">encodeURIComponent(body).length for POST, or 0 for GET</param>
    /// <param name="time">1 for GET manga requests, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() for POST</param>
    public static string GenerateHash(string path, int bodySize = 0, long time = 1)
    {
        var baseString = $"{path}:{bodySize}:{time}";

        var encoded = Uri.EscapeDataString(baseString)
            .Replace("*", "%2A")   // EscapeDataString does not encode *
            .Replace("%7e", "~")   // EscapeDataString uppercases hex; normalise ~
            .Replace("%7E", "~");

        var initialBytes = Encoding.ASCII.GetBytes(encoded)
            .Select(b => (int)b)
            .ToArray();

        var r1 = Round1(initialBytes);
        var r2 = Round2(r1);
        var r3 = Round3(r2);
        var r4 = Round4(r3);
        var r5 = Round5(r4);

        var finalBytes = r5.Select(v => (byte)v).ToArray();

        return Convert.ToBase64String(finalBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
