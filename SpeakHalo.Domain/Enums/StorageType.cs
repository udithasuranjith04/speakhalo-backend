namespace SpeakHalo.Domain.Enums;

/// <summary>
/// Media file storage location types
/// </summary>
public enum StorageType
{
    /// <summary>
    /// Stored in local filesystem
    /// </summary>
    Local = 0,

    /// <summary>
    /// Stored in AWS S3
    /// </summary>
    S3 = 1,

    /// <summary>
    /// Stored in Azure Blob Storage
    /// </summary>
    AzureBlob = 2,

    /// <summary>
    /// Stored in Google Cloud Storage
    /// </summary>
    GoogleCloud = 3,

    /// <summary>
    /// Stored at external URL (CDN, etc.)
    /// </summary>
    External = 4
}
