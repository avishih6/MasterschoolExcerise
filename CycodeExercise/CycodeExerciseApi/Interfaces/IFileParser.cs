namespace CycodeExerciseApi.Interfaces;

/// <summary>
/// Interface for parsing project definition files.
/// Implement this for each ecosystem (npm, pip, nuget, etc.)
/// </summary>
public interface IFileParser
{
    /// <summary>
    /// The ecosystem this parser supports (e.g., "npm", "pip", "nuget")
    /// </summary>
    string Ecosystem { get; }
    
    /// <summary>
    /// Parses the file content and extracts package dependencies.
    /// </summary>
    /// <param name="fileContent">The raw file content (already decoded from base64)</param>
    /// <returns>List of package name and version tuples</returns>
    Task<IEnumerable<PackageDependency>> ParseAsync(string fileContent);
}

public record PackageDependency(string Name, string Version);
