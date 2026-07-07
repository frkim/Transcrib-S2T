using Transcrib.Functions.Services;
using Xunit;

namespace Transcrib.Functions.Tests;

public class BuildTranscriptTests
{
    [Fact]
    public void BuildTranscript_WithPhrases_PrefixesSpeakerAndTimeRange()
    {
        // offset 3200ms, duration 4300ms -> [00:00:03 → 00:00:07]
        // offset 61000ms, duration 5000ms -> [00:01:01 → 00:01:06]
        var payload = """
        {
          "phrases": [
            { "speaker": 1, "offsetMilliseconds": 3200, "durationMilliseconds": 4300, "text": "Bonjour" },
            { "speaker": 2, "offsetMilliseconds": 61000, "durationMilliseconds": 5000, "text": "Avec plaisir" }
          ]
        }
        """;

        var transcript = AzureSpeechTranscriptionService.BuildTranscript(payload);

        var lines = transcript.Split('\n');
        Assert.Equal("[00:00:03 → 00:00:07] Speaker 1: Bonjour", lines[0].TrimEnd('\r'));
        Assert.Equal("[00:01:01 → 00:01:06] Speaker 2: Avec plaisir", lines[1].TrimEnd('\r'));
    }

    [Fact]
    public void BuildTranscript_MissingTiming_DefaultsToZero()
    {
        var payload = """
        {
          "phrases": [
            { "speaker": 1, "text": "No timing here" }
          ]
        }
        """;

        var transcript = AzureSpeechTranscriptionService.BuildTranscript(payload);

        Assert.Equal("[00:00:00 → 00:00:00] Speaker 1: No timing here", transcript.TrimEnd());
    }

    [Fact]
    public void BuildTranscript_NoPhrases_FallsBackToCombinedText()
    {
        var payload = """
        {
          "combinedPhrases": [
            { "text": "Fallback transcript." }
          ]
        }
        """;

        var transcript = AzureSpeechTranscriptionService.BuildTranscript(payload);

        Assert.Equal("Fallback transcript.", transcript.TrimEnd());
    }
}
