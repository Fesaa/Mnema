using System.Text.Json.Serialization;

namespace Mnema.Metadata.Hardcover;

public class HardcoverGetSeriesInfoByIdResponse
{
    [JsonPropertyName("series_by_pk")]
    public HardcoverSeries Series { get; set; }
}
