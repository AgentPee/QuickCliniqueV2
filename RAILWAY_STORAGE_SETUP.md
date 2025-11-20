# Railway Image Storage Setup Guide

## Problem
Railway's file system is **ephemeral**, meaning files saved to disk are lost when:
- The container restarts
- The application redeploys
- The service is updated

This means uploaded images (student IDs, staff IDs, insurance receipts) will be lost.

## Solution
This application now supports two storage providers:
1. **Local Storage** (Development) - Files saved to `wwwroot/img/`
2. **AWS S3** (Production) - Files saved to Amazon S3 bucket

## Setup Instructions

### Option 1: Use AWS S3 (Recommended for Production)

#### Step 1: Create an AWS S3 Bucket

1. Go to [AWS Console](https://console.aws.amazon.com/)
2. Navigate to **S3** service
3. Click **Create bucket**
4. Configure:
   - **Bucket name**: e.g., `quickclinique-uploads`
   - **Region**: Choose closest to your users (e.g., `us-east-1`)
   - **Block Public Access**: Uncheck to allow public read access (or configure bucket policy)
   - **Versioning**: Optional
5. Click **Create bucket**

#### Step 2: Configure Bucket Policy (for Public Read Access)

1. Go to your bucket → **Permissions** → **Bucket Policy**
2. Add this policy (replace `YOUR_BUCKET_NAME`):

```json
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Sid": "PublicReadGetObject",
            "Effect": "Allow",
            "Principal": "*",
            "Action": "s3:GetObject",
            "Resource": "arn:aws:s3:::YOUR_BUCKET_NAME/*"
        }
    ]
}
```

#### Step 3: Create IAM User for S3 Access

1. Go to **IAM** → **Users** → **Create user**
2. User name: `quickclinique-s3-user`
3. Select **Programmatic access**
4. Attach policy: `AmazonS3FullAccess` (or create custom policy with only S3 permissions)
5. **Save the Access Key ID and Secret Access Key** - you'll need these!

#### Step 4: Configure Railway Environment Variables

In your Railway project, go to your **Web Service** → **Variables** tab and add:

```
Storage__Provider=S3
Storage__S3__BucketName=your-bucket-name
Storage__S3__BaseUrl=https://your-bucket-name.s3.amazonaws.com
Storage__S3__Region=us-east-1
Storage__S3__AccessKeyId=YOUR_ACCESS_KEY_ID
Storage__S3__SecretAccessKey=YOUR_SECRET_ACCESS_KEY
```

**OR** use these environment variable names (alternative):

```
Storage__Provider=S3
AWS_S3_BUCKET_NAME=your-bucket-name
AWS_S3_BASE_URL=https://your-bucket-name.s3.amazonaws.com
AWS_ACCESS_KEY_ID=YOUR_ACCESS_KEY_ID
AWS_SECRET_ACCESS_KEY=YOUR_SECRET_ACCESS_KEY
```

**Important Notes:**
- Use double underscores (`__`) for nested configuration in Railway
- Replace `your-bucket-name` with your actual S3 bucket name
- Replace `us-east-1` with your bucket's region
- Keep your Access Key ID and Secret Access Key secure!

#### Step 5: Redeploy

After setting the environment variables, Railway will automatically redeploy your application. The app will now use S3 for file storage.

---

### Option 2: Use Local Storage (Development Only)

For local development, the default configuration uses local file storage. No additional setup needed.

To explicitly set it (optional):

```
Storage__Provider=Local
```

**⚠️ Warning:** Local storage will NOT work properly on Railway in production because files will be lost on restart/redeploy.

---

## Alternative: Other Cloud Storage Providers

If you prefer not to use AWS S3, you can implement support for:

- **Azure Blob Storage** - Similar to S3, Azure's object storage
- **Google Cloud Storage** - Google's equivalent
- **Cloudinary** - Image hosting service with built-in transformations
- **Railway Volumes** - Railway's persistent storage (if available in your plan)

To add support for another provider:
1. Create a new service class implementing `IFileStorageService`
2. Register it in `Program.cs` based on configuration
3. Update `appsettings.json` with the new provider's settings

---

## Testing

After setup, test by:
1. Uploading a student ID image during registration
2. Checking that the image URL is accessible
3. Verifying the image persists after a Railway restart/redeploy

---

## Troubleshooting

### Images not uploading
- Check Railway logs for errors
- Verify AWS credentials are correct
- Ensure S3 bucket exists and is accessible
- Check bucket policy allows public read access

### Images not displaying
- Verify the image URL is accessible in browser
- Check CORS settings if accessing from different domain
- Ensure bucket policy allows public read access

### "Storage provider not configured" error
- Verify `Storage__Provider` is set correctly
- Check all required environment variables are set
- Review Railway logs for configuration errors

---

## Security Best Practices

1. **Never commit AWS credentials** to version control
2. **Use IAM roles** instead of access keys when possible (if Railway supports it)
3. **Limit S3 permissions** to only what's needed (read/write to specific bucket)
4. **Enable S3 bucket versioning** for backup/recovery
5. **Set up S3 lifecycle policies** to manage old files
6. **Monitor S3 access** through CloudTrail

---

## Cost Considerations

- **AWS S3 Free Tier**: 5GB storage, 20,000 GET requests, 2,000 PUT requests per month
- **After free tier**: ~$0.023 per GB storage, $0.0004 per 1,000 GET requests
- For a small clinic, costs should be minimal (< $1/month typically)

---

## Migration from Local to S3

If you have existing images in local storage:
1. Set up S3 as described above
2. Manually upload existing images to S3 (or create a migration script)
3. Update database records with new S3 URLs
4. Switch to S3 provider in Railway

