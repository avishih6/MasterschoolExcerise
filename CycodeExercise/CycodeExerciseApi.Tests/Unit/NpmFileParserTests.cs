using CycodeExerciseApi.Services.FileParsers;
using FluentAssertions;

namespace CycodeExerciseApi.Tests.Unit;

public class NpmFileParserTests
{
    private readonly NpmFileParser _parser = new();

    [Fact]
    public void Ecosystem_ShouldBeNpm()
    {
        _parser.Ecosystem.Should().Be("npm");
    }

    [Fact]
    public async Task ParseAsync_ValidPackageJson_ExtractsDependencies()
    {
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

        // Act
        var result = await _parser.ParseAsync(packageJson);
        var dependencies = result.ToList();

        // Assert
        dependencies.Should().HaveCount(2);
        dependencies.Should().Contain(d => d.Name == "deep-override" && d.Version == "1.0.1");
        dependencies.Should().Contain(d => d.Name == "mustache" && d.Version == "2.1.1");
    }

    [Fact]
    public async Task ParseAsync_PackageJsonWithDevDependencies_ExtractsBoth()
    {
        // Arrange
        var packageJson = """
        {
            "name": "Test App",
            "dependencies": {
                "express": "4.18.2"
            },
            "devDependencies": {
                "jest": "29.5.0"
            }
        }
        """;

        // Act
        var result = await _parser.ParseAsync(packageJson);
        var dependencies = result.ToList();

        // Assert
        dependencies.Should().HaveCount(2);
        dependencies.Should().Contain(d => d.Name == "express" && d.Version == "4.18.2");
        dependencies.Should().Contain(d => d.Name == "jest" && d.Version == "29.5.0");
    }

    [Fact]
    public async Task ParseAsync_PackageJsonNoDependencies_ReturnsEmptyList()
    {
        // Arrange
        var packageJson = """
        {
            "name": "Empty App",
            "version": "1.0.0"
        }
        """;

        // Act
        var result = await _parser.ParseAsync(packageJson);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_PackageJsonWithVersionModifiers_StripsModifiers()
    {
        // Arrange
        var packageJson = """
        {
            "dependencies": {
                "caret-package": "^1.0.0",
                "tilde-package": "~2.0.0",
                "gt-package": ">3.0.0",
                "exact-package": "4.0.0"
            }
        }
        """;

        // Act
        var result = await _parser.ParseAsync(packageJson);
        var dependencies = result.ToList();

        // Assert
        dependencies.Should().HaveCount(4);
        dependencies.Should().Contain(d => d.Name == "caret-package" && d.Version == "1.0.0");
        dependencies.Should().Contain(d => d.Name == "tilde-package" && d.Version == "2.0.0");
        dependencies.Should().Contain(d => d.Name == "gt-package" && d.Version == "3.0.0");
        dependencies.Should().Contain(d => d.Name == "exact-package" && d.Version == "4.0.0");
    }

    [Fact]
    public async Task ParseAsync_InvalidJson_ThrowsFormatException()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act & Assert
        await Assert.ThrowsAsync<FormatException>(() => _parser.ParseAsync(invalidJson));
    }

    [Fact]
    public async Task ParseAsync_UnderscorePackage_ExtractsCorrectly()
    {
        // Arrange - from exercise example
        var packageJson = """
        {
            "name": "My Application",
            "version": "1.0.0",
            "dependencies": {
                "underscore": "1.12.2"
            }
        }
        """;

        // Act
        var result = await _parser.ParseAsync(packageJson);
        var dependencies = result.ToList();

        // Assert
        dependencies.Should().HaveCount(1);
        dependencies.Should().Contain(d => d.Name == "underscore" && d.Version == "1.12.2");
    }
}
