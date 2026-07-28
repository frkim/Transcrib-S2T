namespace Transcrib.Shared.Services;

/// <summary>
/// Computes the playback duration of an MP3 file by parsing its frame headers.
/// Handles ID3v2 tags, MPEG 1/2/2.5 Layer III, and VBR streams (Xing/Info/VBRI
/// headers). Falls back to a constant-bitrate estimate otherwise. Returns
/// <c>null</c> when the stream cannot be parsed, so callers can decide how to
/// treat files of unknown length.
/// </summary>
public sealed class Mp3DurationProbe : IAudioDurationProbe
{
    // Layer III bitrate tables in kbps, indexed by the 4-bit bitrate field.
    private static readonly int[] BitRatesV1 = { 0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320 };
    private static readonly int[] BitRatesV2 = { 0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160 };

    // Sampling rates in Hz, indexed by [versionBits][sampleIndex].
    private static readonly int[][] SampleRates =
    {
        new[] { 11025, 12000, 8000 },  // 0 = MPEG 2.5
        new[] { 0, 0, 0 },             // 1 = reserved
        new[] { 22050, 24000, 16000 }, // 2 = MPEG 2
        new[] { 44100, 48000, 32000 }, // 3 = MPEG 1
    };

    public TimeSpan? TryGetDuration(byte[] data)
    {
        if (data is null || data.Length < 4)
        {
            return null;
        }

        var start = FindFirstFrame(data, SkipId3v2(data));
        if (start < 0 || ParseHeader(data, start) is not { } header)
        {
            return null;
        }

        double seconds;
        if (TryReadVbrFrameCount(data, start, header) is int frames && frames > 0)
        {
            seconds = (double)frames * header.SamplesPerFrame / header.SampleRate;
        }
        else
        {
            // Constant-bitrate estimate from the remaining audio payload.
            var audioBytes = data.Length - start;
            seconds = audioBytes * 8.0 / (header.BitRateKbps * 1000.0);
        }

        if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds <= 0)
        {
            return null;
        }

        return TimeSpan.FromSeconds(seconds);
    }

    /// <summary>Returns the byte offset past any leading ID3v2 tag.</summary>
    private static int SkipId3v2(byte[] data)
    {
        if (data.Length < 10 || data[0] != (byte)'I' || data[1] != (byte)'D' || data[2] != (byte)'3')
        {
            return 0;
        }

        // Tag size is a 28-bit sync-safe integer (bytes 6..9).
        var size = (data[6] & 0x7F) << 21 | (data[7] & 0x7F) << 14 | (data[8] & 0x7F) << 7 | (data[9] & 0x7F);
        var end = 10 + size;
        if ((data[5] & 0x10) != 0)
        {
            end += 10; // footer present
        }

        return end < data.Length ? end : 0;
    }

    /// <summary>Scans forward for the first parseable MPEG audio frame.</summary>
    private static int FindFirstFrame(byte[] data, int from)
    {
        for (var i = Math.Max(0, from); i < data.Length - 4; i++)
        {
            if (data[i] == 0xFF && (data[i + 1] & 0xE0) == 0xE0 && ParseHeader(data, i) is not null)
            {
                return i;
            }
        }

        return -1;
    }

    private static FrameHeader? ParseHeader(byte[] data, int i)
    {
        if (i + 4 > data.Length)
        {
            return null;
        }

        var b1 = data[i + 1];
        var b2 = data[i + 2];
        var b3 = data[i + 3];

        var versionBits = (b1 >> 3) & 0x03; // 0=2.5, 1=reserved, 2=2, 3=1
        var layerBits = (b1 >> 1) & 0x03;   // 1 = Layer III
        if (versionBits == 1 || layerBits != 0x01)
        {
            return null; // reserved version or non Layer III
        }

        var bitrateIndex = (b2 >> 4) & 0x0F;
        var sampleIndex = (b2 >> 2) & 0x03;
        if (bitrateIndex == 0 || bitrateIndex == 0x0F || sampleIndex == 3)
        {
            return null; // free/bad bitrate or reserved sampling rate
        }

        var mpeg1 = versionBits == 3;
        var channelMode = (b3 >> 6) & 0x03;

        return new FrameHeader(
            BitRateKbps: mpeg1 ? BitRatesV1[bitrateIndex] : BitRatesV2[bitrateIndex],
            SampleRate: SampleRates[versionBits][sampleIndex],
            SamplesPerFrame: mpeg1 ? 1152 : 576,
            IsMpeg1: mpeg1,
            IsMono: channelMode == 3);
    }

    /// <summary>Reads the frame count from a Xing/Info or VBRI header when present.</summary>
    private static int? TryReadVbrFrameCount(byte[] data, int frameStart, FrameHeader header)
    {
        var sideInfo = header.IsMpeg1 ? (header.IsMono ? 17 : 32) : (header.IsMono ? 9 : 17);
        var xing = frameStart + 4 + sideInfo;
        if (Matches(data, xing, "Xing") || Matches(data, xing, "Info"))
        {
            var flags = ReadInt32BigEndian(data, xing + 4);
            if (flags is int f && (f & 0x01) != 0)
            {
                return ReadInt32BigEndian(data, xing + 8);
            }
        }

        var vbri = frameStart + 4 + 32;
        if (Matches(data, vbri, "VBRI"))
        {
            return ReadInt32BigEndian(data, vbri + 14);
        }

        return null;
    }

    private static bool Matches(byte[] data, int offset, string tag)
    {
        if (offset < 0 || offset + tag.Length > data.Length)
        {
            return false;
        }

        for (var k = 0; k < tag.Length; k++)
        {
            if (data[offset + k] != (byte)tag[k])
            {
                return false;
            }
        }

        return true;
    }

    private static int? ReadInt32BigEndian(byte[] data, int offset)
    {
        if (offset < 0 || offset + 4 > data.Length)
        {
            return null;
        }

        return data[offset] << 24 | data[offset + 1] << 16 | data[offset + 2] << 8 | data[offset + 3];
    }

    private readonly record struct FrameHeader(
        int BitRateKbps,
        int SampleRate,
        int SamplesPerFrame,
        bool IsMpeg1,
        bool IsMono);
}
