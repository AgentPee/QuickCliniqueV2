using Microsoft.AspNetCore.Http;
using Google.Cloud.Vision.V1;
using Google.Api.Gax;
using Google.Apis.Auth.OAuth2;

namespace QuickClinique.Services
{
    /// <summary>
    /// Service for validating student ID images
    /// Validates image quality and uses Google Cloud Vision OCR to verify ID content
    /// </summary>
    public class IdValidationService : IIdValidationService
    {
        private readonly ILogger<IdValidationService> _logger;
        private readonly bool _enableOcrValidation;
        private readonly double _minImageQualityScore;
        private readonly long _minImageSizeBytes;
        private readonly long _maxImageSizeBytes;
        private readonly string? _googleCloudApiKey;
        private readonly int _maxOcrRetries;
        private readonly TimeSpan _ocrTimeout;
        private readonly TimeSpan _initialRetryDelay;

        public IdValidationService(
            ILogger<IdValidationService> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            
            // Configuration options - can be set in appsettings.json
            _enableOcrValidation = configuration.GetValue<bool>("IdValidation:EnableOcr", false);
            _minImageQualityScore = configuration.GetValue<double>("IdValidation:MinImageQualityScore", 0.5);
            _minImageSizeBytes = configuration.GetValue<long>("IdValidation:MinImageSizeBytes", 5000); // 5KB minimum
            _maxImageSizeBytes = configuration.GetValue<long>("IdValidation:MaxImageSizeBytes", 5 * 1024 * 1024); // 5MB maximum
            _googleCloudApiKey = configuration.GetValue<string>("IdValidation:GoogleCloudApiKey");
            
            // Retry configuration for OCR calls
            _maxOcrRetries = configuration.GetValue<int>("IdValidation:MaxOcrRetries", 3); // Retry up to 3 times
            _ocrTimeout = TimeSpan.FromSeconds(configuration.GetValue<int>("IdValidation:OcrTimeoutSeconds", 15)); // 15 seconds per attempt
            _initialRetryDelay = TimeSpan.FromSeconds(configuration.GetValue<int>("IdValidation:InitialRetryDelaySeconds", 2)); // 2 seconds initial delay
        }

        public async Task<IdValidationResult> ValidateIdImageAsync(IFormFile idImage, int expectedIdNumber, bool isFrontImage = true)
        {
            var result = new IdValidationResult
            {
                IsValid = true,
                Details = new IdValidationDetails()
            };

            try
            {
                // 1. Basic file validation
                if (idImage == null || idImage.Length == 0)
                {
                    result.IsValid = false;
                    result.ErrorMessage = "ID image file is empty or missing.";
                    return result;
                }

                // 2. Check minimum file size (too small might indicate poor quality)
                if (idImage.Length < _minImageSizeBytes)
                {
                    result.IsValid = false;
                    result.ErrorMessage = "ID image file is too small. Please ensure the image is clear and properly captured.";
                    return result;
                }

                // 3. Check maximum file size
                if (idImage.Length > _maxImageSizeBytes)
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"ID image file is too large. Maximum file size is {_maxImageSizeBytes / (1024 * 1024)}MB. Please compress or resize your image.";
                    return result;
                }

                // 4. Validate image format by checking file signature (magic bytes)
                var formatValidation = await ValidateImageFormatAsync(idImage);
                if (!formatValidation.IsValid)
                {
                    result.IsValid = false;
                    result.ErrorMessage = formatValidation.ErrorMessage ?? "Invalid image format. Please upload a valid image file (JPG, JPEG, PNG, GIF, BMP, or WEBP).";
                    return result;
                }

                // 5. Basic quality validation (file size indicates quality)
                // Larger files generally indicate better quality images
                var qualityScore = CalculateBasicQualityScore(idImage.Length);
                result.Details.ImageQualityScore = qualityScore;
                result.Details.ImageQualityPassed = qualityScore >= _minImageQualityScore;

                if (qualityScore < _minImageQualityScore)
                {
                    result.WarningMessage = "Image file size is small. Please ensure the image is clear and high quality.";
                    // You can make this a hard failure by uncommenting below:
                    // result.IsValid = false;
                    // result.ErrorMessage = "Image quality appears insufficient. Please upload a clearer, higher resolution image.";
                }

                // 6. OCR validation - REQUIRED for ID verification
                // This validates:
                // - Front image: Must contain "university of cebu" text
                // - Back image: Must contain academic year "2025-2026"
                // Copy image to byte array first so we can retry if needed
                byte[] imageBytes;
                using (var imageStream = new MemoryStream())
                {
                    await idImage.CopyToAsync(imageStream);
                    imageBytes = imageStream.ToArray();
                }
                
                var ocrResult = await ValidateIdContentWithOcrAsyncWithRetry(imageBytes, expectedIdNumber, isFrontImage);
                result.Details.IdNumberMatched = ocrResult.IdNumberMatched;
                result.Details.ExtractedIdNumber = ocrResult.ExtractedIdNumber;

                // OCR validation is required - failures block registration
                if (!ocrResult.IsValid)
                {
                    result.IsValid = false;
                    result.ErrorMessage = ocrResult.ErrorMessage ?? "ID validation failed. Please ensure you uploaded a valid University of Cebu student ID.";
                }
                else if (!string.IsNullOrEmpty(ocrResult.WarningMessage))
                {
                    result.WarningMessage = ocrResult.WarningMessage;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating ID image for ID number {IdNumber}", expectedIdNumber);
                result.IsValid = false;
                result.ErrorMessage = "An error occurred while validating the ID image. Please try again or contact support.";
            }

            return result;
        }

