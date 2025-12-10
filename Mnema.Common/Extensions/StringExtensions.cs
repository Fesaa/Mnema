namespace Mnema.Common.Extensions;

public static class StringExtensions
{

    extension(string? s)
    {

        public string OrNonEmpty(params string?[] other)
        {
            if (!string.IsNullOrEmpty(s)) return s;

            foreach (var s1 in other)
            {
                if (!string.IsNullOrEmpty(s1)) return s1;
            }

            return string.Empty;
        }
        
    }
    
}