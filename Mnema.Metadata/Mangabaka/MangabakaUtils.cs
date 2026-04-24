namespace Mnema.Metadata.Mangabaka;

public static class MangabakaUtils
{

    extension(List<MangabakaTitle>? titles)
    {
        public string FindBestTitle()
        {
            if (titles == null) return string.Empty;

            var enTitle = titles
                .OrderByDescending(t => t.IsPrimary)
                .FirstOrDefault(t => t.Language == "en");
            if (enTitle != null) return enTitle.Title;

            return titles.FindBestNativeTitle();
        }

        public string FindBestNativeTitle()
        {
            if (titles == null) return string.Empty;

            var nativeTitles = titles
                .Where(t => t.Traits.Contains("native"))
                .ToList();
            if (nativeTitles.Count == 1) return nativeTitles[0].Title;

            // Romanized titles will have a longer language code (-latn appended)
            var nativeTitle = nativeTitles
                .OrderByDescending(t => t.IsPrimary)
                .ThenBy(t => t.Language.Length)
                .FirstOrDefault();
            if (nativeTitle != null) return nativeTitle.Title;

            var officialTitle = titles
                .OrderByDescending(t => t.IsPrimary)
                .FirstOrDefault(t => t.Traits.Contains("official") && !t.Traits.Contains("native"));
            if (officialTitle != null) return officialTitle.Title;

            return titles
                .OrderByDescending(t => t.IsPrimary)
                .FirstOrDefault()?.Title ?? string.Empty;
        }
    }
}
