namespace Transcrib.Api.Services;

/// <summary>
/// Upload quotas and constraints for transcription jobs. Bound from the
/// <c>TranscriptionLimits</c> configuration section so the values can be tuned
/// per environment without a code change.
/// </summary>
public class TranscriptionLimits
{
    /// <summary>Maximum number of jobs that may be created in a rolling 24-hour window.</summary>
    public int MaxPerDay { get; set; } = 3;

    /// <summary>Maximum number of jobs that may be created in a rolling 7-day window.</summary>
    public int MaxPerWeek { get; set; } = 10;

    /// <summary>Maximum allowed audio length, in minutes.</summary>
    public int MaxDurationMinutes { get; set; } = 5;

    public TimeSpan MaxDuration => TimeSpan.FromMinutes(MaxDurationMinutes);
}
