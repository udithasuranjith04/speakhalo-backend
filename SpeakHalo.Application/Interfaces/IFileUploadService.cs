using Microsoft.AspNetCore.Http;

namespace SpeakHalo.Application.Interfaces;

public interface IFileUploadService
{
    Task<string> UploadFileAsync(IFormFile file, string folder, CancellationToken cancellationToken = default);
    Task<string> UploadImageAsync(IFormFile file, string folder, int maxWidth = 1920, int maxHeight = 1080, CancellationToken cancellationToken = default);
    Task<bool> DeleteFileAsync(string filePath, CancellationToken cancellationToken = default);
    Task<byte[]> DownloadFileAsync(string filePath, CancellationToken cancellationToken = default);
    bool IsValidImageFile(IFormFile file);
    bool IsValidFileSize(IFormFile file, long maxSizeInBytes);
    string GetFileExtension(string fileName);
    string GenerateUniqueFileName(string originalFileName);
}
