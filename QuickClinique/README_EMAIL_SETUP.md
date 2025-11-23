# Email Configuration Setup

## Security Notice
**Never commit API keys or passwords to the repository!**

## Local Development Setup

### Option 1: Environment Variables (Recommended)

Set the following environment variables before running the application:

**Windows (PowerShell):**
```powershell
$env:SMTP_PASSWORD="your-sendgrid-api-key-here"
$env:EMAIL_FROM="quickclinique25@gmail.com"
$env:SMTP_SERVER="smtp.sendgrid.net"
$env:SMTP_PORT="587"
$env:SMTP_USERNAME="apikey"
```

**Windows (Command Prompt):**
```cmd
set SMTP_PASSWORD=your-sendgrid-api-key-here
set EMAIL_FROM=quickclinique25@gmail.com
set SMTP_SERVER=smtp.sendgrid.net
set SMTP_PORT=587
set SMTP_USERNAME=apikey
```

**Linux/Mac:**
```bash
export SMTP_PASSWORD="your-sendgrid-api-key-here"
export EMAIL_FROM="quickclinique25@gmail.com"
export SMTP_SERVER="smtp.sendgrid.net"
export SMTP_PORT="587"
export SMTP_USERNAME="apikey"
```

### Option 2: appsettings.Development.json

Create or update `appsettings.Development.json` (this file should be in .gitignore):

```json
{
  "EmailSettings": {
    "FromEmail": "quickclinique25@gmail.com",
    "FromName": "QuickClinique",
    "SmtpServer": "smtp.sendgrid.net",
    "SmtpPort": "587",
    "SmtpUsername": "apikey",
    "SmtpPassword": "your-sendgrid-api-key-here"
  }
}
```

## Production Setup

### Railway / Azure / Other Cloud Platforms

Set the following environment variables in your hosting platform:

- `SMTP_PASSWORD` - Your SendGrid API key
- `EMAIL_FROM` - Sender email address (optional, defaults to config)
- `SMTP_SERVER` - SMTP server (optional, defaults to smtp.sendgrid.net)
- `SMTP_PORT` - SMTP port (optional, defaults to 587)
- `SMTP_USERNAME` - SMTP username (optional, defaults to "apikey")

### Railway Example:
1. Go to your Railway project
2. Navigate to Variables tab
3. Add: `SMTP_PASSWORD` = `your-sendgrid-api-key`

## SendGrid Setup

1. Sign up at https://sendgrid.com
2. Verify your sender email in SendGrid Dashboard → Settings → Sender Authentication
3. Create an API Key in Settings → API Keys
4. Use the API key as `SMTP_PASSWORD` environment variable

## Important Notes

- The application checks environment variables first, then falls back to `appsettings.json`
- For production, always use environment variables
- Never commit `appsettings.Development.json` or `appsettings.Production.json` with real API keys
- If you accidentally commit a secret, rotate/regenerate it immediately in SendGrid

