using Hugh.Prompts;

namespace Hugh.Tests.Prompts;

/// <summary>
/// Unit tests for BuiltInPromptLoader.
/// Tests that all built-in prompts load, caching works, and errors are handled.
/// </summary>
public class BuiltInPromptLoaderTests : IDisposable
{
    public BuiltInPromptLoaderTests()
    {
        // Clear cache before each test to ensure isolation
        BuiltInPromptLoader.ClearCache();
    }

    public void Dispose()
    {
        BuiltInPromptLoader.ClearCache();
    }

    #region Load Tests

    [Fact]
    public void Load_TriagePrompt_ReturnsContent()
    {
        var content = BuiltInPromptLoader.Load("triage");

        Assert.NotNull(content);
        Assert.NotEmpty(content);
    }

    [Fact]
    public void Load_ClarificationPrompt_ReturnsContent()
    {
        var content = BuiltInPromptLoader.Load("clarification");

        Assert.NotNull(content);
        Assert.NotEmpty(content);
    }

    [Fact]
    public void Load_DecompositionPrompt_ReturnsContent()
    {
        var content = BuiltInPromptLoader.Load("decomposition");

        Assert.NotNull(content);
        Assert.NotEmpty(content);
    }

    [Fact]
    public void Load_TriagePrompt_ContainsClassificationKeyword()
    {
        var content = BuiltInPromptLoader.Load("triage");

        Assert.Contains("Classification", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_ClarificationPrompt_ContainsVisionKeyword()
    {
        var content = BuiltInPromptLoader.Load("clarification");

        Assert.Contains("vision", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_DecompositionPrompt_ContainsReleaseKeyword()
    {
        var content = BuiltInPromptLoader.Load("decomposition");

        Assert.Contains("release", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_NonExistentPrompt_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() =>
            BuiltInPromptLoader.Load("nonexistent-prompt"));
    }

    [Fact]
    public void Load_SamePromptTwice_ReturnsSameContent()
    {
        var first = BuiltInPromptLoader.Load("triage");
        var second = BuiltInPromptLoader.Load("triage");

        Assert.Equal(first, second);
    }

    #endregion

    #region Exists Tests

    [Fact]
    public void Exists_TriagePrompt_ReturnsTrue()
    {
        Assert.True(BuiltInPromptLoader.Exists("triage"));
    }

    [Fact]
    public void Exists_ClarificationPrompt_ReturnsTrue()
    {
        Assert.True(BuiltInPromptLoader.Exists("clarification"));
    }

    [Fact]
    public void Exists_DecompositionPrompt_ReturnsTrue()
    {
        Assert.True(BuiltInPromptLoader.Exists("decomposition"));
    }

    [Fact]
    public void Exists_NonExistentPrompt_ReturnsFalse()
    {
        Assert.False(BuiltInPromptLoader.Exists("does-not-exist"));
    }

    #endregion

    #region GetAvailablePrompts Tests

    [Fact]
    public void GetAvailablePrompts_IncludesTriage()
    {
        var prompts = BuiltInPromptLoader.GetAvailablePrompts().ToList();

        Assert.Contains("triage", prompts);
    }

    [Fact]
    public void GetAvailablePrompts_IncludesClarification()
    {
        var prompts = BuiltInPromptLoader.GetAvailablePrompts().ToList();

        Assert.Contains("clarification", prompts);
    }

    [Fact]
    public void GetAvailablePrompts_IncludesDecomposition()
    {
        var prompts = BuiltInPromptLoader.GetAvailablePrompts().ToList();

        Assert.Contains("decomposition", prompts);
    }

    [Fact]
    public void GetAvailablePrompts_ContainsAtLeastThreePrompts()
    {
        var prompts = BuiltInPromptLoader.GetAvailablePrompts().ToList();

        Assert.True(prompts.Count >= 3,
            $"Expected at least 3 built-in prompts, found {prompts.Count}: {string.Join(", ", prompts)}");
    }

    #endregion

    #region ClearCache Tests

    [Fact]
    public void ClearCache_AfterLoad_NextLoadStillWorks()
    {
        var first = BuiltInPromptLoader.Load("triage");
        BuiltInPromptLoader.ClearCache();
        var second = BuiltInPromptLoader.Load("triage");

        Assert.Equal(first, second);
    }

    #endregion
}
