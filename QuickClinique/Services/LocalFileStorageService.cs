using Microsoft.AspNetCore.Hosting;

namespace QuickClinique.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(IWebHostEnvironment webHostEnvironment, ILogger<LocalFileStorageService> logger)
    {
        _webHostEnvironment = webHostEnvironment;
        _logger = logger;
    }

    public async Task<string> UploadFileAsync(IFormFile file, string folder, string fileName)
    {
        try
        {
            // Validate file
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is null or empty", nameof(file));
            }

            // Get file extension
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fullFileName = $"{fileName}{fileExtension}";

            // Create uploads directory path
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "img", folder);

            // Ensure directory exists
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
                _logger.LogInformation("Created upload directory: {Directory}", uploadsFolder);
            }

            // Create full file path
            var filePath = Path.Combine(uploadsFolder, fullFileName);

            // Save file
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // Return relative path for web access
            var relativePath = $"/img/{folder}/{fullFileName}";
            _logger.LogInformation("File uploaded successfully: {Path}", relativePath);

            return relativePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to local storage");
            throw;
        }
    }

    public Task<bool> DeleteFileAsync(string filePath)
    {
        try
        {
            // Remove leading slash if present and convert to physical path
            var cleanPath = filePath.TrimStart('/');
            var physicalPath = Path.Combine(_webHostEnvironment.WebRootPath, cleanPath);

            if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
                _logger.LogInformation("File deleted: {Path}", physicalPath);
                return Task.FromResult(true);
            }

            _logger.LogWarning("File not found for deletion: {Path}", physicalPath);
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {Path}", filePath);
            return Task.FromResult(false);
        }
    }

    public Task<bool> FileExistsAsync(string filePath)
    {
        try
        {
            var cleanPath = filePath.TrimStart('/');
            var physicalPath = Path.Combine(_webHostEnvironment.WebRootPath, cleanPath);
            return Task.FromResult(File.Exists(physicalPath));
        }
        catch
        {
            return Task.FromResult(false);
        }
    }
}

