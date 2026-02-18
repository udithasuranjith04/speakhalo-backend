namespace SpeakHalo.Application.Interfaces;

/// <summary>
/// File storage abstraction layer
/// Supports local filesystem, S3, Azure Blob, etc.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Save file to storage
    /// </summary>
    /// <param name="fileName">File name (with extension)</param>
    /// <param name="fileData">File binary data</param>
    /// <param name="contentType">MIME type</param>
    /// <param name="folder">Optional folder/path prefix</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stored file path</returns>
    Task<string> SaveFileAsync(
        string fileName,
        byte[] fileData,
        string contentType,
        string? folder = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Save file from stream to storage
    /// </summary>
    Task<string> SaveFileFromStreamAsync(
        string fileName,
        Stream fileStream,
        string contentType,
        string? folder = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get file data
    /// </summary>
    /// <param name="filePath">File path returned from SaveFileAsync</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File binary data</returns>
    Task<byte[]> GetFileAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get file as stream
    /// </summary>
    Task<Stream> GetFileStreamAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete file from storage
    /// </summary>
    /// <param name="filePath">File path to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteFileAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if file exists
    /// </summary>
    Task<bool> FileExistsAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get file size in bytes
    /// </summary>
    Task<long> GetFileSizeAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate a public access URL (temporary or permanent)
    /// </summary>
    /// <param name="filePath">File path</param>
    /// <param name="expirationMinutes">Expiration time in minutes (0 for permanent)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Public URL</returns>
    Task<string> GetPublicUrlAsync(
        string filePath,
        int expirationMinutes = 60,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Copy file to another location
    /// </summary>
    Task<string> CopyFileAsync(
        string sourceFilePath,
        string destinationFileName,
        string? destinationFolder = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Move file to another location
    /// </summary>
    Task<string> MoveFileAsync(
        string sourceFilePath,
        string destinationFileName,
        string? destinationFolder = null,
        CancellationToken cancellationToken = default);
}
