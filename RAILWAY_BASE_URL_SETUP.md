# Setting Base URL for Email Verification Links

## Problem
Email verification links are using `localhost` instead of your Railway URL.

## Solution
Set the `BASE_URL` environment variable in Railway to your production URL.

## Step 1: Get Your Railway URL

1. Go to your Railway project dashboard
2. Click on your **Web Service**
3. Go to the **Settings** tab
4. Find your **Public Domain** or **Custom Domain**
   - Example: `https://your-app-name.up.railway.app`
   - Or your custom domain: `https://yourdomain.com`

## Step 2: Set BASE_URL Environment Variable

1. In your Railway project, click on your **Web Service**
2. Go to the **Variables** tab
3. Click **"New Variable"** or **"Raw Editor"**
4. Add:

```
Variable Name: BASE_URL
Value: https://your-app-name.up.railway.app
```

**Important:**
- Use `https://` (not `http://`)
- Don't include a trailing slash (`/`)
- Use your actual Railway URL or custom domain

## Step 3: Redeploy

After adding the variable, Railway will automatically redeploy your app. The verification links in emails will now use your Railway URL instead of localhost.

## How It Works

The application checks for `BASE_URL` environment variable first:
- ✅ If `BASE_URL` is set → Uses that URL
- ✅ If not set → Falls back to the request's scheme and host

## Testing

After deployment:
1. Register a new user
2. Check the verification email
3. The link should now use your Railway URL (not localhost)

## Example

**Before:**
```
http://localhost:5097/Student/VerifyEmail?token=...
```

**After (with BASE_URL set):**
```
https://your-app-name.up.railway.app/Student/VerifyEmail?token=...
```

## Troubleshooting

### Links still show localhost?

1. **Check the variable name:** Must be exactly `BASE_URL` (case-sensitive)
2. **Check the value:** Should be `https://your-url` (no trailing slash)
3. **Redeploy:** Make sure Railway redeployed after adding the variable
4. **Check logs:** Look for any errors in Railway logs

### Using a Custom Domain?

If you have a custom domain:
```
BASE_URL=https://yourdomain.com
```

Make sure your custom domain is properly configured in Railway.

