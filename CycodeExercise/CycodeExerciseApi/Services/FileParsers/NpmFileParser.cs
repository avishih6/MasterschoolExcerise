using System.Text.Json;
using CycodeExerciseApi.Interfaces;

namespace CycodeExerciseApi.Services.FileParsers;

/// <summary>
/// Parser for npm package.json files.
/// </summary>
public class NpmFileParser : IFileParser
{
    public string Ecosystem => "npm";

    public Task<IEnumerable<PackageDependency>> ParseAsync(string fileContent)
    {
        try
        {
            using var doc = JsonDocument.Parse(fileContent);
            var root = doc.RootElement;
            
            var dependencies = new List<PackageDependency>();
            
            // Parse "dependencies" section
            if (root.TryGetProperty("dependencies", out var depsElement))
            {
                dependencies.AddRange(ParseDependencySection(depsElement));
            }
            
            // Also parse "devDependencies" if present
            if (root.TryGetProperty("devDependencies", out var devDepsElement))
            {
                dependencies.AddRange(ParseDependencySection(devDepsElement));
            }
            
            return Task.FromResult<IEnumerable<PackageDependency>>(dependencies);
        }
        catch (JsonException ex)
        {
            throw new FormatException($"Invalid package.json format: {ex.Message}", ex);
        }
    }
    
    private static IEnumerable<PackageDependency> ParseDependencySection(JsonElement element)
    {
        foreach (var prop in element.EnumerateObject())
        {
            var packageName = prop.Name;
            var version = prop.Value.GetString() ?? string.Empty;
            
            // Clean version string (remove ^, ~, etc. if present)
            version = CleanVersionString(version);
            
            if (!string.IsNullOrEmpty(version))
            {
                yield return new PackageDependency(packageName, version);
            }
        }
    }
    
    private static string CleanVersionString(string version)
    {
        // Remove common version prefixes
        // Note: Exercise says we can assume specific versions without modifiers,
        // but handling them gracefully is better
        return version.TrimStart('^', '~', '>', '<', '=', ' ');
    }
}
