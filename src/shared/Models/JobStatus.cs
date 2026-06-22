namespace Transcrib.Shared.Models;

/// <summary>
/// Status values shared across all components of the Transcrib-S2T solution.
/// Mirrors the shared contract defined by the orchestrator agent.
/// </summary>
public static class JobStatus
{
    public const string Processing = "Processing";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
    public const string Purged = "Purged";
}
