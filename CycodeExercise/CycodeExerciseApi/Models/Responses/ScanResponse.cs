using System.Text.Json.Serialization;

namespace CycodeExerciseApi.Models.Responses;

public class ScanResponse
{
    [JsonPropertyName("vulnerablePackages")]
    public List<VulnerablePackage> VulnerablePackages { get; set; } = new();
}

public class VulnerablePackage
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;
    
    [JsonPropertyName("vulnerabilities")]
    public List<Vulnerability> Vulnerabilities { get; set; } = new();
}

public class Vulnerability
{
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;
    
    [JsonPropertyName("severity")]
    public string Severity { get; set; } = string.Empty;
    
    [JsonPropertyName("firstPatchedVersion")]
    public string? FirstPatchedVersion { get; set; }
}
