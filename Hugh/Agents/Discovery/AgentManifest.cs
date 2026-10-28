using System.Text.Json;

namespace Hugh.Agents.Discovery;

/// <summary>
/// Tracks the state of locally cached agent files from a remote source.
/// Persisted as ~/.hugh/agents/{org}/manifest.json.
/// </summary>
public record AgentManifest
{
    public string Org { get; init; } = "";
    public string Repository { get; init; } = ".github-private";
    public DateTime LastSyncUtc { get; init; }
    public List<AgentFileEntry> Files { get; init; } = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Loads a manifest from a directory. Returns null if not found or corrupt.
    /// </summary>
    public static AgentManifest? Load(string directoryPath)
    {
        var manifestPath = Path.Combine(directoryPath, "manifest.json");
        if (!File.Exists(manifestPath))
            return null;

        try
        {
            var json = File.ReadAllText(manifestPath);
            return JsonSerializer.Deserialize<AgentManifest>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Saves the manifest to the specified directory.
    /// </summary>
    public void Save(string directoryPath)
    {
        Directory.CreateDirectory(directoryPath);
        var manifestPath = Path.Combine(directoryPath, "manifest.json");
        var json = JsonSerializer.Serialize(this, JsonOptions);
        var tmpPath = manifestPath + ".tmp";
        File.WriteAllText(tmpPath, json);
        File.Move(tmpPath, manifestPath, overwrite: true);
    }
}

/// <summary>
/// Represents a single cached agent file entry in the manifest.
/// </summary>
public record AgentFileEntry
{
    public string FileName { get; init; } = "";
    public string Sha { get; init; } = "";
    public long Size { get; init; }
    public DateTime DownloadedAtUtc { get; init; }
}
