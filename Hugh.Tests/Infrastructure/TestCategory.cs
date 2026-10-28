namespace Hugh.Tests.Infrastructure;

/// <summary>
/// Defines standard test trait category constants for organizing and filtering tests.
/// Usage: [Trait("Category", TestCategory.Unit)]
/// </summary>
public static class TestCategory
{
    /// <summary>
    /// Fast, isolated tests with no external dependencies.
    /// </summary>
    public const string Unit = "Unit";

    /// <summary>
    /// Tests verifying component interactions with mocked dependencies.
    /// </summary>
    public const string Integration = "Integration";

    /// <summary>
    /// Quick validation that critical paths work end-to-end.
    /// </summary>
    public const string Smoke = "Smoke";

    /// <summary>
    /// Tests requiring a live GitHub Copilot SDK connection.
    /// Automatically skipped when SDK is unavailable.
    /// </summary>
    public const string LiveSdk = "LiveSdk";

    /// <summary>
    /// Full end-to-end workflow tests spanning multiple phases.
    /// </summary>
    public const string E2E = "E2E";
}
