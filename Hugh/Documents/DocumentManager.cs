using Serilog;

namespace Hugh.Documents;

/// <summary>
/// Manages document operations for the development loop.
/// Handles reading document content and finding documents by type.
/// </summary>
public class DocumentManager
{
    private readonly string _workspaceRoot;
    private readonly ILogger _logger;

    /// <summary>
    /// Document type folders relative to /docs/
    /// </summary>
    private static readonly Dictionary<string, string> DocumentTypeFolders = new()
    {
        { "vision", "docs/vision" },
        { "research", "docs/research" },
        { "plan", "docs/plans" },
        { "plans", "docs/plans" }
    };

    public DocumentManager(string workspaceRoot, ILogger? logger = null)
    {
        _workspaceRoot = workspaceRoot;
        _logger = logger ?? Log.Logger;
    }

    /// <summary>
    /// Reads the content of a document.
    /// </summary>
    /// <param name="documentPath">Relative or absolute path to the document.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The document content.</returns>
    public async Task<string> ReadDocumentAsync(string documentPath, CancellationToken ct = default)
    {
        var fullPath = GetFullPath(documentPath);

        if (!File.Exists(fullPath))
        {
            _logger.Warning("Document not found: {Path}", fullPath);
            throw new FileNotFoundException($"Document not found: {documentPath}", fullPath);
        }

        _logger.Debug("Reading document: {Path}", fullPath);
        return await File.ReadAllTextAsync(fullPath, ct);
    }

    /// <summary>
    /// Checks if a document exists.
    /// </summary>
    public bool DocumentExists(string documentPath)
    {
        var fullPath = GetFullPath(documentPath);
        return File.Exists(fullPath);
    }

    /// <summary>
    /// Finds the latest document of a given type (e.g., "vision", "research", "plan").
    /// Documents are expected to be numbered with a prefix (e.g., 001-, 002-).
    /// </summary>
    /// <param name="documentType">Type of document to find.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The path to the latest document, or null if none found.</returns>
    public async Task<string?> FindLatestDocumentAsync(string documentType, CancellationToken ct = default)
    {
        // Allow cancellation check
        await Task.CompletedTask;
        ct.ThrowIfCancellationRequested();

        if (!DocumentTypeFolders.TryGetValue(documentType.ToLowerInvariant(), out var folder))
        {
            _logger.Warning("Unknown document type: {Type}", documentType);
            return null;
        }

        var folderPath = Path.Combine(_workspaceRoot, folder);

        if (!Directory.Exists(folderPath))
        {
            _logger.Debug("Document folder does not exist: {Path}", folderPath);
            return null;
        }

        // Find all markdown files and sort by name (assumes numeric prefix like 001-, 002-)
        var files = Directory.GetFiles(folderPath, "*.md")
            .OrderByDescending(f => Path.GetFileName(f))
            .ToList();

        if (files.Count == 0)
        {
            _logger.Debug("No documents found in {Path}", folderPath);
            return null;
        }

        var latest = files.First();
        _logger.Debug("Found latest {Type} document: {Path}", documentType, latest);

        // Return relative path
        return Path.GetRelativePath(_workspaceRoot, latest);
    }

    /// <summary>
    /// Finds documents matching a specific feature/topic name.
    /// </summary>
    /// <param name="documentType">Type of document (vision, research, plan).</param>
    /// <param name="featureName">Feature name to search for in filenames.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of matching document paths.</returns>
    public async Task<IReadOnlyList<string>> FindDocumentsByFeatureAsync(
        string documentType,
        string featureName,
        CancellationToken ct = default)
    {
        // Allow cancellation check
        await Task.CompletedTask;
        ct.ThrowIfCancellationRequested();

        if (!DocumentTypeFolders.TryGetValue(documentType.ToLowerInvariant(), out var folder))
        {
            _logger.Warning("Unknown document type: {Type}", documentType);
            return Array.Empty<string>();
        }

        var folderPath = Path.Combine(_workspaceRoot, folder);

        if (!Directory.Exists(folderPath))
        {
            return Array.Empty<string>();
        }

        // Normalize feature name for matching
        var normalizedFeature = NormalizeForSearch(featureName);

        var matches = Directory.GetFiles(folderPath, "*.md")
            .Where(f => NormalizeForSearch(Path.GetFileNameWithoutExtension(f)).Contains(normalizedFeature))
            .Select(f => Path.GetRelativePath(_workspaceRoot, f))
            .ToList();

        _logger.Debug("Found {Count} {Type} documents matching '{Feature}'", 
            matches.Count, documentType, featureName);

        return matches;
    }

