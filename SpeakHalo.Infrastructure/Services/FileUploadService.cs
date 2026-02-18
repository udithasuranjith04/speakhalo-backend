using System.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpeakHalo.Application.Interfaces;

namespace SpeakHalo.Infrastructure.Services;

public class FileUploadService : IFileUploadService
{
    private readonly string _uploadBasePath;
    private readonly ILogger<FileUploadService> _logger;
    private readonly long _maxFileSizeBytes = 10 * 1024 * 1024;
    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private static readonly string[] AllowedDocumentExtensions = { ".pdf", ".doc", ".docx", ".txt", ".csv", ".xlsx" };

    private static readonly Dictionary<string, List<string>> AllowedMimeTypes = new()
    {
        { ".jpg", new List<string> { "image/jpeg", "image/pjpeg" } },
        { ".jpeg", new List<string> { "image/jpeg", "image/pjpeg" } },
        { ".png", new List<string> { "image/png" } },
        { ".gif", new List<string> { "image/gif" } },
        { ".webp", new List<string> { "image/webp" } },
        { ".pdf", new List<string> { "application/pdf" } },
        { ".doc", new List<string> { "application/msword" } },
        { ".docx", new List<string> { "application/vnd.openxmlformats-officedocument.wordprocessingml.document" } },
        { ".txt", new List<string> { "text/plain" } },
        { ".csv", new List<string> { "text/csv", "application/csv" } },
        { ".xlsx", new List<string> { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" } }
    };

    private static readonly Dictionary<string, byte[][]> FileSignatures = new()
    {
        { ".jpg", new[] { new byte[] { 0xFF, 0xD8, 0xFF } } },
        { ".jpeg", new[] { new byte[] { 0xFF, 0xD8, 0xFF } } },
        { ".png", new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } } },
        { ".gif", new[] { new byte[] { 0x47, 0x49, 0x46, 0x38 } } },
        { ".pdf", new[] { new byte[] { 0x25, 0x50, 0x44, 0x46 } } },
        { ".docx", new[] { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } },
        { ".xlsx", new[] { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } }
    };

    public FileUploadService(IConfiguration configuration, ILogger<FileUploadService> logger)
    {
        _uploadBasePath = configuration["FileUpload:BasePath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        _logger = logger;

        if (!Directory.Exists(_uploadBasePath))
        {
            Directory.CreateDirectory(_uploadBasePath);
        }
    }

    public async Task<string> UploadFileAsync(IFormFile file, string folder, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty or null");

        if (!IsValidFileSize(file, _maxFileSizeBytes))
            throw new ArgumentException($"File size exceeds maximum allowed size of {_maxFileSizeBytes / 1024 / 1024}MB");

        var extension = GetFileExtension(file.FileName);
        if (!IsValidExtension(extension))
            throw new ArgumentException($"File type '{extension}' is not allowed");

        if (!IsValidMimeType(file, extension))
            throw new ArgumentException($"File content does not match the expected type for '{extension}'");

        if (!await IsValidFileSignatureAsync(file, extension, cancellationToken))
            throw new ArgumentException($"File signature validation failed for '{extension}'");

        var sanitizedFolder = SanitizeFolderPath(folder);

        var uniqueFileName = GenerateUniqueFileName(file.FileName);
        var folderPath = Path.Combine(_uploadBasePath, sanitizedFolder);

        var fullFolderPath = Path.GetFullPath(folderPath);
        var fullBasePath = Path.GetFullPath(_uploadBasePath);

        if (!fullFolderPath.StartsWith(fullBasePath, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Path traversal attempt detected: Folder={Folder}, Base={Base}", fullFolderPath, fullBasePath);
            throw new SecurityException("Invalid upload path detected");
        }

        if (!Directory.Exists(fullFolderPath))
        {
            Directory.CreateDirectory(fullFolderPath);
        }

        var filePath = Path.Combine(fullFolderPath, uniqueFileName);

        var fullFilePath = Path.GetFullPath(filePath);
        if (!fullFilePath.StartsWith(fullBasePath, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Path traversal attempt detected in filename: File={File}, Base={Base}", fullFilePath, fullBasePath);
            throw new SecurityException("Invalid file path detected");
        }

        using (var stream = new FileStream(fullFilePath, FileMode.Create))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        _logger.LogInformation("File uploaded successfully: {FilePath}", fullFilePath);

        return Path.Combine(sanitizedFolder, uniqueFileName).Replace("\\", "/");
    }

    public async Task<string> UploadImageAsync(IFormFile file, string folder, int maxWidth = 1920, int maxHeight = 1080, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("Image file is empty or null");

        if (!IsValidImageFile(file))
            throw new ArgumentException("File is not a valid image");

        // For now, just upload the file without resizing
        // In production, use ImageSharp or SkiaSharp to resize images
        return await UploadFileAsync(file, folder, cancellationToken);
    }

    public async Task<bool> DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.Combine(_uploadBasePath, filePath);

            var resolvedPath = Path.GetFullPath(fullPath);
            var fullBasePath = Path.GetFullPath(_uploadBasePath);

            if (!resolvedPath.StartsWith(fullBasePath, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Path traversal attempt in delete: {Path}", filePath);
                return false;
            }

            if (!File.Exists(resolvedPath))
            {
                _logger.LogWarning("File not found for deletion: {FilePath}", resolvedPath);
                return false;
            }

            await Task.Run(() => File.Delete(resolvedPath), cancellationToken);
            _logger.LogInformation("File deleted successfully: {FilePath}", resolvedPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FilePath}", filePath);
            return false;
        }
    }

    public async Task<byte[]> DownloadFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_uploadBasePath, filePath);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: {filePath}");

        return await File.ReadAllBytesAsync(fullPath, cancellationToken);
    }

