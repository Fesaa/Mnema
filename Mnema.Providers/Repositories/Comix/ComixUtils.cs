using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mnema.Models.DTOs.UI;

namespace Mnema.Providers.Comix;

public static class ComixUtils
{

    public static readonly List<FormControlOption> ContentRating =
    [
        FormControlOption.Option("Safe", "safe"),
        FormControlOption.Option("Suggestive", "suggestive"),
        FormControlOption.Option("Erotica", "erotica"),
        FormControlOption.Option("Pornographic", "pornographic"),
    ];

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
        "JxTcdyiA5GZxnbrmthXBQfU2IMTKcY1+3nNhbq98Sgo=", // 0  RC4 key  round 1
        "3PordjODbhqla382Cxapmo/1JiABJQcjiJj1+48gTJ4=", // 1  mutKey   round 1
        "OaKvnI5ARA==",                                 // 2  prefKey  round 1
        "MHNBHYWA7lvy867fXgvGcJwWDk79KqUJUVFsh3RwnnI=", // 3  RC4 key  round 2
        "8i0Cru/VJBSVB2Y1GcMDVpzx2WepOcfnWdd81yxICl4=", // 4  mutKey   round 2
        "Fyskubz8VvA=",                                 // 5  prefKey  round 2
        "B46L1x+UeWP+19cRpQ+OZvdLAK9EHID8g3mSgn57tew=", // 6  RC4 key  round 3
        "DTSTmUt6LpDUw9r1lSQqyb3YlFTzruT8tk8wUGkwehQ=", // 7  mutKey   round 3
        "vY/meeI=",                                     // 8  prefKey  round 3
        "7xWfIF5THL5LAnRgAARg+4mjWHPU9n3PQwvzbaMNi+Q=", // 9  RC4 key  round 4
        "bewtiTuV+HJk56xxkf2iCljLgruCpBmN9BgE8i6gc9M=", // 10 mutKey   round 4
        "/Xcb2zAu8AU=",                                 // 11 prefKey  round 4
        "WgeCQ3T8R51uTwVSiVa7Zy0dN6JOg6Z5JleMS+HV8Aw=", // 12 RC4 key  round 5
        "yXayUVFrrcW56jQCEfZzuCidjpnWKjTDUNT7XeX9i7k=", // 13 mutKey   round 5
        "tSLco2w=",                                     // 14 prefKey  round 5
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

    private static int GetMutKey(int[] mk, int idx) =>
        mk.Length > 0 && (idx % 32) < mk.Length ? mk[idx % 32] : 0;

    private static int ShiftRight7Left1(int e) => ((e >> 7) | (e << 1)) & 255;
    private static int ShiftLeft1Right7(int e)  => ((e << 1) | (e >> 7)) & 255;
    private static int ShiftRight2Left6(int e)  => ((e >> 2) | (e << 6)) & 255;
    private static int ShiftLeft4Right4(int e)  => ((e << 4) | (e >> 4)) & 255;
    private static int ShiftRight4Left4(int e)  => ((e >> 4) | (e << 4)) & 255;

    private static int[] Mutate(int[] data, int[] mutKey, int[] prefKey, int prefKeyLimit, int round)
    {
        var out_ = new List<int>();
        for (int o = 0; o < data.Length; o++)
        {
            // Insert prefKey byte BEFORE the data byte (matches Kotlin)
            if (o < prefKeyLimit && o < prefKey.Length)
                out_.Add(prefKey[o]);

            int n = data[o] ^ GetMutKey(mutKey, o);
            n = round switch
            {
                1 => (o % 10) switch
                {
                    0      => ShiftRight7Left1(n),
                    1      => n ^ 37,
                    2      => n ^ 81,
                    3      => n ^ 147,
                    4      => ShiftRight2Left6(n),
                    5 or 8 => ShiftRight4Left4(n),
                    6      => n ^ 218,
                    7      => (n + 159) & 255,
                    9      => n ^ 180,
                    _      => n,
                },
                2 => (o % 10) switch
                {
                    0 or 9 => n ^ 180,
                    1      => ShiftLeft1Right7(n),
                    2      => n ^ 147,
                    3      => ShiftRight7Left1(n),
                    4      => ShiftRight2Left6(n),
                    5      => ShiftRight4Left4(n),
                    6 or 8 => (n + 159) & 255,
                    7      => (n + 34) & 255,
                    _      => n,
                },
                3 => (o % 10) switch
                {
                    0      => n ^ 81,
                    1      => ShiftRight4Left4(n),
                    2 or 9 => ShiftLeft4Right4(n),
                    3      => n ^ 37,
                    4      => (n + 159) & 255,
                    5      => ShiftLeft1Right7(n),
                    6      => n ^ 180,
                    7      => (n + 34) & 255,
                    8      => ShiftRight2Left6(n),
                    _      => n,
                },
                4 => (o % 10) switch
                {
                    0 or 7 => n ^ 218,
                    1 or 4 => ShiftLeft1Right7(n),
                    2      => ShiftRight7Left1(n),
                    3      => (n + 159) & 255,
                    5 or 8 => n ^ 180,
                    6      => n ^ 147,
                    9      => n ^ 37,
                    _      => n,
                },
                5 => (o % 10) switch
                {
                    0      => ShiftLeft4Right4(n),
                    1 or 3 => n ^ 147,
                    2      => (n + 34) & 255,
                    4 or 9 => n ^ 218,
                    5 or 7 => ShiftLeft1Right7(n),
                    6      => n ^ 180,
                    8      => ShiftRight2Left6(n),
                    _      => n,
                },
                _ => n,
            };
            out_.Add(n & 255);
        }
        return out_.ToArray();
    }

    private static int[] Round1(int[] data)
    {
        var mut = Mutate(data, GetKeyBytes(1), GetKeyBytes(2), 7, 1);
        return Rc4(GetKeyBytes(0), mut);
    }

    private static int[] Round2(int[] data)
    {
        var mut = Mutate(data, GetKeyBytes(4), GetKeyBytes(5), 8, 2);
        return Rc4(GetKeyBytes(3), mut);
    }

    private static int[] Round3(int[] data)
    {
        var mut = Mutate(data, GetKeyBytes(7), GetKeyBytes(8), 5, 3);
        return Rc4(GetKeyBytes(6), mut);
    }

    private static int[] Round4(int[] data)
    {
        var mut = Mutate(data, GetKeyBytes(10), GetKeyBytes(11), 8, 4);
        return Rc4(GetKeyBytes(9), mut);
    }

    private static int[] Round5(int[] data)
    {
        var mut = Mutate(data, GetKeyBytes(13), GetKeyBytes(14), 5, 5);
        return Rc4(GetKeyBytes(12), mut);
    }

    /// <summary>
    /// Generates a hash for an API request.
    /// </summary>
    /// <param name="path">API path, e.g. "/manga/some-hash/chapters"</param>
    public static string GenerateHash(string path)
    {
        var encoded = Uri.EscapeDataString(path)
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
