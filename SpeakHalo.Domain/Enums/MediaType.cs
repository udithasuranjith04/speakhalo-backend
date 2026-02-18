namespace SpeakHalo.Domain.Enums;

/// <summary>
/// Media file type categories
/// </summary>
public enum MediaType
{
    /// <summary>
    /// Unknown or unclassified media
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Image file (JPEG, PNG, GIF, WebP, etc.)
    /// </summary>
    Image = 1,

    /// <summary>
    /// Video file (MP4, MOV, AVI, etc.)
    /// </summary>
    Video = 2,

    /// <summary>
    /// Audio file (MP3, WAV, OGG, etc.)
    /// </summary>
    Audio = 3,

    /// <summary>
    /// Document file (PDF, DOC, XLS, TXT, etc.)
    /// </summary>
    Document = 4,

    /// <summary>
    /// Voice message
    /// </summary>
    Voice = 5,

    /// <summary>
    /// Sticker
    /// </summary>
    Sticker = 6,

    /// <summary>
    /// Location data
    /// </summary>
    Location = 7,

    /// <summary>
    /// Contact vCard
    /// </summary>
    Contact = 8
}
