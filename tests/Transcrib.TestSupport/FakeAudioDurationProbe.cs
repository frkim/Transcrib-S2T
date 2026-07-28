using Transcrib.Shared.Services;

namespace Transcrib.Tests.Common;

/// <summary>
/// Deterministic <see cref="IAudioDurationProbe"/> for unit tests. Returns a
/// fixed duration regardless of the supplied bytes; defaults to <c>null</c>
/// (unknown length), which mirrors an unparseable stub upload.
/// </summary>
public class FakeAudioDurationProbe : IAudioDurationProbe
{
    private readonly TimeSpan? _duration;

    public FakeAudioDurationProbe(TimeSpan? duration = null) => _duration = duration;

    public TimeSpan? TryGetDuration(byte[] data) => _duration;
}
