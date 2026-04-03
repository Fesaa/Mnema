namespace Mnema.Metadata.Mangabaka;

public static class MangabakaUtils
{

    extension(List<MangabakaTitle>? titles)
    {
        public string FindBestTitle()
        {
            if (titles == null) return string.Empty;

            var enTitle = titles.FirstOrDefault(t => t.Language == "en");
            if (enTitle != null) return enTitle.Title;

            return titles.FindBestNativeTitle();
        }

        public string FindBestNativeTitle()
        {
            if (titles == null) return string.Empty;

            var nativeTitle = titles.FirstOrDefault(t => t.Traits.Contains("native"));
            if (nativeTitle != null) return nativeTitle.Title;

            var officialTitle = titles.FirstOrDefault(t => t.Traits.Contains("official") && !t.Traits.Contains("native"));
            if (officialTitle != null) return officialTitle.Title;

            return titles.OrderBy(t => t.IsPrimary).FirstOrDefault()?.Title ?? string.Empty;
        }
    }
}