        /// <summary>
        /// Validates image format by checking file signature (magic bytes)
        /// This is more reliable than checking file extension
        /// </summary>
        private async Task<FormatValidationResult> ValidateImageFormatAsync(IFormFile imageFile)
        {
            var result = new FormatValidationResult { IsValid = false };

            try
            {
                using (var stream = imageFile.OpenReadStream())
                {
                    var buffer = new byte[12];
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                    if (bytesRead < 4)
                    {
                        result.ErrorMessage = "File is too small to be a valid image.";
                        return result;
                    }

                    // Check file signatures (magic bytes)
                    // JPEG: FF D8 FF
                    if (buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF)
                    {
                        result.IsValid = true;
                        return result;
                    }

                    // PNG: 89 50 4E 47 0D 0A 1A 0A
                    if (buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47)
                    {
                        result.IsValid = true;
                        return result;
                    }

                    // GIF: 47 49 46 38 (GIF8)
                    if (buffer[0] == 0x47 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x38)
                    {
                        result.IsValid = true;
                        return result;
                    }

                    // BMP: 42 4D (BM)
                    if (buffer[0] == 0x42 && buffer[1] == 0x4D)
                    {
                        result.IsValid = true;
                        return result;
                    }

                    // WEBP: Check for RIFF header and WEBP
                    if (bytesRead >= 12 && 
                        buffer[0] == 0x52 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x46 &&
                        buffer[8] == 0x57 && buffer[9] == 0x45 && buffer[10] == 0x42 && buffer[11] == 0x50)
                    {
                        result.IsValid = true;
                        return result;
                    }

                    result.ErrorMessage = "File format is not a supported image type. Please upload a JPG, PNG, GIF, BMP, or WEBP image.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating image format");
                result.ErrorMessage = "Unable to validate image format. Please ensure the file is a valid image.";
            }

            return result;
        }

