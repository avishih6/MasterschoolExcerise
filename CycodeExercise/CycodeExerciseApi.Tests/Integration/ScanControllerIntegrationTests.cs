using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CycodeExerciseApi.Models.Requests;
using CycodeExerciseApi.Models.Responses;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CycodeExerciseApi.Tests.Integration;

/// <summary>
/// Integration tests for the Scan API endpoint.
/// These tests require the GITHUB_ACCESS_TOKEN environment variable to be set.
/// </summary>
public class ScanControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly bool _hasGitHubToken;

    public ScanControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _hasGitHubToken = !string.IsNullOrEmpty(
            Environment.GetEnvironmentVariable("GITHUB_ACCESS_TOKEN") ?? 
            Environment.GetEnvironmentVariable("GITHUB-ACCESS-TOKEN"));
        
        _client = factory.CreateClient();
    }

    private static string EncodeToBase64(string content) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(content));

    [Fact]
    public async Task Scan_MissingEcosystem_ReturnsBadRequest()
    {
        // Arrange
        var request = new { fileContent = EncodeToBase64("{}") };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/scan", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Scan_MissingFileContent_ReturnsBadRequest()
    {
        // Arrange
        var request = new { ecosystem = "npm" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/scan", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Scan_UnsupportedEcosystem_ReturnsBadRequest()
    {
        // Arrange
        var request = new ScanRequest
        {
            Ecosystem = "pip",
            FileContent = EncodeToBase64("{}")
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/scan", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Error.Should().Contain("ecosystem");
    }

    [Fact]
    public async Task Scan_InvalidBase64_ReturnsBadRequest()
    {
        // Arrange
        var request = new ScanRequest
        {
            Ecosystem = "npm",
            FileContent = "invalid!!base64"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/scan", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Error.Should().Contain("Invalid");
    }

    [Fact]
    public async Task Scan_InvalidJson_ReturnsBadRequest()
    {
        // Arrange
        var request = new ScanRequest
        {
            Ecosystem = "npm",
            FileContent = EncodeToBase64("{ invalid json }")
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/scan", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(Skip = "Requires GITHUB_ACCESS_TOKEN - run manually")]
    public async Task Scan_VulnerablePackages_ReturnsVulnerabilities()
    {
        // This is the example from the exercise
        // Arrange
        var packageJson = """
        {
            "name": "My Application",
            "version": "1.0.0",
            "dependencies": {
                "deep-override": "1.0.1",
                "mustache": "2.1.1"
            }
        }
        """;
        
        var request = new ScanRequest
        {
            Ecosystem = "npm",
            FileContent = EncodeToBase64(packageJson)
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/scan", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<ScanResponse>();
        result.Should().NotBeNull();
        result!.VulnerablePackages.Should().HaveCount(2);
        
        // Check deep-override
        var deepOverride = result.VulnerablePackages.FirstOrDefault(p => p.Name == "deep-override");
        deepOverride.Should().NotBeNull();
        deepOverride!.Version.Should().Be("1.0.1");
        deepOverride.Vulnerabilities.Should().Contain(v => v.Severity == "CRITICAL");
        
        // Check mustache
        var mustache = result.VulnerablePackages.FirstOrDefault(p => p.Name == "mustache");
        mustache.Should().NotBeNull();
        mustache!.Version.Should().Be("2.1.1");
        mustache.Vulnerabilities.Should().NotBeEmpty();
    }

    [Fact(Skip = "Requires GITHUB_ACCESS_TOKEN - run manually")]
    public async Task Scan_SafePackages_ReturnsEmptyList()
    {
        // This is the second example from the exercise
        // Arrange
        var packageJson = """
        {
            "name": "My Application",
            "version": "1.0.0",
            "dependencies": {
                "underscore": "1.12.2"
            }
        }
        """;
        
        var request = new ScanRequest
        {
            Ecosystem = "npm",
            FileContent = EncodeToBase64(packageJson)
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/scan", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<ScanResponse>();
        result.Should().NotBeNull();
        result!.VulnerablePackages.Should().BeEmpty();
    }

    [Fact(Skip = "Requires GITHUB_ACCESS_TOKEN - run manually")]
    public async Task Scan_EmptyDependencies_ReturnsEmptyList()
    {
        // Arrange
        var packageJson = """
        {
            "name": "Empty App",
            "version": "1.0.0",
            "dependencies": {}
        }
        """;
        
        var request = new ScanRequest
        {
            Ecosystem = "npm",
            FileContent = EncodeToBase64(packageJson)
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/scan", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<ScanResponse>();
        result.Should().NotBeNull();
        result!.VulnerablePackages.Should().BeEmpty();
    }
}
