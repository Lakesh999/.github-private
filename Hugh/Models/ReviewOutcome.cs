namespace Hugh.Models;

/// <summary>
/// Represents the outcome of a plan review phase.
/// Used to determine whether the plan proceeds to coding or requires revision.
/// </summary>
public enum ReviewOutcome
{
    /// <summary>
    /// Plan is approved with no issues. Proceed to coding.
    /// </summary>
    Approved,

    /// <summary>
    /// Plan is approved but has minor findings. Proceed to coding with warnings logged.
    /// </summary>
    ApprovedWithFindings,

    /// <summary>
    /// Plan has issues that require revision before coding can begin.
    /// </summary>
    Rejected
}
