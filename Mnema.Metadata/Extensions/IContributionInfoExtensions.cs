using Mnema.Metadata.Hardcover.Generated;
using Mnema.Models.Publication;

namespace Mnema.Metadata.Extensions;

public static class IContributionInfoExtensions
{

    public const string Author = nameof(Author);
    public const string Illustrator = nameof(Illustrator);
    public const string Translator = nameof(Translator);
    public const string Letterer = nameof(Letterer);

    extension(IContributionInfo info)
    {
        public PersonRole? Role
        {
            get
            {
                if (info.Contribution is null ||
                    string.Equals(info.Contribution, Author, StringComparison.OrdinalIgnoreCase))
                    return PersonRole.Writer;

                if (string.Equals(info.Contribution, Illustrator, StringComparison.OrdinalIgnoreCase))
                    return PersonRole.Colorist;

                if (string.Equals(info.Contribution, Translator, StringComparison.OrdinalIgnoreCase))
                    return PersonRole.Translator;

                if (string.Equals(info.Contribution, Letterer, StringComparison.OrdinalIgnoreCase))
                    return PersonRole.Letterer;

                return null;
            }
        }
    }
}
