# Google Cloud Vision API Setup Guide

This guide explains how to set up Google Cloud Vision API for OCR validation using **Service Account authentication** (Option 2 - Recommended for Production).

## Why Service Account?

- **More Secure**: Better for production environments
- **Better for Railway**: Works seamlessly with environment variables
- **No API Key Exposure**: Credentials are stored in a JSON file, not in code
- **Fine-grained Permissions**: You can limit what the service account can do
- **Free Tier**: 1,000 requests/month free

---

## Step-by-Step Setup

### Step 1: Create a Google Cloud Project

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Click on the project dropdown at the top
3. Click **"New Project"**
4. Enter a project name (e.g., "QuickClinique")
5. Click **"Create"**
6. Wait for the project to be created and select it

### Step 2: Enable Cloud Vision API

1. In the Google Cloud Console, go to **"APIs & Services"** > **"Library"**
2. Search for **"Cloud Vision API"**
3. Click on **"Cloud Vision API"**
4. Click **"Enable"**
5. Wait for the API to be enabled (usually takes a few seconds)

### Step 3: Create a Service Account

1. Go to **"APIs & Services"** > **"Credentials"**
2. Click **"+ CREATE CREDENTIALS"** at the top
3. Select **"Service account"**
4. Fill in the details:
   - **Service account name**: `quickclinique-vision` (or any name you prefer)
   - **Service account ID**: Will auto-populate (you can change it if needed)
   - **Description**: "Service account for QuickClinique OCR validation"
5. Click **"CREATE AND CONTINUE"**

### Step 4: Grant Permissions

1. In the **"Grant this service account access to project"** section:
   - **Role**: Select **"Cloud Vision API User"** or **"Cloud Vision API Client"**
   - This gives the service account permission to use the Vision API
2. Click **"CONTINUE"**
3. (Optional) Add users who can manage this service account
4. Click **"DONE"**

### Step 5: Create and Download JSON Key

