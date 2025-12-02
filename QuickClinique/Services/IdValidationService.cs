using Microsoft.AspNetCore.Http;

namespace QuickClinique.Services
{
    /// <summary>
    /// Service for validating student ID images
    /// Validates image quality and optionally uses OCR to verify ID number matches
    /// </summary>
    public class IdValidationService : IIdValidationService
    {
        private readonly ILogger<IdValidationService> _logger;
        private readonly bool _enableOcrValidation;
        private readonly double _minImageQualityScore;
        private readonly long _minImageSizeBytes;
        private readonly long _maxImageSizeBytes;

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
                using (var imageStream = new MemoryStream())
                {
                    await idImage.CopyToAsync(imageStream);
                    imageStream.Position = 0;
                    
                    var ocrResult = await ValidateIdContentWithOcrAsync(imageStream, expectedIdNumber, isFrontImage);
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
        /// Validates ID content using OCR
        /// Front image: Must contain "university of cebu" text
        /// Back image: Must contain academic year "2025-2026"
        /// </summary>
        private async Task<OcrContentValidationResult> ValidateIdContentWithOcrAsync(Stream imageStream, int expectedIdNumber, bool isFrontImage)
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

                // Perform OCR using Tesseract.NET
                string extractedText = await PerformOcrAsync(imageStream);
                
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
                result.IsValid = false;
                result.ErrorMessage = "An error occurred while validating the ID image. Please try again or contact support.";
            }

            return result;
        }

        /// <summary>
        /// Performs OCR on the image stream using Tesseract.NET
        /// </summary>
        private async Task<string> PerformOcrAsync(Stream imageStream)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Find tessdata folder
                    var tessdataPath = Path.Combine(Directory.GetCurrentDirectory(), "tessdata");
                    
                    // If tessdata folder doesn't exist, try common alternative locations
                    if (!Directory.Exists(tessdataPath))
                    {
                        tessdataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
                    }
                    
                    if (!Directory.Exists(tessdataPath))
                    {
                        _logger.LogError("Tesseract tessdata folder not found. Please download tessdata files from https://github.com/tesseract-ocr/tessdata and place them in a 'tessdata' folder in your project root.");
                        throw new InvalidOperationException("Tesseract OCR is not properly configured. Please download tessdata files from https://github.com/tesseract-ocr/tessdata and place them in a 'tessdata' folder in your project root.");
                    }

                    // Try to use Tesseract.NET with direct references
                    // First, try to find the assembly by checking loaded assemblies
                    System.Reflection.Assembly? tesseractAssembly = null;
                    
                    // Check already loaded assemblies
                    tesseractAssembly = AppDomain.CurrentDomain.GetAssemblies()
                        .FirstOrDefault(a => 
                            a.GetName().Name == "Tesseract" || 
                            a.GetName().Name == "TesseractOCR" ||
                            a.FullName?.Contains("Tesseract") == true);

                    if (tesseractAssembly == null)
                    {
                        // Try to load the assembly explicitly using different approaches
                        var assemblyNames = new[] { 
                            "Tesseract, Version=5.2.0.0, Culture=neutral, PublicKeyToken=null",
                            "Tesseract",
                            "TesseractOCR",
                            "Tesseract.NET"
                        };
                        
                        foreach (var name in assemblyNames)
                        {
                            try
                            {
                                tesseractAssembly = System.Reflection.Assembly.Load(name);
                                if (tesseractAssembly != null)
                                {
                                    _logger.LogInformation("Successfully loaded Tesseract assembly: {AssemblyName}", tesseractAssembly.FullName);
                                    break;
                                }
                            }
                            catch (Exception loadEx)
                            {
                                _logger.LogDebug("Failed to load assembly '{Name}': {Error}", name, loadEx.Message);
                            }
                        }
                        
                        // If still not found, try loading from the application's base directory
                        if (tesseractAssembly == null)
                        {
                            try
                            {
                                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                                var possiblePaths = new[]
                                {
                                    Path.Combine(baseDir, "Tesseract.dll"),
                                    Path.Combine(baseDir, "lib", "netstandard2.0", "Tesseract.dll"),
                                    Path.Combine(baseDir, "runtimes", "win-x64", "native", "Tesseract.dll")
                                };
                                
                                foreach (var path in possiblePaths)
                                {
                                    if (File.Exists(path))
                                    {
                                        try
                                        {
                                            tesseractAssembly = System.Reflection.Assembly.LoadFrom(path);
                                            if (tesseractAssembly != null)
                                            {
                                                _logger.LogInformation("Successfully loaded Tesseract assembly from: {Path}", path);
                                                break;
                                            }
                                        }
                                        catch (Exception loadEx)
                                        {
                                            _logger.LogDebug("Failed to load from path '{Path}': {Error}", path, loadEx.Message);
                                        }
                                    }
                                }
                            }
                            catch (Exception pathEx)
                            {
                                _logger.LogDebug("Error searching for Tesseract DLL: {Error}", pathEx.Message);
                            }
                        }
                    }

                    if (tesseractAssembly == null)
                    {
                        _logger.LogError("Tesseract.NET assembly not found. Please ensure the package is installed: dotnet add package Tesseract");
                        throw new InvalidOperationException("Tesseract.NET package is not installed. Please install it using: dotnet add package Tesseract");
                    }

                    // Use reflection to load Tesseract types
                    var engineType = tesseractAssembly.GetType("Tesseract.TesseractEngine");
                    if (engineType == null)
                    {
                        engineType = tesseractAssembly.GetType("TesseractEngine");
                    }

                    var pixType = tesseractAssembly.GetType("Tesseract.Pix");
                    if (pixType == null)
                    {
                        pixType = tesseractAssembly.GetType("Pix");
                    }

