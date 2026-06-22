namespace Transcrib.Shared.Services;

/// <summary>
/// Validation helpers for uploaded audio files. Only MP3 files are accepted,
/// per the shared solution contract.
/// </summary>
public static class AudioFileValidator
{
    public const string AllowedContentType = "audio/mpeg";
    public const string AllowedExtension = ".mp3";

    public static bool IsValidMp3(string? fileName, string? contentType)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        var hasMp3Extension = fileName.EndsWith(AllowedExtension, StringComparison.OrdinalIgnoreCase);

        // Content type is optional (some clients omit it); when present it must match.
        var contentTypeOk = string.IsNullOrWhiteSpace(contentType)
            || contentType.Equals(AllowedContentType, StringComparison.OrdinalIgnoreCase);

        return hasMp3Extension && contentTypeOk;
    }
}
