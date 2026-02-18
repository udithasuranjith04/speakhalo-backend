using System.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpeakHalo.Application.Interfaces;

namespace SpeakHalo.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;
    private readonly string _baseUrl;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(
        IConfiguration configuration,
        ILogger<LocalFileStorageService> logger)
    {
        _logger = logger;

        _basePath = Path.GetFullPath(configuration["Storage:LocalPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads"));
        _baseUrl = configuration["Storage:BaseUrl"] ?? "/uploads";

        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
            _logger.LogInformation("Created storage directory: {Path}", _basePath);
        }
    }

    private string ResolveSafePath(string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_basePath, relativePath));
        if (!fullPath.StartsWith(_basePath, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Path traversal attempt detected: {Path}", relativePath);
            throw new SecurityException("Invalid file path detected");
        }
        return fullPath;
    }

    private string ResolveSafeDirectory(string? folder)
    {
        if (string.IsNullOrEmpty(folder))
            return _basePath;

        var fullPath = Path.GetFullPath(Path.Combine(_basePath, folder));
        if (!fullPath.StartsWith(_basePath, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Path traversal attempt detected in folder: {Folder}", folder);
            throw new SecurityException("Invalid folder path detected");
        }
        return fullPath;
    }

    public async Task<string> SaveFileAsync(
        string fileName,
        byte[] fileData,
        string contentType,
        string? folder = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var directory = ResolveSafeDirectory(folder);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var filePath = ResolveSafePath(Path.Combine(folder ?? "", fileName));

            await File.WriteAllBytesAsync(filePath, fileData, cancellationToken);

            _logger.LogInformation("Saved file: {FileName}, Size: {Size} bytes", fileName, fileData.Length);

            return string.IsNullOrEmpty(folder)
                ? fileName
                : Path.Combine(folder, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save file: {FileName}", fileName);
            throw;
        }
    }

    public async Task<string> SaveFileFromStreamAsync(
        string fileName,
        Stream fileStream,
        string contentType,
        string? folder = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var directory = ResolveSafeDirectory(folder);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var filePath = ResolveSafePath(Path.Combine(folder ?? "", fileName));

            using (var fileStreamOut = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                await fileStream.CopyToAsync(fileStreamOut, cancellationToken);
            }

            _logger.LogInformation("Saved file from stream: {FileName}", fileName);

            return string.IsNullOrEmpty(folder)
                ? fileName
                : Path.Combine(folder, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save file from stream: {FileName}", fileName);
            throw;
        }
    }

    public async Task<byte[]> GetFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = ResolveSafePath(filePath);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            return await File.ReadAllBytesAsync(fullPath, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get file: {FilePath}", filePath);
            throw;
        }
    }

    public Task<Stream> GetFileStreamAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = ResolveSafePath(filePath);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            return Task.FromResult<Stream>(new FileStream(fullPath, FileMode.Open, FileAccess.Read));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get file stream: {FilePath}", filePath);
            throw;
        }
    }

    public Task<bool> DeleteFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = ResolveSafePath(filePath);

            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("File not found for deletion: {FilePath}", filePath);
                return Task.FromResult(false);
            }

            File.Delete(fullPath);
            _logger.LogInformation("Deleted file: {FilePath}", filePath);

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file: {FilePath}", filePath);
            return Task.FromResult(false);
        }
    }

    public Task<bool> FileExistsAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var fullPath = ResolveSafePath(filePath);
        return Task.FromResult(File.Exists(fullPath));
    }

    public Task<long> GetFileSizeAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = ResolveSafePath(filePath);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            var fileInfo = new FileInfo(fullPath);
            return Task.FromResult(fileInfo.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get file size: {FilePath}", filePath);
            throw;
        }
    }

    public Task<string> GetPublicUrlAsync(
        string filePath,
        int expirationMinutes = 60,
        CancellationToken cancellationToken = default)
    {
        // For local storage, return a static URL (no expiration)
        // In production, you'd want to implement signed URLs or use a CDN
        var url = $"{_baseUrl}/{filePath.Replace("\\", "/")}";
        return Task.FromResult(url);
    }

    public Task<string> CopyFileAsync(
        string sourceFilePath,
        string destinationFileName,
        string? destinationFolder = null,
        CancellationToken cancellationToken = default)
    {
        var sourceFullPath = ResolveSafePath(sourceFilePath);

        if (!File.Exists(sourceFullPath))
            throw new FileNotFoundException($"Source file not found: {sourceFilePath}");

        var destinationDirectory = ResolveSafeDirectory(destinationFolder);

        if (!Directory.Exists(destinationDirectory))
            Directory.CreateDirectory(destinationDirectory);

        var destinationFullPath = ResolveSafePath(Path.Combine(destinationFolder ?? "", destinationFileName));
        File.Copy(sourceFullPath, destinationFullPath, overwrite: true);

        _logger.LogInformation("Copied file: {Source} -> {Destination}", sourceFilePath, destinationFileName);

        var relativePath = string.IsNullOrEmpty(destinationFolder)
            ? destinationFileName
            : Path.Combine(destinationFolder, destinationFileName);

        return Task.FromResult(relativePath);
    }

    public Task<string> MoveFileAsync(
        string sourceFilePath,
        string destinationFileName,
        string? destinationFolder = null,
        CancellationToken cancellationToken = default)
    {
        var sourceFullPath = ResolveSafePath(sourceFilePath);

        if (!File.Exists(sourceFullPath))
            throw new FileNotFoundException($"Source file not found: {sourceFilePath}");

        var destinationDirectory = ResolveSafeDirectory(destinationFolder);

        if (!Directory.Exists(destinationDirectory))
            Directory.CreateDirectory(destinationDirectory);

        var destinationFullPath = ResolveSafePath(Path.Combine(destinationFolder ?? "", destinationFileName));
        File.Move(sourceFullPath, destinationFullPath, overwrite: true);

        _logger.LogInformation("Moved file: {Source} -> {Destination}", sourceFilePath, destinationFileName);

        var relativePath = string.IsNullOrEmpty(destinationFolder)
            ? destinationFileName
            : Path.Combine(destinationFolder, destinationFileName);

        return Task.FromResult(relativePath);
    }
}
