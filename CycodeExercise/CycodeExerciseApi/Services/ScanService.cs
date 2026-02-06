using System.Text;
using CycodeExerciseApi.Interfaces;
using CycodeExerciseApi.Models.Responses;

namespace CycodeExerciseApi.Services;

/// <summary>
/// Main scanning service that orchestrates file parsing and vulnerability checking.
/// </summary>
public class ScanService : IScanService
{
    private readonly IEnumerable<IFileParser> _fileParsers;
    private readonly IVulnerabilityProvider _vulnerabilityProvider;
    private readonly ILogger<ScanService> _logger;

    public ScanService(
        IEnumerable<IFileParser> fileParsers,
        IVulnerabilityProvider vulnerabilityProvider,
        ILogger<ScanService> logger)
    {
        _fileParsers = fileParsers;
        _vulnerabilityProvider = vulnerabilityProvider;
        _logger = logger;
    }

    public async Task<ScanResponse> ScanAsync(string ecosystem, string fileContent)
    {
        // 1. Find the appropriate parser for this ecosystem
        var parser = _fileParsers.FirstOrDefault(p => 
            p.Ecosystem.Equals(ecosystem, StringComparison.OrdinalIgnoreCase));
        
        if (parser == null)
        {
            throw new NotSupportedException($"Ecosystem '{ecosystem}' is not supported. Supported ecosystems: {string.Join(", ", _fileParsers.Select(p => p.Ecosystem))}");
        }

        // 2. Decode base64 content
        string decodedContent;
        try
        {
            var bytes = Convert.FromBase64String(fileContent);
            decodedContent = Encoding.UTF8.GetString(bytes);
        }
        catch (FormatException ex)
        {
            throw new ArgumentException("Invalid base64 encoding in fileContent", ex);
        }

        // 3. Parse the file to extract dependencies
        var dependencies = await parser.ParseAsync(decodedContent);
        var dependencyList = dependencies.ToList();
        
        _logger.LogInformation("Found {Count} dependencies in {Ecosystem} project file", 
            dependencyList.Count, ecosystem);

        // 4. Check each dependency for vulnerabilities
        var vulnerablePackages = new List<VulnerablePackage>();
        
        foreach (var dep in dependencyList)
        {
            var vulnerabilities = await _vulnerabilityProvider.GetVulnerabilitiesAsync(
                ecosystem, dep.Name, dep.Version);
            
            var vulnList = vulnerabilities.ToList();
            
            if (vulnList.Count > 0)
            {
                _logger.LogInformation("Package {Package}@{Version} has {Count} vulnerabilities",
                    dep.Name, dep.Version, vulnList.Count);
                
                vulnerablePackages.Add(new VulnerablePackage
                {
                    Name = dep.Name,
                    Version = dep.Version,
                    Vulnerabilities = vulnList.Select(v => new Vulnerability
                    {
                        Summary = v.Summary,
                        Severity = v.Severity,
                        FirstPatchedVersion = v.FirstPatchedVersion
                    }).ToList()
                });
            }
        }

        return new ScanResponse { VulnerablePackages = vulnerablePackages };
    }
}
