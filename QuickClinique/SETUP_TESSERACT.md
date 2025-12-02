# Tesseract OCR Setup Guide

## Overview

The ID validation system requires Tesseract OCR to read and validate text from student ID images. This guide will help you set up Tesseract.NET.

## Quick Setup

### Step 1: Install Tesseract.NET Package

The package is already added to `QuickClinique.csproj`. If you need to add it manually:

```bash
dotnet add package Tesseract
```

### Step 2: Download Tesseract Language Data

1. Go to: https://github.com/tesseract-ocr/tessdata
2. Download `eng.traineddata` (English language data)
3. Create a folder named `tessdata` in your project root (same level as `QuickClinique.csproj`)
4. Place `eng.traineddata` in the `tessdata` folder

**Project Structure:**
```
QuickCliniqueV2/
  QuickClinique/
    tessdata/
      eng.traineddata
    QuickClinique.csproj
    ...
```

### Step 3: Verify Configuration

Ensure `appsettings.json` has:
```json
{
  "IdValidation": {
    "EnableOcr": true
  }
}
```

## What Gets Validated

### Front ID Image
- ✅ Must contain "University of Cebu" text (case-insensitive)
- ✅ Extracts and verifies ID number matches entered number

### Back ID Image
- ✅ Must contain "SCH. YR." column text
- ✅ Must show current year (e.g., 2024, 2025)
- ✅ Must have at least one signed/validated row for current year

## Troubleshooting

### Error: "Tesseract tessdata folder not found"

**Solution:**
1. Create `tessdata` folder in project root
2. Download `eng.traineddata` from https://github.com/tesseract-ocr/tessdata
3. Place file in `tessdata` folder
4. Ensure the file is named exactly `eng.traineddata`

### Error: "Tesseract.NET package is not installed"

**Solution:**
```bash
dotnet restore
dotnet add package Tesseract
```

### Error: "Could not read text from the ID image"

**Possible Causes:**
- Image is too blurry or low quality
- Image is too dark or has poor contrast
- Image format is not supported

**Solutions:**
- Ask user to upload a clearer, higher quality image
- Ensure good lighting when taking photo
- Use a scanner instead of camera if possible

### OCR Not Finding "University of Cebu" Text

**Possible Causes:**
- Text is not clearly visible in image
- Image is rotated or skewed
- Font is unusual or stylized

**Solutions:**
- Ensure image is clear and text is readable
- Try different image formats (PNG often works better than JPG for text)
- Check if image needs to be rotated

### OCR Not Finding "SCH. YR." or Current Year

**Possible Causes:**
- Back image is not clear
- Year format is different than expected
- Signature/validation mark is not visible

**Solutions:**
- Ensure back image is clear and well-lit
- Verify the ID shows current year
- Check that validation marks are visible

## Testing

To test if Tesseract is working:

1. Run the application
2. Try to register with a test student ID
3. Check the logs for OCR-related messages
4. If OCR fails, you'll see specific error messages

## Alternative: Using Cloud OCR Services

If Tesseract.NET doesn't work well, you can use cloud OCR services:

### Azure Computer Vision
- More accurate than Tesseract
- Requires Azure subscription
- API key needed

### Google Cloud Vision
- High accuracy
- Requires Google Cloud account
- API key needed

To use cloud services, modify `PerformOcrAsync` method in `IdValidationService.cs` to call your chosen service instead of Tesseract.

## Notes

- Tesseract works best with high-quality, clear images
- Text should be horizontal (not rotated)
- Good lighting improves accuracy
- PNG format often works better than JPG for text recognition