    public bool IsValidImageFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return false;

        var extension = GetFileExtension(file.FileName);
        return AllowedImageExtensions.Contains(extension.ToLowerInvariant());
    }

    public bool IsValidFileSize(IFormFile file, long maxSizeInBytes)
    {
        return file != null && file.Length > 0 && file.Length <= maxSizeInBytes;
    }

    public string GetFileExtension(string fileName)
    {
        return Path.GetExtension(fileName).ToLowerInvariant();
    }

    public string GenerateUniqueFileName(string originalFileName)
    {
        var extension = GetFileExtension(originalFileName);
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);

        fileNameWithoutExtension = SanitizeFileName(fileNameWithoutExtension);

        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var guid = Guid.NewGuid().ToString("N").Substring(0, 8);

        return $"{fileNameWithoutExtension}_{timestamp}_{guid}{extension}";
    }

    private bool IsValidExtension(string extension)
    {
        var allAllowedExtensions = AllowedImageExtensions.Concat(AllowedDocumentExtensions);
        return allAllowedExtensions.Contains(extension.ToLowerInvariant());
    }

    private string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));

        if (sanitized.Length > 50)
            sanitized = sanitized.Substring(0, 50);

        return sanitized;
    }

    private string SanitizeFolderPath(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder))
            return string.Empty;

        folder = folder.Replace("..", string.Empty)
                       .Replace("~", string.Empty)
                       .Trim()
                       .Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        var invalidChars = Path.GetInvalidPathChars();
        var sanitized = string.Join("_", folder.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));

        sanitized = sanitized.Replace('/', Path.DirectorySeparatorChar)
                            .Replace('\\', Path.DirectorySeparatorChar);

        return sanitized;
    }

    private bool IsValidMimeType(IFormFile file, string extension)
    {
        if (string.IsNullOrEmpty(file.ContentType))
            return false;

        if (!AllowedMimeTypes.TryGetValue(extension.ToLowerInvariant(), out var allowedTypes))
            return true; // No MIME restrictions for this extension

        return allowedTypes.Any(mt => file.ContentType.Equals(mt, StringComparison.OrdinalIgnoreCase));
    }

    private Task<bool> IsValidFileSignatureAsync(IFormFile file, string extension, CancellationToken cancellationToken)
    {
        if (!FileSignatures.TryGetValue(extension.ToLowerInvariant(), out var signatures))
            return Task.FromResult(true);

        using var stream = file.OpenReadStream();
        using var reader = new BinaryReader(stream);

        foreach (var signature in signatures)
        {
            stream.Position = 0;
            var headerBytes = reader.ReadBytes(signature.Length);

            if (headerBytes.Length != signature.Length)
                continue;

            if (headerBytes.SequenceEqual(signature))
                return Task.FromResult(true);
        }

        _logger.LogWarning("File signature validation failed for {FileName} with extension {Extension}", file.FileName, extension);
        return Task.FromResult(false);
    }
}