                    var pageType = tesseractAssembly.GetType("Tesseract.Page");
                    if (pageType == null)
                    {
                        pageType = tesseractAssembly.GetType("Page");
                    }

                    if (engineType == null || pixType == null || pageType == null)
                    {
                        _logger.LogError("Tesseract.NET types not found. Assembly: {AssemblyName}, Types: Engine={Engine}, Pix={Pix}, Page={Page}", 
                            tesseractAssembly.FullName, engineType != null, pixType != null, pageType != null);
                        throw new InvalidOperationException("Tesseract.NET types not found. Please ensure the package is properly installed and restored.");
                    }

                    // Get EngineMode enum
                    var engineModeType = tesseractAssembly.GetType("Tesseract.EngineMode") ?? 
                                        tesseractAssembly.GetType("EngineMode");
                    var defaultMode = engineModeType != null 
                        ? Enum.Parse(engineModeType, "Default") 
                        : 0; // Fallback to 0 if enum not found

                    // Create engine
                    var engine = Activator.CreateInstance(engineType, tessdataPath, "eng", defaultMode);
                    if (engine == null)
                    {
                        throw new InvalidOperationException("Failed to create Tesseract engine.");
                    }
                    
                    // Get PageSegMode enum type (needed for Process method)
                    var pageSegModeType = tesseractAssembly.GetType("Tesseract.PageSegMode") ?? 
                                         tesseractAssembly.GetType("PageSegMode");

                    // Load image
                    imageStream.Position = 0;
                    var imageBytes = new byte[imageStream.Length];
                    imageStream.Read(imageBytes, 0, (int)imageStream.Length);

                    // Load image using Pix.LoadFromMemory
                    var loadFromMemoryMethod = pixType.GetMethod("LoadFromMemory", 
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, 
                        null, 
                        new[] { typeof(byte[]) }, 
                        null);
                    
                    if (loadFromMemoryMethod == null)
                    {
                        throw new InvalidOperationException("Tesseract Pix.LoadFromMemory method not found.");
                    }

                    var image = loadFromMemoryMethod.Invoke(null, new object[] { imageBytes });
                    if (image == null)
                    {
                        throw new InvalidOperationException("Failed to load image for OCR.");
                    }

                    // Process image - Process method requires (Pix, Nullable<PageSegMode>)
                    // Use the simplest overload: Process(Pix, Nullable<PageSegMode>) with null for PageSegMode
                    System.Reflection.MethodInfo? processMethod = null;
                    
                    if (pageSegModeType != null)
                    {
                        // Create Nullable<PageSegMode> type
                        var nullablePageSegModeType = typeof(Nullable<>).MakeGenericType(pageSegModeType);
                        
                        // Try to find Process(Pix, Nullable<PageSegMode>)
                        processMethod = engineType.GetMethod("Process", 
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                            null,
                            new[] { pixType, nullablePageSegModeType },
                            null);
                    }
                    
                    // If not found, try to get all Process methods and find the one that takes Pix and Nullable
                    if (processMethod == null)
                    {
                        var allProcessMethods = engineType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                            .Where(m => m.Name == "Process")
                            .ToList();
                        
                        foreach (var method in allProcessMethods)
                        {
                            var parameters = method.GetParameters();
                            // Look for Process(Pix, Nullable<PageSegMode>) - 2 parameters
                            if (parameters.Length == 2 && 
                                parameters[0].ParameterType == pixType &&
                                parameters[1].ParameterType.IsGenericType &&
                                parameters[1].ParameterType.GetGenericTypeDefinition() == typeof(Nullable<>))
                            {
                                processMethod = method;
                                break;
                            }
                        }
                    }
                    
                    if (processMethod == null)
                    {
                        // Log available methods for debugging
                        var availableMethods = string.Join(", ", engineType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                            .Where(m => m.Name == "Process")
                            .Select(m => $"{m.Name}({string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name))})"));
                        
                        _logger.LogError("Tesseract Engine.Process method not found. Available Process methods: {Methods}", availableMethods);
                        throw new InvalidOperationException($"Tesseract Engine.Process method not found. Available Process methods: {availableMethods}");
                    }

                    // Invoke Process with Pix and null (for PageSegMode)
                    var page = processMethod.Invoke(engine, new object[] { image, null! });
                    if (page == null)
                    {
                        throw new InvalidOperationException("Failed to process image with OCR.");
                    }

                    // Get text
                    var getTextMethod = pageType.GetMethod("GetText");
                    if (getTextMethod == null)
                    {
                        throw new InvalidOperationException("Tesseract Page.GetText method not found.");
                    }

                    var text = getTextMethod.Invoke(page, null) as string;

                    // Dispose resources
                    if (page is IDisposable pageDisposable) pageDisposable.Dispose();
                    if (image is IDisposable imageDisposable) imageDisposable.Dispose();
                    if (engine is IDisposable engineDisposable) engineDisposable.Dispose();

                    return text ?? string.Empty;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to perform OCR. Error: {ErrorMessage}, StackTrace: {StackTrace}", ex.Message, ex.StackTrace);
                    
                    // Provide helpful error message
                    if (ex.Message.Contains("Tesseract") || ex.Message.Contains("tessdata") || ex.InnerException?.Message.Contains("Tesseract") == true)
                    {
                        throw new InvalidOperationException(
                            "OCR validation requires Tesseract.NET. " +
                            "Please ensure:\n" +
                            "1. Package is installed: dotnet add package Tesseract\n" +
                            "2. Packages are restored: dotnet restore\n" +
                            "3. Tessdata files are downloaded from: https://github.com/tesseract-ocr/tessdata\n" +
                            "4. Tessdata folder is placed in your project root\n" +
                            $"Error: {ex.Message}", ex);
                    }
                    
                    throw;
                }
            });
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

