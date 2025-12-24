using Flurl;
using Mnema.Common;

namespace Mnema.Providers.Extensions;

internal static class QueryExtensions
{

    extension(string s)
    {
        internal Url AddIncludes()
        {
            return s.SetQueryParam("includes[]", "cover_art")
                .AppendQueryParam("includes[]", "author")
                .AppendQueryParam("includes[]", "artist");
        }
    }

    extension(Url url)
    {

        internal Url SetQueryParamIf(bool condition, string key, string value)
        {
            return condition ? url.SetQueryParam(key, value) : url;
        }
        
        internal Url AddIncludes()
        {
            return url.SetQueryParam("includes[]", "cover_art")
                .AppendQueryParam("includes[]", "author")
                .AppendQueryParam("includes[]", "artist");
        }

        internal Url AddAllContentRatings()
        {
            return url.SetQueryParam("contentRating[]", "pornographic")
                .AppendQueryParam("contentRating[]", "erotica")
                .AppendQueryParam("contentRating[]", "suggestive")
                .AppendQueryParam("contentRating[]", "safe");
        }

        internal Url AddPagination(PaginationParams pagination)
            => url.AddPagination(pagination.PageSize, pagination.PageNumber * pagination.PageSize);

        internal Url AddPagination(int pageSize, int offSet)
        {
            return url
                .SetQueryParam("offset", offSet)
                .SetQueryParam("limit", pageSize);
        }
    }
    
    
}