        /// <summary>
        /// Calculates a basic quality score based on file size
        /// Larger files generally indicate better quality (higher resolution, less compression)
        /// </summary>
        private double CalculateBasicQualityScore(long fileSizeBytes)
        {
            // Normalize file size to a 0-1 score
            // Files between 200KB and 2MB get the best score
            // Files smaller than 50KB get lower scores
            if (fileSizeBytes < _minImageSizeBytes)
            {
                return fileSizeBytes / (double)_minImageSizeBytes * 0.5; // 0 to 0.5
            }
            else if (fileSizeBytes < 200 * 1024) // 200KB
            {
                return 0.5 + (fileSizeBytes - _minImageSizeBytes) / (200.0 * 1024 - _minImageSizeBytes) * 0.3; // 0.5 to 0.8
            }
            else if (fileSizeBytes <= 2 * 1024 * 1024) // 2MB
            {
                return 0.8 + (fileSizeBytes - 200 * 1024) / (2.0 * 1024 * 1024 - 200 * 1024) * 0.2; // 0.8 to 1.0
            }
            else
            {
                // Very large files might be unnecessarily large, but still good quality
                return 1.0;
            }
        }

        /// <summary>
        /// Validates ID content using OCR with retry logic
        /// Front image: Must contain "university of cebu" text
        /// Back image: Must contain academic year "2025-2026"
        /// </summary>
        private async Task<OcrContentValidationResult> ValidateIdContentWithOcrAsyncWithRetry(byte[] imageBytes, int expectedIdNumber, bool isFrontImage)
        {
            var result = new OcrContentValidationResult
            {
                IsValid = true,
                IdNumberMatched = null
            };

            try
            {
                // Check if OCR is enabled
                if (!_enableOcrValidation)
                {
                    _logger.LogWarning("OCR validation is disabled. Enable it in appsettings.json to validate ID content.");
                    // If OCR is disabled, we can't validate content, so we'll allow it but log a warning
                    result.WarningMessage = "OCR validation is disabled. ID content validation was skipped.";
                    return result;
                }

                // Perform OCR with retry logic to handle transient failures and timeouts
                // Retries up to _maxOcrRetries times (default: 3 retries = 4 total attempts)
                // Uses exponential backoff between retries (2s, 4s, 8s delays)
                // Each attempt has a timeout of _ocrTimeout (default: 15 seconds)
                // If all retries fail, registration proceeds with a warning instead of blocking
                string? extractedText = null;
                Exception? lastException = null;
                
                for (int attempt = 0; attempt <= _maxOcrRetries; attempt++)
                {
                    try
                    {
                        if (attempt > 0)
                        {
                            // Exponential backoff: 2s, 4s, 8s delays
                            var delay = TimeSpan.FromMilliseconds(_initialRetryDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
                            _logger.LogInformation("OCR attempt {Attempt} failed. Retrying after {Delay}ms delay...", attempt, delay.TotalMilliseconds);
                            await Task.Delay(delay);
                        }

                        // Create a new stream from the byte array for each attempt
                        using var imageStream = new MemoryStream(imageBytes);
                        
                        // Perform OCR with timeout
                        using var cts = new CancellationTokenSource(_ocrTimeout);
                        extractedText = await PerformOcrAsync(imageStream, cts.Token);
                        
                        // If we got text, break out of retry loop
                        if (!string.IsNullOrWhiteSpace(extractedText))
                        {
                            if (attempt > 0)
                            {
                                _logger.LogInformation("OCR succeeded on attempt {Attempt}", attempt + 1);
                            }
                            break;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        lastException = new TimeoutException($"OCR timed out after {_ocrTimeout.TotalSeconds} seconds");
                        _logger.LogWarning("OCR attempt {Attempt} timed out after {Timeout}s", attempt + 1, _ocrTimeout.TotalSeconds);
                        
                        // If this was the last attempt, we'll handle it below
                        if (attempt == _maxOcrRetries)
                        {
                            _logger.LogWarning("OCR validation timed out after {MaxRetries} retries. ID content validation was skipped.", _maxOcrRetries + 1);
                            result.WarningMessage = $"OCR validation timed out after {_maxOcrRetries + 1} attempts. ID content validation was skipped. Please ensure your ID images are valid.";
                            return result; // Return with IsValid = true, but with a warning
                        }
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        _logger.LogWarning(ex, "OCR attempt {Attempt} failed: {Error}", attempt + 1, ex.Message);
                        
                        // If this was the last attempt, we'll handle it below
                        if (attempt == _maxOcrRetries)
                        {
                            _logger.LogWarning("OCR validation failed after {MaxRetries} retries. ID content validation was skipped.", _maxOcrRetries + 1);
                            result.WarningMessage = "OCR validation is currently unavailable after multiple attempts. ID content validation was skipped. Please ensure your ID images are valid.";
                            return result; // Return with IsValid = true, but with a warning
                        }
                    }
                }
                
                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Could not read text from the ID image. Please ensure the image is clear and well-lit.";
                    return result;
                }

                // Normalize text for comparison (lowercase, remove extra spaces)
                var normalizedText = System.Text.RegularExpressions.Regex.Replace(
                    extractedText.ToLowerInvariant(), 
                    @"\s+", 
                    " "
                ).Trim();

                if (isFrontImage)
                {
                    // Front image validation: Must contain "university of cebu"
                    var universityKeywords = new[] { "University of Cebu", "university of cebu", "uc", "university cebu" };
                    var containsUniversity = universityKeywords.Any(keyword => 
                        normalizedText.Contains(keyword, StringComparison.OrdinalIgnoreCase));

                    if (!containsUniversity)
                    {
                        result.IsValid = false;
                        result.ErrorMessage = "The front ID image does not appear to be a University of Cebu student ID. Please upload a valid UC student ID.";
                        _logger.LogWarning("Front ID validation failed. Extracted text: {Text}", extractedText.Substring(0, Math.Min(200, extractedText.Length)));
                        return result;
                    }

                    // Also try to extract and match ID number from front
                    var idPattern = @"\b\d{8,}\b"; // 8+ digit number
                    var idMatch = System.Text.RegularExpressions.Regex.Match(extractedText, idPattern);
                    
                    if (idMatch.Success && int.TryParse(idMatch.Value, out int extractedId))
                    {
                        result.ExtractedIdNumber = idMatch.Value;
                        result.IdNumberMatched = extractedId == expectedIdNumber;
                        
                        if (result.IdNumberMatched == false)
                        {
                            result.WarningMessage = $"The ID number in the image ({extractedId}) does not match the entered ID number ({expectedIdNumber}).";
                        }
                    }
                }
                else
                {
                    // Back image validation: Must have academic year 2025-2026
                    var requiredAcademicYear = "2025-2026";
                    var yearPattern1 = @"2025\s*[-–—]\s*2026"; // Matches "2025-2026", "2025 - 2026", "2025–2026", etc.
                    var yearPattern2 = @"2025\s*/\s*2026"; // Matches "2025/2026"
                    var yearPattern3 = @"\b2025\b.*?\b2026\b"; // Matches "2025" and "2026" appearing together

                    // Check for academic year 2025-2026 in various formats
                    var containsAcademicYear = System.Text.RegularExpressions.Regex.IsMatch(
                        extractedText, 
                        yearPattern1, 
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase
                    ) || System.Text.RegularExpressions.Regex.IsMatch(
                        extractedText, 
                        yearPattern2, 
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase
                    ) || System.Text.RegularExpressions.Regex.IsMatch(
                        extractedText, 
                        yearPattern3, 
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase
                    );

                    if (!containsAcademicYear)
                    {
                        result.IsValid = false;
                        result.ErrorMessage = $"The back ID image does not show the academic year {requiredAcademicYear}. Please ensure your ID is valid for the {requiredAcademicYear} academic year.";
                        _logger.LogWarning("Back ID validation failed - academic year {Year} not found. Extracted text: {Text}", requiredAcademicYear, extractedText.Substring(0, Math.Min(200, extractedText.Length)));
                        return result;
                    }

                    result.IsValid = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OCR validation failed for {ImageType}", isFrontImage ? "front" : "back");
                // For errors, allow registration but log the error
                result.WarningMessage = "An error occurred during OCR validation. ID content validation was skipped. Please ensure your ID images are valid.";
            }

            return result;
        }

        /// <summary>
        /// Performs OCR on the image stream using Google Cloud Vision API
        /// </summary>
        private async Task<string> PerformOcrAsync(Stream imageStream, CancellationToken cancellationToken = default)
        {
            try
            {
                ImageAnnotatorClient client;
                
                // Check if GOOGLE_APPLICATION_CREDENTIALS contains JSON content (Railway) or is a file path (localhost)
                var credentialsEnv = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
                
                if (!string.IsNullOrWhiteSpace(credentialsEnv) && credentialsEnv.TrimStart().StartsWith("{"))
                {
                    // Railway: GOOGLE_APPLICATION_CREDENTIALS contains JSON content directly
                    _logger.LogDebug("Detected JSON credentials in GOOGLE_APPLICATION_CREDENTIALS (Railway mode)");
                    
                    try
                    {
                        // Parse JSON credentials and create client
                        var credential = GoogleCredential.FromJson(credentialsEnv);
                        var clientBuilder = new ImageAnnotatorClientBuilder
                        {
                            GoogleCredential = credential
                        };
                        client = await clientBuilder.BuildAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to parse JSON credentials from GOOGLE_APPLICATION_CREDENTIALS");
                        throw new InvalidOperationException("Invalid JSON credentials in GOOGLE_APPLICATION_CREDENTIALS environment variable.", ex);
                    }
                }
                else
                {
                    // Localhost: GOOGLE_APPLICATION_CREDENTIALS is a file path, or use default credentials
                    // This will use GOOGLE_APPLICATION_CREDENTIALS environment variable if set (as file path),
                    // or default service account credentials
                    client = await ImageAnnotatorClient.CreateAsync();
                }

                // Read image bytes
                imageStream.Position = 0;
                var imageBytes = new byte[imageStream.Length];
                await imageStream.ReadAsync(imageBytes, 0, (int)imageStream.Length);

                // Create image object - Image.FromBytes expects byte[] directly
                var image = Image.FromBytes(imageBytes);

                // Perform text detection with timeout (15 seconds max)
                // This prevents hanging when Google Cloud Vision is slow or unresponsive
                // Wrap the call in a timeout task since DetectTextAsync doesn't support cancellation tokens directly
                CancellationTokenSource timeoutCts;
                if (cancellationToken != default)
                {
                    timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    timeoutCts.CancelAfter(TimeSpan.FromSeconds(15));
                }
                else
                {
                    timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                }
                
                using (timeoutCts)
                {
                    // Use Task.WhenAny to implement timeout since DetectTextAsync doesn't support cancellation tokens
                    var detectTask = client.DetectTextAsync(image);
                    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(15), timeoutCts.Token);
                    
                    var completedTask = await Task.WhenAny(detectTask, timeoutTask);
                    if (completedTask == timeoutTask)
                    {
                        timeoutCts.Cancel();
                        throw new OperationCanceledException("OCR operation timed out after 15 seconds");
                    }
                    
                    var response = await detectTask;
                    
                    // Extract all text from annotations
                    // The first annotation contains the full text, others are individual words
                    var fullTextAnnotation = response.FirstOrDefault();
                    var extractedText = fullTextAnnotation?.Description ?? string.Empty;

                    _logger.LogDebug("Google Cloud Vision OCR extracted {Length} characters", extractedText.Length);

                    return extractedText;
                }

                // Extract all text from annotations
                // The first annotation contains the full text, others are individual words
                var fullTextAnnotation = response.FirstOrDefault();
                var extractedText = fullTextAnnotation?.Description ?? string.Empty;

                _logger.LogDebug("Google Cloud Vision OCR extracted {Length} characters", extractedText.Length);

                return extractedText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to perform OCR using Google Cloud Vision. Error: {ErrorMessage}", ex.Message);
                throw;
            }
        }

        private class FormatValidationResult
        {
            public bool IsValid { get; set; }
            public string? ErrorMessage { get; set; }
        }

        private class OcrContentValidationResult
        {
            public bool IsValid { get; set; }
            public string? ErrorMessage { get; set; }
            public string? WarningMessage { get; set; }
            public bool? IdNumberMatched { get; set; } // null = not checked, true = matched, false = not matched
            public string? ExtractedIdNumber { get; set; }
        }
    }
}

