using System.Collections.Concurrent;

namespace Hugh.Prompts;

/// <summary>
/// Loads built-in Hugh prompts from embedded assembly resources.
/// These prompts are compiled into the Hugh binary and are always available
/// without file system dependencies.
/// </summary>
public static class BuiltInPromptLoader
{
    private static readonly ConcurrentDictionary<string, string> Cache = new();

    /// <summary>
    /// Loads a built-in prompt by name.
    /// </summary>
    /// <param name="promptName">The prompt name (e.g., "triage", "clarification", "decomposition").</param>
    /// <returns>The prompt content.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the prompt is not found in the assembly.</exception>
    public static string Load(string promptName)
    {
        return Cache.GetOrAdd(promptName, name =>
        {
            var resourceName = $"Hugh.Prompts.BuiltIn.{name}.md";
            using var stream = typeof(BuiltInPromptLoader).Assembly
                .GetManifestResourceStream(resourceName);

            if (stream == null)
            {
                // List available resources for diagnostics
                var available = typeof(BuiltInPromptLoader).Assembly
                    .GetManifestResourceNames();
                throw new InvalidOperationException(
                    $"Built-in prompt not found: {resourceName}. " +
                    $"Available resources: {string.Join(", ", available)}");
            }

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        });
    }

    /// <summary>
    /// Checks if a built-in prompt exists.
    /// </summary>
    /// <param name="promptName">The prompt name to check.</param>
    /// <returns>True if the prompt exists as an embedded resource.</returns>
    public static bool Exists(string promptName)
    {
        var resourceName = $"Hugh.Prompts.BuiltIn.{promptName}.md";
        return typeof(BuiltInPromptLoader).Assembly
            .GetManifestResourceStream(resourceName) != null;
    }

    /// <summary>
    /// Gets the names of all available built-in prompts.
    /// </summary>
    public static IEnumerable<string> GetAvailablePrompts()
    {
        var prefix = "Hugh.Prompts.BuiltIn.";
        var suffix = ".md";

        return typeof(BuiltInPromptLoader).Assembly
            .GetManifestResourceNames()
            .Where(name => name.StartsWith(prefix) && name.EndsWith(suffix))
            .Select(name => name[prefix.Length..^suffix.Length]);
    }

    /// <summary>
    /// Clears the prompt cache. Primarily for testing.
    /// </summary>
    internal static void ClearCache()
    {
        Cache.Clear();
    }
}
