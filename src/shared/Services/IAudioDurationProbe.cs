namespace Transcrib.Shared.Services;

/// <summary>
/// Estimates the playback duration of an uploaded audio file. Abstracted so the
/// upload limit (maximum conversation length) can be enforced in the API and
/// unit tested with a deterministic fake.
/// </summary>
public interface IAudioDurationProbe
{
    /// <summary>
    /// Returns the estimated duration of the supplied audio bytes, or
    /// <c>null</c> when the duration cannot be determined.
    /// </summary>
    TimeSpan? TryGetDuration(byte[] data);
}
