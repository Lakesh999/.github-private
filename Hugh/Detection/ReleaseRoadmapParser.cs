using System.Text.RegularExpressions;

namespace Hugh.Detection;

/// <summary>
/// Represents a release parsed from a roadmap document.
/// </summary>
public record ParsedRelease
{
    /// <summary>
    /// The 1-based release number.
    /// </summary>
    public int Number { get; init; }

    /// <summary>
    /// The release name (e.g., "MVP — Core Task Management").
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    /// The goal of this release.
    /// </summary>
    public string Goal { get; init; } = "";

    /// <summary>
    /// Bulleted scope items for this release.
    /// </summary>
    public List<string> Scope { get; init; } = new();

    /// <summary>
    /// Release numbers this release depends on.
    /// </summary>
    public List<int> Dependencies { get; init; } = new();
}

/// <summary>
/// Parses release roadmap documents produced by the decomposition agent.
/// Extracts structured release definitions from the markdown format.
/// </summary>
public partial class ReleaseRoadmapParser
{
    // Matches release headers: ### Release 1: MVP — Core Task Management
    // or: ### Release 2: Collaboration Features
    [GeneratedRegex(
        @"###\s+Release\s+(\d+)\s*:\s*(.+?)(?:\r?\n|$)",
        RegexOptions.Compiled | RegexOptions.Multiline)]
    private static partial Regex ReleaseHeaderRegex();

    // Matches goal lines: **Goal:** {text}
    [GeneratedRegex(
        @"\*\*Goal:\*\*\s*(.+?)(?:\r?\n|$)",
        RegexOptions.Compiled)]
    private static partial Regex GoalRegex();

    // Matches scope bullet items: - {text}
    [GeneratedRegex(
        @"^-\s+(.+?)$",
        RegexOptions.Compiled | RegexOptions.Multiline)]
    private static partial Regex ScopeBulletRegex();

    // Matches dependencies: **Dependencies:** Release 1 (...)
    [GeneratedRegex(
        @"\*\*Dependencies?:\*\*\s*(.+?)(?:\r?\n|$)",
        RegexOptions.Compiled)]
    private static partial Regex DependenciesRegex();

    // Extracts release numbers from dependency text: "Release 1", "Release 1, Release 2"
    [GeneratedRegex(
        @"Release\s+(\d+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex DependencyNumberRegex();

    /// <summary>
    /// Parses all releases from a roadmap document.
    /// </summary>
    /// <param name="roadmapContent">The full content of the roadmap document.</param>
    /// <returns>List of parsed releases in order.</returns>
    public IReadOnlyList<ParsedRelease> ParseReleases(string roadmapContent)
    {
        if (string.IsNullOrWhiteSpace(roadmapContent))
        {
            return Array.Empty<ParsedRelease>();
        }

        var headerMatches = ReleaseHeaderRegex().Matches(roadmapContent);
        if (headerMatches.Count == 0)
        {
            return Array.Empty<ParsedRelease>();
        }

        var releases = new List<ParsedRelease>();

        for (var i = 0; i < headerMatches.Count; i++)
        {
            var headerMatch = headerMatches[i];
            if (!int.TryParse(headerMatch.Groups[1].Value, out var releaseNumber))
                continue;

            var releaseName = headerMatch.Groups[2].Value.Trim();

            // Extract the section text between this header and the next
            var sectionStart = headerMatch.Index + headerMatch.Length;
            var sectionEnd = i + 1 < headerMatches.Count
                ? headerMatches[i + 1].Index
                : roadmapContent.Length;
            var sectionText = roadmapContent[sectionStart..sectionEnd];

            // Parse goal
            var goalMatch = GoalRegex().Match(sectionText);
            var goal = goalMatch.Success ? goalMatch.Groups[1].Value.Trim() : "";

            // Parse scope items (bullets between **Scope:** and the next ** section)
            var scope = ParseScopeItems(sectionText);

            // Parse dependencies
            var dependencies = ParseDependencies(sectionText);

            releases.Add(new ParsedRelease
            {
                Number = releaseNumber,
                Name = releaseName,
                Goal = goal,
                Scope = scope,
                Dependencies = dependencies
            });
        }

        return releases.OrderBy(r => r.Number).ToList();
    }

    /// <summary>
    /// Gets the total number of releases in the roadmap.
    /// </summary>
    public int GetReleaseCount(string roadmapContent)
    {
        return ParseReleases(roadmapContent).Count;
    }

    /// <summary>
    /// Extracts scope bullet items from a release section.
    /// Looks for bullets between **Scope:** and the next bold section header.
    /// </summary>
    private static List<string> ParseScopeItems(string sectionText)
    {
        var items = new List<string>();

        // Find the **Scope:** marker
        var scopeIndex = sectionText.IndexOf("**Scope:**", StringComparison.OrdinalIgnoreCase);
        if (scopeIndex < 0)
        {
            // Try alternate format: **Scope**
            scopeIndex = sectionText.IndexOf("**Scope**", StringComparison.OrdinalIgnoreCase);
        }

        if (scopeIndex < 0) return items;

        // Find the end of the scope section (next **Header:** or ### header)
        var scopeStart = scopeIndex;
        var nextSectionPattern = new Regex(@"\*\*(?!Scope)[A-Z][^*]+\*\*|^###", RegexOptions.Multiline);
        var nextSectionMatch = nextSectionPattern.Match(sectionText, scopeStart + 10);
        var scopeEnd = nextSectionMatch.Success ? nextSectionMatch.Index : sectionText.Length;
        var scopeText = sectionText[scopeStart..scopeEnd];

        // Extract bullet items
        var bulletMatches = ScopeBulletRegex().Matches(scopeText);
        foreach (Match bulletMatch in bulletMatches)
        {
            var item = bulletMatch.Groups[1].Value.Trim();
            if (!string.IsNullOrWhiteSpace(item))
            {
                items.Add(item);
            }
        }

        return items;
    }

    /// <summary>
    /// Parses dependency release numbers from a section.
    /// </summary>
    private static List<int> ParseDependencies(string sectionText)
    {
        var deps = new List<int>();

        var depMatch = DependenciesRegex().Match(sectionText);
        if (!depMatch.Success) return deps;

        var depText = depMatch.Groups[1].Value;

        // Check for "None" or similar
        if (depText.Contains("None", StringComparison.OrdinalIgnoreCase) ||
            depText.Contains("N/A", StringComparison.OrdinalIgnoreCase))
        {
            return deps;
        }

        var numberMatches = DependencyNumberRegex().Matches(depText);
        foreach (Match numMatch in numberMatches)
        {
            if (int.TryParse(numMatch.Groups[1].Value, out var depNumber))
            {
                deps.Add(depNumber);
            }
        }

        return deps.Distinct().OrderBy(n => n).ToList();
    }
}
