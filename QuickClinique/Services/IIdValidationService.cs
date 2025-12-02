using Microsoft.AspNetCore.Http;

namespace QuickClinique.Services
{
    /// <summary>
    /// Service for validating student ID images
    /// </summary>
    public interface IIdValidationService
    {
        /// <summary>
        /// Validates if the uploaded ID image is valid and matches the provided ID number
        /// </summary>
        /// <param name="idImage">The uploaded ID image file</param>
        /// <param name="expectedIdNumber">The ID number that should match the ID in the image</param>
        /// <param name="isFrontImage">True if this is the front image, false if back image</param>
        /// <returns>Validation result with success status and error message if validation fails</returns>
        Task<IdValidationResult> ValidateIdImageAsync(IFormFile idImage, int expectedIdNumber, bool isFrontImage = true);
    }

    /// <summary>
    /// Result of ID validation
    /// </summary>
    public class IdValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public string? WarningMessage { get; set; }
        public IdValidationDetails? Details { get; set; }
    }

    /// <summary>
    /// Detailed validation information
    /// </summary>
    public class IdValidationDetails
    {
        public bool ImageQualityPassed { get; set; }
        public bool? IdNumberMatched { get; set; } // null = not checked, true = matched, false = not matched
        public string? ExtractedIdNumber { get; set; }
        public double? ImageQualityScore { get; set; }
        public string? QualityIssues { get; set; }
    }
}

