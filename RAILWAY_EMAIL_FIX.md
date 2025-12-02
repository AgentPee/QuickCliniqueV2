# Fix Email Not Working on Railway

## Problem
Email works fine on localhost but not on Railway. This is because Railway doesn't have the `RESEND_API_KEY` environment variable set.

## Solution: Add RESEND_API_KEY to Railway

### Step 1: Get Your Resend API Key

If you don't have it:
1. Go to https://resend.com
2. Sign up or log in to your account
3. Navigate to API Keys section
4. Create a new API key or copy an existing one
5. The key should start with `re_`

### Step 2: Add Environment Variable in Railway

1. **Go to Railway Dashboard:**
   - https://railway.app
   - Select your **QuickClinique project**
   - Click on your **Web Service** (the one running your app)

2. **Go to Variables Tab:**
   - Click on **"Variables"** tab
   - Click **"+ New Variable"** or **"Raw Editor"**

3. **Add the Resend API Key:**
   ```
   Variable Name: RESEND_API_KEY
   Value: re_your-resend-api-key-here
   ```
   
   **Important:** Replace `re_your-resend-api-key-here` with your actual Resend API key!
   
   **Note:** For backward compatibility, you can also use `SMTP_PASSWORD` instead of `RESEND_API_KEY`.

4. **Optional - Add Other Email Settings (if needed):**
   ```
   EMAIL_FROM=noreply@quickclinique.site
   EMAIL_FROM_NAME=QuickClinique
   EMAIL_REPLY_TO=noreply@quickclinique.site
   ```
   
   **Note:** These are optional because they're already in `appsettings.json` (defaults to `noreply@quickclinique.site`), but you can override them if needed.

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

- [ ] `RESEND_API_KEY` = `re_your-actual-api-key` (REQUIRED)
- [ ] `EMAIL_FROM` = `noreply@quickclinique.site` (optional, defaults to this)
- [ ] `EMAIL_FROM_NAME` = `QuickClinique` (optional)
- [ ] `EMAIL_REPLY_TO` = `noreply@quickclinique.site` (optional, defaults to EMAIL_FROM)

## Why It Works Locally But Not on Railway

- **Localhost:** Uses `appsettings.Development.json` which has your API key
- **Railway:** Uses environment variables (which are empty) → Falls back to `appsettings.json` (which has empty password) → Error!

## Troubleshooting

### Still Not Working?

1. **Check Railway Logs:**
   - Look for `[EMAIL ERROR]` messages
   - Common errors:
     - `Resend API key is not configured` → Variable not set correctly
     - `Unauthorized` → Wrong API key
     - `Domain verification issue` → Sender email/domain not verified in Resend

2. **Verify Environment Variable:**
   - In Railway → Your Service → Variables
   - Make sure `RESEND_API_KEY` exists and has the correct value
   - The value should start with `re_`

3. **Redeploy:**
   - After adding/changing variables, Railway should auto-redeploy
   - If not, manually trigger a redeploy

4. **Check Resend:**
   - Ensure your sender email/domain is verified in Resend Dashboard → Domains
   - Verify the API key is active
   - Check Resend dashboard for email attempts and logs

### Common Errors

**Error: `[EMAIL ERROR] Resend API key is not configured`**
- **Fix:** Add `RESEND_API_KEY` environment variable in Railway

**Error: `Unauthorized` or `401`**
- **Fix:** Verify the API key is correct and active
- **Fix:** Check that the API key starts with `re_`

**Error: `Domain verification issue`**
- **Fix:** Ensure `quickclinique.site` is verified in Resend Dashboard → Domains
- **Fix:** The default `noreply@quickclinique.site` uses your verified domain
- **Fix:** If using a different email, make sure it's from a verified domain

## Using Railway CLI (Alternative)

If you prefer command line:

```bash
# Install Railway CLI
npm i -g @railway/cli

# Login
railway login

# Link your project
railway link

# Set Resend API key
railway variables set RESEND_API_KEY="re_your-api-key-here"

# Verify
railway variables
```

## Security Note

- ✅ Environment variables in Railway are secure and encrypted
- ✅ Never commit API keys to Git (you've already protected `appsettings.Development.json`)
- ✅ Railway variables are only visible to project members
- ✅ Resend API keys should be kept secret and rotated if compromised

