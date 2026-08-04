using System.Text.Json.Serialization;

namespace Mnema.Models.DTOs.UI;

public class FormFieldValidationRequestDto<T>
{
    public T FormValue { get; set; }
}

public class FormFieldValidationRequestDto<T, T2> : FormFieldValidationRequestDto<T>
{
    [JsonPropertyName("siblingValues")]
    public T2? GroupValue { get; set; }
}
