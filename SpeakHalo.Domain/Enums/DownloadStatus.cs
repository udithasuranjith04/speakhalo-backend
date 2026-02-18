namespace SpeakHalo.Domain.Enums;

/// <summary>
/// Media download status
/// </summary>
public enum DownloadStatus
{
    /// <summary>
    /// Download not started yet
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Download in progress
    /// </summary>
    Downloading = 1,

    /// <summary>
    /// Download completed successfully
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Download failed
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Original URL expired before download
    /// </summary>
    Expired = 4,

    /// <summary>
    /// Download cancelled
    /// </summary>
    Cancelled = 5
}
