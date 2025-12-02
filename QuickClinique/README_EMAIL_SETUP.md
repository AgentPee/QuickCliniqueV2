# Email Configuration Setup

## Security Notice
**Never commit API keys or passwords to the repository!**

## Local Development Setup

### Option 1: Environment Variables (Recommended)

Set the following environment variables before running the application:

**Windows (PowerShell):**
```powershell
$env:RESEND_API_KEY="re_your-resend-api-key-here"
$env:EMAIL_FROM="quickclinique25@gmail.com"
```

**Windows (Command Prompt):**
```cmd
set RESEND_API_KEY=re_your-resend-api-key-here
set EMAIL_FROM=quickclinique25@gmail.com
```

**Linux/Mac:**
```bash
export RESEND_API_KEY="re_your-resend-api-key-here"
export EMAIL_FROM="quickclinique25@gmail.com"
```

**Note:** For backward compatibility, `SMTP_PASSWORD` can also be used instead of `RESEND_API_KEY`.

### Option 2: appsettings.Development.json

Create or update `appsettings.Development.json` (this file should be in .gitignore):

```json
{
  "EmailSettings": {
    "FromEmail": "quickclinique25@gmail.com",
    "FromName": "QuickClinique",
    "SmtpPassword": "re_your-resend-api-key-here"
  }
}
```

## Production Setup

### Railway / Azure / Other Cloud Platforms

Set the following environment variables in your hosting platform:

- `RESEND_API_KEY` - Your Resend API key (REQUIRED)
- `EMAIL_FROM` - Sender email address (optional, defaults to config)
- `EMAIL_FROM_NAME` - Sender name (optional, defaults to "QuickClinique")
- `EMAIL_REPLY_TO` - Reply-to email address (optional, defaults to EMAIL_FROM)

**Note:** For backward compatibility, `SMTP_PASSWORD` can also be used instead of `RESEND_API_KEY`.

### Railway Example:
1. Go to your Railway project
2. Navigate to Variables tab
3. Add: `RESEND_API_KEY` = `re_your-resend-api-key`

## Resend Setup

1. Sign up at https://resend.com
2. Verify your sender email/domain in Resend Dashboard → Domains
   - For testing, you can use the Resend test domain
   - For production, verify your own domain for better deliverability
3. Create an API Key in API Keys section
4. Use the API key as `RESEND_API_KEY` environment variable
   - Resend API keys start with `re_`

## Important Notes

- The application checks environment variables first, then falls back to `appsettings.json`
- For production, always use environment variables
- Never commit `appsettings.Development.json` or `appsettings.Production.json` with real API keys
- If you accidentally commit a secret, rotate/regenerate it immediately in Resend
- Resend free tier: 3,000 emails/month, 100 emails/day

