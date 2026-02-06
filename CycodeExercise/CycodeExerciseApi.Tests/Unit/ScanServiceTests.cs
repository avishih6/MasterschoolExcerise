using CycodeExerciseApi.Interfaces;
using CycodeExerciseApi.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CycodeExerciseApi.Tests.Unit;

public class ScanServiceTests
{
    private readonly Mock<IFileParser> _mockNpmParser;
    private readonly Mock<IVulnerabilityProvider> _mockVulnProvider;
    private readonly Mock<ILogger<ScanService>> _mockLogger;
    private readonly ScanService _scanService;

    public ScanServiceTests()
    {
        _mockNpmParser = new Mock<IFileParser>();
        _mockNpmParser.Setup(p => p.Ecosystem).Returns("npm");
        
        _mockVulnProvider = new Mock<IVulnerabilityProvider>();
        _mockLogger = new Mock<ILogger<ScanService>>();

        _scanService = new ScanService(
            new[] { _mockNpmParser.Object },
            _mockVulnProvider.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task ScanAsync_ValidRequest_ReturnsVulnerabilities()
    {
        // Arrange
        var base64Content = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
            """{"dependencies":{"test-package":"1.0.0"}}"""));
        
        _mockNpmParser
            .Setup(p => p.ParseAsync(It.IsAny<string>()))
            .ReturnsAsync(new[] { new PackageDependency("test-package", "1.0.0") });

        _mockVulnProvider
            .Setup(v => v.GetVulnerabilitiesAsync("npm", "test-package", "1.0.0"))
            .ReturnsAsync(new[] 
            { 
                new VulnerabilityInfo("Test vulnerability", "HIGH", "1.0.1", "< 1.0.1") 
            });

        // Act
        var result = await _scanService.ScanAsync("npm", base64Content);

        // Assert
        result.VulnerablePackages.Should().HaveCount(1);
        result.VulnerablePackages[0].Name.Should().Be("test-package");
        result.VulnerablePackages[0].Version.Should().Be("1.0.0");
        result.VulnerablePackages[0].Vulnerabilities.Should().HaveCount(1);
        result.VulnerablePackages[0].Vulnerabilities[0].Severity.Should().Be("HIGH");
    }

    [Fact]
    public async Task ScanAsync_NoVulnerabilities_ReturnsEmptyList()
    {
        // Arrange
        var base64Content = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
            """{"dependencies":{"safe-package":"1.0.0"}}"""));
        
        _mockNpmParser
            .Setup(p => p.ParseAsync(It.IsAny<string>()))
            .ReturnsAsync(new[] { new PackageDependency("safe-package", "1.0.0") });

        _mockVulnProvider
            .Setup(v => v.GetVulnerabilitiesAsync("npm", "safe-package", "1.0.0"))
            .ReturnsAsync(Enumerable.Empty<VulnerabilityInfo>());

        // Act
        var result = await _scanService.ScanAsync("npm", base64Content);

        // Assert
        result.VulnerablePackages.Should().BeEmpty();
    }

    [Fact]
    public async Task ScanAsync_UnsupportedEcosystem_ThrowsNotSupportedException()
    {
        // Arrange
        var base64Content = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("test"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotSupportedException>(
            () => _scanService.ScanAsync("pip", base64Content));
        
        exception.Message.Should().Contain("pip");
        exception.Message.Should().Contain("not supported");
    }

    [Fact]
    public async Task ScanAsync_InvalidBase64_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _scanService.ScanAsync("npm", "invalid!!base64"));
    }

    [Fact]
    public async Task ScanAsync_MultiplePackages_ChecksEach()
    {
        // Arrange
        var base64Content = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
            """{"dependencies":{"pkg1":"1.0.0","pkg2":"2.0.0"}}"""));
        
        _mockNpmParser
            .Setup(p => p.ParseAsync(It.IsAny<string>()))
            .ReturnsAsync(new[] 
            { 
                new PackageDependency("pkg1", "1.0.0"),
                new PackageDependency("pkg2", "2.0.0")
            });

        _mockVulnProvider
            .Setup(v => v.GetVulnerabilitiesAsync("npm", "pkg1", "1.0.0"))
            .ReturnsAsync(new[] { new VulnerabilityInfo("Vuln 1", "CRITICAL", "1.0.1", "< 1.0.1") });
        
        _mockVulnProvider
            .Setup(v => v.GetVulnerabilitiesAsync("npm", "pkg2", "2.0.0"))
            .ReturnsAsync(Enumerable.Empty<VulnerabilityInfo>());

        // Act
        var result = await _scanService.ScanAsync("npm", base64Content);

        // Assert
        result.VulnerablePackages.Should().HaveCount(1);
        result.VulnerablePackages[0].Name.Should().Be("pkg1");
        
        _mockVulnProvider.Verify(v => v.GetVulnerabilitiesAsync("npm", "pkg1", "1.0.0"), Times.Once);
        _mockVulnProvider.Verify(v => v.GetVulnerabilitiesAsync("npm", "pkg2", "2.0.0"), Times.Once);
    }

    [Fact]
    public async Task ScanAsync_EcosystemCaseInsensitive_Works()
    {
        // Arrange
        var base64Content = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
            """{"dependencies":{}}"""));
        
        _mockNpmParser
            .Setup(p => p.ParseAsync(It.IsAny<string>()))
            .ReturnsAsync(Enumerable.Empty<PackageDependency>());

        // Act - using uppercase NPM
        var result = await _scanService.ScanAsync("NPM", base64Content);

        // Assert
        result.VulnerablePackages.Should().BeEmpty();
    }
}
