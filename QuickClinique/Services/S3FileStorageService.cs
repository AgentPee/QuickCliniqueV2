using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Configuration;

namespace QuickClinique.Services;

public class S3FileStorageService : IFileStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly string _baseUrl;
    private readonly ILogger<S3FileStorageService> _logger;

    public S3FileStorageService(
        IAmazonS3 s3Client,
        IConfiguration configuration,
        ILogger<S3FileStorageService> logger)
    {
        _s3Client = s3Client;
        _bucketName = configuration["Storage:S3:BucketName"] 
            ?? Environment.GetEnvironmentVariable("AWS_S3_BUCKET_NAME")
            ?? throw new InvalidOperationException("Storage:S3:BucketName or AWS_S3_BUCKET_NAME is not configured");
        _baseUrl = configuration["Storage:S3:BaseUrl"] 
            ?? Environment.GetEnvironmentVariable("AWS_S3_BASE_URL")
            ?? $"https://{_bucketName}.s3.amazonaws.com";
        _logger = logger;
    }

    public async Task<string> UploadFileAsync(IFormFile file, string folder, string fileName)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is null or empty", nameof(file));
            }

            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fullFileName = $"{fileName}{fileExtension}";
            var s3Key = $"img/{folder}/{fullFileName}";

            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                var uploadRequest = new TransferUtilityUploadRequest
                {
                    InputStream = memoryStream,
                    Key = s3Key,
                    BucketName = _bucketName,
                    ContentType = file.ContentType,
                    CannedACL = S3CannedACL.PublicRead // Make files publicly accessible
                };

                var transferUtility = new TransferUtility(_s3Client);
                await transferUtility.UploadAsync(uploadRequest);

                var fileUrl = $"{_baseUrl}/{s3Key}";
                _logger.LogInformation("File uploaded to S3: {Url}", fileUrl);
                return fileUrl;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to S3");
            throw;
        }
    }

    public async Task<bool> DeleteFileAsync(string filePath)
    {
        try
        {
            // Extract S3 key from URL or use as-is if it's already a key
            var s3Key = ExtractS3Key(filePath);

            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = s3Key
            };

            await _s3Client.DeleteObjectAsync(deleteRequest);
            _logger.LogInformation("File deleted from S3: {Key}", s3Key);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file from S3: {Path}", filePath);
            return false;
        }
    }

    public async Task<bool> FileExistsAsync(string filePath)
    {
        try
        {
            var s3Key = ExtractS3Key(filePath);

            var request = new GetObjectMetadataRequest
            {
                BucketName = _bucketName,
                Key = s3Key
            };

            await _s3Client.GetObjectMetadataAsync(request);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking file existence in S3: {Path}", filePath);
            return false;
        }
    }

    private string ExtractS3Key(string filePath)
    {
        // If it's a full URL, extract the key part
        if (filePath.StartsWith("http://") || filePath.StartsWith("https://"))
        {
            var uri = new Uri(filePath);
            return uri.AbsolutePath.TrimStart('/');
        }

        // If it's a relative path starting with /img/, remove the leading slash
        if (filePath.StartsWith("/img/"))
        {
            return filePath.TrimStart('/');
        }

        // Otherwise assume it's already a key
        return filePath;
    }
}

