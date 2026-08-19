namespace Mnema.Models.Enums;

public enum ImportErrorType
{
    UnknownDirectory = 0,
    GenericException = 1,
    MixedContentFormats = 2,
    FailedToParseContentFormat = 3,
    FailedToParseSeries = 4,
}
