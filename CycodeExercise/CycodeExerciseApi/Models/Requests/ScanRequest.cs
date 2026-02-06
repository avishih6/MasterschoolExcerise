using System.ComponentModel.DataAnnotations;

namespace CycodeExerciseApi.Models.Requests;

public class ScanRequest
{
    /// <summary>
    /// Name of the ecosystem (e.g., npm, nuget, pip)
    /// </summary>
    [Required(ErrorMessage = "Ecosystem is required")]
    public string Ecosystem { get; set; } = string.Empty;
    
    /// <summary>
    /// Base64-encoded project definition file content
    /// </summary>
    [Required(ErrorMessage = "FileContent is required")]
    public string FileContent { get; set; } = string.Empty;
}