    /// <summary>
    /// Gets the next document number for a given type.
    /// </summary>
    /// <param name="documentType">Type of document.</param>
    /// <returns>The next number (e.g., 3 if 001 and 002 exist).</returns>
    public int GetNextDocumentNumber(string documentType)
    {
        if (!DocumentTypeFolders.TryGetValue(documentType.ToLowerInvariant(), out var folder))
        {
            return 1;
        }

        var folderPath = Path.Combine(_workspaceRoot, folder);

        if (!Directory.Exists(folderPath))
        {
            return 1;
        }

        var files = Directory.GetFiles(folderPath, "*.md");
        if (files.Length == 0)
        {
            return 1;
        }

        // Extract numbers from filenames (assumes format like "001-feature-name.md")
        var maxNumber = files
            .Select(f => Path.GetFileName(f))
            .Select(name =>
            {
                var parts = name.Split('-', 2);
                return int.TryParse(parts[0], out var num) ? num : 0;
            })
            .Max();

        return maxNumber + 1;
    }

    /// <summary>
    /// Generates a document path for a new document.
    /// </summary>
    /// <param name="documentType">Type of document.</param>
    /// <param name="featureName">Feature name for the filename.</param>
    /// <returns>The generated document path.</returns>
    public string GenerateDocumentPath(string documentType, string featureName)
    {
        if (!DocumentTypeFolders.TryGetValue(documentType.ToLowerInvariant(), out var folder))
        {
            throw new ArgumentException($"Unknown document type: {documentType}");
        }

        var number = GetNextDocumentNumber(documentType);
        var normalizedName = NormalizeForFilename(featureName);
        var filename = $"{number:D3}-{normalizedName}.md";

        return Path.Combine(folder, filename);
    }

    /// <summary>
    /// Gets all documents of a given type.
    /// </summary>
    public IReadOnlyList<string> GetAllDocuments(string documentType)
    {
        if (!DocumentTypeFolders.TryGetValue(documentType.ToLowerInvariant(), out var folder))
        {
            return Array.Empty<string>();
        }

        var folderPath = Path.Combine(_workspaceRoot, folder);

        if (!Directory.Exists(folderPath))
        {
            return Array.Empty<string>();
        }

        return Directory.GetFiles(folderPath, "*.md")
            .Select(f => Path.GetRelativePath(_workspaceRoot, f))
            .OrderBy(f => f)
            .ToList();
    }

    /// <summary>
    /// Gets the full path for a document path.
    /// </summary>
    private string GetFullPath(string documentPath)
    {
        if (Path.IsPathRooted(documentPath))
        {
            return documentPath;
        }

        return Path.Combine(_workspaceRoot, documentPath);
    }

    /// <summary>
    /// Normalizes a string for search matching.
    /// </summary>
    private static string NormalizeForSearch(string input)
    {
        return input
            .ToLowerInvariant()
            .Replace("-", "")
            .Replace("_", "")
            .Replace(" ", "");
    }

    /// <summary>
    /// Normalizes a string for use in filenames.
    /// </summary>
    private static string NormalizeForFilename(string input)
    {
        // Convert to kebab-case
        return input
            .ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-")
            .Where(c => char.IsLetterOrDigit(c) || c == '-')
            .Aggregate("", (current, c) => current + c)
            .Trim('-');
    }
}
