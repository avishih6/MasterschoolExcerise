using CycodeExerciseApi.Models.Responses;

namespace CycodeExerciseApi.Interfaces;

/// <summary>
/// Main scanning service interface.
/// Orchestrates file parsing and vulnerability checking.
/// </summary>
public interface IScanService
{
    /// <summary>
    /// Scans a project file for vulnerable packages.
    /// </summary>
    /// <param name="ecosystem">The ecosystem (npm, pip, nuget)</param>
    /// <param name="fileContent">Base64-encoded file content</param>
    /// <returns>Scan result with vulnerable packages</returns>
    Task<ScanResponse> ScanAsync(string ecosystem, string fileContent);
}
