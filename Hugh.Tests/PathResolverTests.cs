using Hugh.Configuration;

namespace Hugh.Tests;

/// <summary>
/// Unit tests for the PathResolver utility class.
/// </summary>
public class PathResolverTests : IDisposable
{
    private readonly string? _originalEnvValue;

    public PathResolverTests()
    {
        // Store original environment variable value
        _originalEnvValue = Environment.GetEnvironmentVariable("HUGH_CONFIG_PATH");
        // Reset cached path before each test
        PathResolver.ResetCachedPath();
    }

    public void Dispose()
    {
        // Restore original environment variable
        if (_originalEnvValue != null)
        {
            Environment.SetEnvironmentVariable("HUGH_CONFIG_PATH", _originalEnvValue);
        }
        else
        {
            Environment.SetEnvironmentVariable("HUGH_CONFIG_PATH", null);
        }
        // Reset cached path after each test
        PathResolver.ResetCachedPath();
    }

    [Fact]
    public void GetInstallationPath_WithValidEnvVariable_ReturnsEnvPath()
    {
        // Arrange - Use current directory which should have appsettings.json (or AppContext.BaseDirectory)
        var testPath = AppContext.BaseDirectory;
        
        // Ensure appsettings.json exists for the test
        var configFile = Path.Combine(testPath, "appsettings.json");
        var configExists = File.Exists(configFile);
        var createdTempConfig = false;
        
        if (!configExists)
        {
            File.WriteAllText(configFile, "{}");
            createdTempConfig = true;
        }
        
        try
        {
            Environment.SetEnvironmentVariable("HUGH_CONFIG_PATH", testPath);
            PathResolver.ResetCachedPath();

            // Act
            var result = PathResolver.GetInstallationPath();

            // Assert
            Assert.Equal(testPath, result);
        }
        finally
        {
            if (createdTempConfig)
            {
                File.Delete(configFile);
            }
        }
    }

    [Fact]
    public void GetInstallationPath_WithInvalidEnvVariable_FallsBack()
    {
        // Arrange - Set env variable to non-existent path
        var invalidPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Environment.SetEnvironmentVariable("HUGH_CONFIG_PATH", invalidPath);
        PathResolver.ResetCachedPath();

        // Act
        var result = PathResolver.GetInstallationPath();

        // Assert - Should not be the invalid path
        Assert.NotEqual(invalidPath, result);
    }

    [Fact]
    public void GetInstallationPath_CachesResult()
    {
        // Arrange
        Environment.SetEnvironmentVariable("HUGH_CONFIG_PATH", null);
        PathResolver.ResetCachedPath();

        // Act
        var result1 = PathResolver.GetInstallationPath();
        var result2 = PathResolver.GetInstallationPath();

        // Assert - Both calls should return the same cached result
        Assert.Equal(result1, result2);
    }

    [Fact]
    public void GetInstallationPath_FallsBackToCWD_WhenNoAppsettingsInAssemblyPath()
    {
        // Arrange - Clear env variable
        Environment.SetEnvironmentVariable("HUGH_CONFIG_PATH", null);
        PathResolver.ResetCachedPath();

        // Act
        var result = PathResolver.GetInstallationPath();

        // Assert - Result should be a valid directory
        Assert.True(Directory.Exists(result));
    }

    [Fact]
    public void GetUserDataPath_ReturnsPathInUserProfile()
    {
        // Act
        var result = PathResolver.GetUserDataPath();

        // Assert
        var expectedParent = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        Assert.StartsWith(expectedParent, result);
        Assert.EndsWith(".hugh", result);
        Assert.True(Directory.Exists(result));
    }

    [Fact]
    public void GetUserDataPath_CreatesDirectoryIfNotExists()
    {
        // Act
        var result = PathResolver.GetUserDataPath();

        // Assert
        Assert.True(Directory.Exists(result));
    }

    [Fact]
    public void GetLogPath_ReturnsLogsSubdirectory()
    {
        // Act
        var result = PathResolver.GetLogPath();

        // Assert
        Assert.EndsWith("logs", result);
        Assert.Contains(".hugh", result);
        Assert.True(Directory.Exists(result));
    }

    [Fact]
    public void GetLogPath_CreatesDirectoryIfNotExists()
    {
        // Act
        var result = PathResolver.GetLogPath();

        // Assert
        Assert.True(Directory.Exists(result));
    }

    [Fact]
    public void GetInstallationPath_UsesAssemblyLocationWhenAppsettingsExists()
    {
        // Arrange - Clear env variable so it's not used
        Environment.SetEnvironmentVariable("HUGH_CONFIG_PATH", null);
        PathResolver.ResetCachedPath();
        
        var assemblyPath = AppContext.BaseDirectory;
        var configFile = Path.Combine(assemblyPath, "appsettings.json");
        var configExists = File.Exists(configFile);
        var createdTempConfig = false;
        
        if (!configExists)
        {
            File.WriteAllText(configFile, "{}");
            createdTempConfig = true;
        }
        
        try
        {
            // Act
            var result = PathResolver.GetInstallationPath();

            // Assert - Should use assembly location since appsettings.json exists there
            Assert.Equal(assemblyPath, result);
        }
        finally
        {
            if (createdTempConfig)
            {
                File.Delete(configFile);
            }
        }
    }
}
