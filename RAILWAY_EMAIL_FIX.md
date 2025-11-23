# Fix Email Not Working on Railway

## Problem
Email works fine on localhost but not on Railway. This is because Railway doesn't have the `SMTP_PASSWORD` environment variable set.

## Solution: Add SMTP_PASSWORD to Railway

### Step 1: Get Your SendGrid API Key

If you don't have it:
1. Go to https://app.sendgrid.com
2. Settings → API Keys
3. Create a new API key or copy an existing one
4. The key should start with `SG.`

### Step 2: Add Environment Variable in Railway

1. **Go to Railway Dashboard:**
   - https://railway.app
   - Select your **QuickClinique project**
   - Click on your **Web Service** (the one running your app)

2. **Go to Variables Tab:**
   - Click on **"Variables"** tab
   - Click **"+ New Variable"** or **"Raw Editor"**

3. **Add the SMTP Password:**
   ```
   Variable Name: SMTP_PASSWORD
   Value: SG.your-sendgrid-api-key-here
   ```
   
   **Important:** Replace `SG.your-sendgrid-api-key-here` with your actual SendGrid API key!

4. **Optional - Add Other Email Settings (if needed):**
   ```
   EMAIL_FROM=quickclinique25@gmail.com
   SMTP_SERVER=smtp.sendgrid.net
   SMTP_PORT=587
   SMTP_USERNAME=apikey
   ```
   
   **Note:** These are optional because they're already in `appsettings.json`, but you can override them if needed.

5. **Save:**
   - Click **"Add"** or **"Save"**
   - Railway will automatically redeploy your service

### Step 3: Verify Configuration

After Railway redeploys:

1. **Check Railway Logs:**
   - Go to your service → **"Deployments"** tab
   - Click on the latest deployment
   - Click **"View Logs"**
   - Look for email-related messages:
     - `[EMAIL] Attempting to send email to: ...`
     - `[EMAIL SUCCESS]` = Working! ✅
     - `[EMAIL ERROR]` = Check the error message

2. **Test Email Sending:**
   - Try registering a new account
   - Try confirming an appointment
   - Check if emails are received

## Quick Checklist

Make sure these are set in Railway Variables:

- [ ] `SMTP_PASSWORD` = `SG.your-actual-api-key` (REQUIRED)
- [ ] `EMAIL_FROM` = `quickclinique25@gmail.com` (optional)
- [ ] `SMTP_SERVER` = `smtp.sendgrid.net` (optional)
- [ ] `SMTP_PORT` = `587` (optional)
- [ ] `SMTP_USERNAME` = `apikey` (optional)

## Why It Works Locally But Not on Railway

- **Localhost:** Uses `appsettings.Development.json` which has your API key
- **Railway:** Uses environment variables (which are empty) → Falls back to `appsettings.json` (which has empty password) → Error!

## Troubleshooting

### Still Not Working?

1. **Check Railway Logs:**
   - Look for `[EMAIL ERROR]` messages
   - Common errors:
     - `SmtpPassword is not configured` → Variable not set correctly
     - `Authentication failed` → Wrong API key
     - `The from address does not match` → Sender email not verified in SendGrid

2. **Verify Environment Variable:**
   - In Railway → Your Service → Variables
   - Make sure `SMTP_PASSWORD` exists and has the correct value
   - The value should start with `SG.`

3. **Redeploy:**
   - After adding/changing variables, Railway should auto-redeploy
   - If not, manually trigger a redeploy

4. **Check SendGrid:**
   - Ensure your sender email (`quickclinique25@gmail.com`) is verified
   - Verify the API key has "Mail Send" permissions
   - Check SendGrid Activity Feed for email attempts

### Common Errors

**Error: `[EMAIL ERROR] SmtpPassword is not configured`**
- **Fix:** Add `SMTP_PASSWORD` environment variable in Railway

**Error: `Authentication failed`**
- **Fix:** Check that `SMTP_USERNAME` is exactly `"apikey"` (lowercase)
- **Fix:** Verify the API key is correct

**Error: `The from address does not match a verified Sender Identity`**
- **Fix:** Verify `quickclinique25@gmail.com` in SendGrid Dashboard → Settings → Sender Authentication

## Using Railway CLI (Alternative)

If you prefer command line:

```bash
# Install Railway CLI
npm i -g @railway/cli

# Login
railway login

# Link your project
railway link

# Set SMTP password
railway variables set SMTP_PASSWORD="SG.your-api-key-here"

# Verify
railway variables
```

## Security Note

- ✅ Environment variables in Railway are secure and encrypted
- ✅ Never commit API keys to Git (you've already protected `appsettings.Development.json`)
- ✅ Railway variables are only visible to project members