1. In the **"Credentials"** page, find your service account in the list
2. Click on the service account email
3. Go to the **"KEYS"** tab
4. Click **"ADD KEY"** > **"Create new key"**
5. Select **"JSON"** as the key type
6. Click **"CREATE"**
7. A JSON file will be downloaded automatically - **SAVE THIS FILE SECURELY!**
   - The file will be named something like: `quickclinique-vision-xxxxx-xxxxx.json`
   - **DO NOT** commit this file to Git (it's already in .gitignore)
   - **DO NOT** share this file publicly

### Step 6: Understand the JSON File

The downloaded JSON file contains:
```json
{
  "type": "service_account",
  "project_id": "your-project-id",
  "private_key_id": "...",
  "private_key": "-----BEGIN PRIVATE KEY-----\n...\n-----END PRIVATE KEY-----\n",
  "client_email": "quickclinique-vision@your-project.iam.gserviceaccount.com",
  "client_id": "...",
  "auth_uri": "https://accounts.google.com/o/oauth2/auth",
  "token_uri": "https://oauth2.googleapis.com/token",
  "auth_provider_x509_cert_url": "https://www.googleapis.com/oauth2/v1/certs",
  "client_x509_cert_url": "..."
}
```

**Important**: Keep this file secure and never commit it to version control!

---

## Configuration for Localhost (Development)

### Option A: Environment Variable (Recommended)

1. **Windows (PowerShell)**:
   ```powershell
   $env:GOOGLE_APPLICATION_CREDENTIALS="D:\path\to\your\service-account-key.json"
   ```

2. **Windows (Command Prompt)**:
   ```cmd
   set GOOGLE_APPLICATION_CREDENTIALS=D:\path\to\your\service-account-key.json
   ```

3. **Linux/Mac**:
   ```bash
   export GOOGLE_APPLICATION_CREDENTIALS="/path/to/your/service-account-key.json"
   ```

4. **In Visual Studio/VS Code**:
   - Create or edit `launchSettings.json` in `Properties` folder
   - Add:
     ```json
     "environmentVariables": {
       "GOOGLE_APPLICATION_CREDENTIALS": "D:\\path\\to\\your\\service-account-key.json"
     }
     ```

### Option B: Place in Project Root (Not Recommended for Production)

1. Place the JSON file in your project root (same level as `QuickClinique.csproj`)
2. Name it: `google-credentials.json`
3. Add to `.gitignore`:
   ```
   google-credentials.json
   *.json
   !appsettings*.json
   ```
4. Update `appsettings.Development.json`:
   ```json
   {
     "IdValidation": {
       "GoogleCloudCredentialsPath": "google-credentials.json"
     }
   }
   ```

**Note**: The current implementation uses the `GOOGLE_APPLICATION_CREDENTIALS` environment variable, so Option A is recommended.

---

## Configuration for Railway (Production)

### Method 1: JSON Content in Environment Variable (Recommended) ✅

**The code automatically detects if `GOOGLE_APPLICATION_CREDENTIALS` contains JSON content (Railway) or is a file path (localhost).**

1. **In Railway Dashboard**:
   - Go to your project
   - Click on your service
   - Go to **"Variables"** tab
   - Click **"+ New Variable"**

2. **Add the JSON content as an environment variable**:
   - **Name**: `GOOGLE_APPLICATION_CREDENTIALS`
   - **Value**: Paste the **entire contents** of your JSON file (as a single line or multi-line)
   
   **Example** (paste the entire JSON):
   ```json
   {
     "type": "service_account",
     "project_id": "your-project-id",
     "private_key": "-----BEGIN PRIVATE KEY-----\n...\n-----END PRIVATE KEY-----\n",
     "client_email": "your-service-account@project.iam.gserviceaccount.com",
     ...
   }
   ```
   
   **Note**: The code will automatically detect that this is JSON content (starts with `{`) and parse it directly. No file path needed!

### Method 2: Base64 Encoded JSON (More Secure)

1. **Convert JSON to Base64**:
   ```powershell
   # Windows PowerShell
   $jsonContent = Get-Content "path\to\service-account-key.json" -Raw
   $bytes = [System.Text.Encoding]::UTF8.GetBytes($jsonContent)
   $base64 = [Convert]::ToBase64String($bytes)
   Write-Host $base64
   ```
   
   ```bash
   # Linux/Mac
   base64 -i service-account-key.json
   ```

2. **In Railway**:
   - **Name**: `GOOGLE_APPLICATION_CREDENTIALS_BASE64`
   - **Value**: Paste the Base64 string

3. **Update the code** to decode Base64 (optional - see below)

### Method 3: Railway Secrets (If Available)

Some Railway plans support secrets management. Check Railway documentation for this feature.

---

## Updating Code for Base64 Support (Optional)

If you want to support Base64-encoded credentials, you can update `IdValidationService.cs`:

```csharp
private ImageAnnotatorClient CreateVisionClient()
{
    // Check for Base64 encoded credentials first (Railway)
    var base64Credentials = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS_BASE64");
    if (!string.IsNullOrWhiteSpace(base64Credentials))
    {
        try
        {
            var jsonBytes = Convert.FromBase64String(base64Credentials);
            var jsonContent = System.Text.Encoding.UTF8.GetString(jsonBytes);
            
            // Create client from JSON string
            var clientBuilder = new ImageAnnotatorClientBuilder();
            clientBuilder.JsonCredentials = jsonContent;
            return clientBuilder.Build();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decode Base64 credentials");
            throw;
        }
    }
    
    // Fall back to standard GOOGLE_APPLICATION_CREDENTIALS
    return ImageAnnotatorClient.Create();
}
```

**Note**: The current implementation automatically detects whether `GOOGLE_APPLICATION_CREDENTIALS` contains JSON content (Railway) or is a file path (localhost). Method 1 works out of the box - just paste your JSON content directly into the Railway environment variable!

---

## Testing the Setup

### Test Locally

1. Set the environment variable:
   ```powershell
   $env:GOOGLE_APPLICATION_CREDENTIALS="D:\path\to\service-account-key.json"
   ```

2. Run your application:
   ```bash
   dotnet run
   ```

3. Try registering a student with ID images
4. Check logs for: `"Google Cloud Vision OCR extracted X characters"`

### Test on Railway

1. Deploy your application to Railway
2. Set the `GOOGLE_APPLICATION_CREDENTIALS` environment variable with your JSON content
3. Redeploy
4. Test registration with ID images
5. Check Railway logs for any errors

---

## Troubleshooting

### Error: "The Application Default Credentials are not available"

**Solution**: Make sure `GOOGLE_APPLICATION_CREDENTIALS` environment variable is set correctly.

### Error: "Permission denied" or "Access denied"

**Solution**: 
1. Check that the service account has the **"Cloud Vision API User"** role
2. Verify the Cloud Vision API is enabled in your project
3. Check that the JSON key file is valid

### Error: "Invalid JSON"

**Solution**: 
- If using Base64, make sure you decoded it correctly
- If using environment variable, ensure the JSON is properly formatted (escape quotes if needed)

### Error: "Quota exceeded"

**Solution**: 
- You've exceeded the free tier (1,000 requests/month)
- Check your usage in Google Cloud Console
- Consider upgrading to a paid plan

---

## Security Best Practices

1. ✅ **Never commit** the JSON key file to Git
2. ✅ **Use environment variables** instead of hardcoding
3. ✅ **Rotate keys** periodically (every 90 days recommended)
4. ✅ **Limit permissions** - only grant the minimum required roles
5. ✅ **Monitor usage** in Google Cloud Console
6. ✅ **Use different service accounts** for development and production
7. ✅ **Enable audit logging** to track API usage

---

## Cost Information

- **Free Tier**: First 1,000 requests/month are free
- **Pricing**: After free tier, ~$1.50 per 1,000 requests
- **Monitoring**: Check usage in Google Cloud Console > Billing

---

## Additional Resources

- [Google Cloud Vision API Documentation](https://cloud.google.com/vision/docs)
- [Service Account Best Practices](https://cloud.google.com/iam/docs/best-practices-service-accounts)
- [Railway Environment Variables](https://docs.railway.app/develop/variables)

---

## Quick Reference

**For Localhost:**
```powershell
$env:GOOGLE_APPLICATION_CREDENTIALS="D:\path\to\service-account-key.json"
```

**For Railway:**
- Variable Name: `GOOGLE_APPLICATION_CREDENTIALS`
- Variable Value: Full JSON content (or Base64 encoded)

**Leave in appsettings.json:**
```json
"GoogleCloudApiKey": ""  // Leave empty when using service account
```

