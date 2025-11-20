namespace QuickClinique.Services;

public interface IFileStorageService
{
    /// <summary>
    /// Uploads a file and returns the URL/path to access it
    /// </summary>
    /// <param name="file">The file to upload</param>
    /// <param name="folder">The folder/subdirectory to store the file in (e.g., "student-ids", "staff-ids", "insurance-receipts")</param>
    /// <param name="fileName">The desired file name (without extension)</param>
    /// <returns>The URL or path to access the uploaded file</returns>
    Task<string> UploadFileAsync(IFormFile file, string folder, string fileName);

    /// <summary>
    /// Deletes a file
    /// </summary>
    /// <param name="filePath">The path or URL of the file to delete</param>
    /// <returns>True if deletion was successful, false otherwise</returns>
    Task<bool> DeleteFileAsync(string filePath);

    /// <summary>
    /// Checks if a file exists
    /// </summary>
    /// <param name="filePath">The path or URL of the file to check</param>
    /// <returns>True if file exists, false otherwise</returns>
    Task<bool> FileExistsAsync(string filePath);
}

