namespace Hugh.Models;

/// <summary>
/// Represents a decision made during the development loop.
/// Used for audit logging and decision transparency.
/// </summary>
public record Decision
{
    public string Phase { get; init; } = "";
    public string Question { get; init; } = "";
    public List<string> Options { get; init; } = new();
    public string Selected { get; init; } = "";
    public string Rationale { get; init; } = "";
    public bool Autonomous { get; init; }
    public DateTime Timestamp { get; init; }
}
