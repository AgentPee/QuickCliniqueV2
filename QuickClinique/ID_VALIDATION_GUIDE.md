# Student ID Validation Guide

## Overview

The ID validation system checks uploaded student ID images to ensure they are valid and meet quality requirements. This helps verify that students are uploading legitimate ID images during registration.

## Features

### 1. **File Format Validation**
- Validates that uploaded files are actual image files (not just renamed files)
- Checks file signatures (magic bytes) to detect:
  - JPEG/JPG
  - PNG
  - GIF
  - BMP
  - WEBP

### 2. **File Size Validation**
- **Minimum Size**: 50KB (configurable)
  - Files smaller than this are likely low quality or corrupted
- **Maximum Size**: 5MB (configurable)
  - Prevents excessively large files that could cause performance issues

### 3. **Image Quality Assessment**
- Calculates a quality score based on file size
- Larger files generally indicate better quality (higher resolution, less compression)
- Provides warnings for low-quality images

### 4. **OCR Validation (Required)**
- **Front Image**: Must contain "University of Cebu" text to verify it's a valid UC student ID
- **Back Image**: Must have "SCH. YR." column with at least one signed row for the current year
- **ID Number Matching**: Extracts and verifies the ID number matches the entered number
- Requires Tesseract.NET package and tessdata files

## Configuration

Add the following to your `appsettings.json`:

```json
{
  "IdValidation": {
    "EnableOcr": true,
    "MinImageQualityScore": 0.5,
    "MinImageSizeBytes": 5000,
    "MaxImageSizeBytes": 5242880
  }
}
```

### Configuration Options

- **EnableOcr**: Set to `true` to enable OCR validation (REQUIRED for ID validation). Default: `true`
- **MinImageQualityScore**: Minimum quality score (0.0 to 1.0). Default: 0.5
- **MinImageSizeBytes**: Minimum file size in bytes. Default: 5,000 (5KB)
- **MaxImageSizeBytes**: Maximum file size in bytes. Default: 5,242,880 (5MB)

## How It Works

### During Registration

1. When a student uploads ID images (front and back), the validation service is called
2. The service performs:
   - File format validation (checks magic bytes)
   - File size validation (min/max)
   - Quality score calculation
   - **Required OCR validation**:
     - **Front Image**: Checks for "University of Cebu" text
     - **Back Image**: Checks for "SCH. YR." column with signed row for current year
     - **ID Number**: Extracts and matches ID number

3. If validation fails:
   - Registration is **blocked**
   - User sees a specific error message explaining what failed
   - User must upload a valid University of Cebu student ID

4. If validation passes with warnings:
   - Registration continues
   - User sees a warning message (e.g., ID number mismatch)
   - Image is still accepted

## Setting Up OCR Validation

OCR validation is **REQUIRED** for ID verification. It validates:
- Front ID contains "University of Cebu" text
- Back ID has "SCH. YR." column with signed row for current year
- ID number matches the entered number

### Setup Steps

1. **Install Tesseract.NET Package** (already added to project):
   ```bash
   dotnet add package Tesseract
   ```
   Or the package is already included in `QuickClinique.csproj`

2. **Download Tesseract Language Data Files**:
   - Download from: https://github.com/tesseract-ocr/tessdata
   - You need the `eng.traineddata` file at minimum
   - Create a `tessdata` folder in your project root
   - Place `eng.traineddata` in the `tessdata` folder

3. **Verify Configuration**:
   - Ensure `EnableOcr` is set to `true` in `appsettings.json`
   - The service will automatically look for `tessdata` folder in:
     - Project root directory
     - Application base directory

### Option 2: Using Cloud OCR Services

You can integrate cloud OCR services like:
- Azure Computer Vision API
- Google Cloud Vision API
- AWS Textract

Modify `ValidateIdNumberWithOcrAsync` to call your chosen service.

## Validation Results

The validation service returns a `IdValidationResult` object with:

- **IsValid**: `true` if validation passed, `false` if it failed
- **ErrorMessage**: Error message if validation failed
- **WarningMessage**: Warning message if validation passed but with concerns
- **Details**: Detailed validation information including:
  - `ImageQualityPassed`: Whether quality check passed
  - `ImageQualityScore`: Quality score (0.0 to 1.0)
  - `IdNumberMatched`: Whether OCR matched the ID number (if OCR enabled)
  - `ExtractedIdNumber`: ID number extracted from image (if OCR enabled)

## Making OCR Validation Strict

By default, OCR mismatches are warnings (non-blocking). To make them hard failures:

1. Open `IdValidationService.cs`
2. In the `ValidateIdImageAsync` method, find the OCR validation section
3. Uncomment these lines:
   ```csharp
   // result.IsValid = false;
   // result.ErrorMessage = result.WarningMessage;
   ```

## Troubleshooting

### "Image file is too small" Error
- **Cause**: File is smaller than `MinImageSizeBytes`
- **Solution**: Upload a higher quality image or reduce `MinImageSizeBytes` in config

### "Image file is too large" Error
- **Cause**: File exceeds `MaxImageSizeBytes`
- **Solution**: Compress the image or increase `MaxImageSizeBytes` in config

### "Invalid image format" Error
- **Cause**: File is not a valid image or has wrong extension
- **Solution**: Ensure the file is a valid JPG, PNG, GIF, BMP, or WEBP image

### OCR Not Working
- **Cause**: OCR is disabled or not properly configured
- **Solution**: 
  - Check `EnableOcr` is set to `true` in config
  - Ensure Tesseract.NET is installed and configured
  - Check that `tessdata` folder is accessible

## Best Practices

1. **Set Appropriate Limits**: Adjust min/max file sizes based on your needs
2. **Monitor Quality Scores**: Review quality scores to adjust thresholds
3. **Enable OCR for Production**: Consider enabling OCR in production for better validation
4. **Provide Clear Error Messages**: Error messages guide users to fix issues
5. **Test with Various Images**: Test with different image qualities and formats

## Future Enhancements

Potential improvements:
- Image dimension validation (width/height checks)
- Blur detection
- Brightness/contrast analysis
- Face detection (verify ID contains a photo)
- Document structure validation (verify it looks like an ID card)

## Support

For issues or questions:
1. Check the logs for detailed error messages
2. Review the validation results in `IdValidationDetails`
3. Adjust configuration settings as needed

