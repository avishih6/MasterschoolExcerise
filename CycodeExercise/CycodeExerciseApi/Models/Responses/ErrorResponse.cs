using System.Text.Json.Serialization;

namespace CycodeExerciseApi.Models.Responses;

public class ErrorResponse
{
    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;
    
    [JsonPropertyName("details")]
    public string? Details { get; set; }
}
