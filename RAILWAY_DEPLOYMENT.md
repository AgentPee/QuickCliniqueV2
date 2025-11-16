# Railway Deployment Guide for QuickClinique

This guide will help you deploy your QuickClinique ASP.NET Core application to Railway.

## Prerequisites

1. A Railway account (sign up at https://railway.app)
2. A MySQL database (Railway provides MySQL, or you can use an external service)
3. Git repository (GitHub, GitLab, or Bitbucket)

## Step 1: Prepare Your Repository

1. Make sure all your changes are committed to Git
2. Push your code to GitHub/GitLab/Bitbucket

## Step 2: Create a New Project on Railway

1. Go to https://railway.app and sign in
2. Click "New Project"
3. Select "Deploy from GitHub repo" (or your Git provider)
4. Select your repository
5. Railway will automatically detect it's a .NET application

## Step 3: Add MySQL Database

1. In your Railway project, click "New"
2. Select "Database" → "Add MySQL"
3. Railway will create a MySQL database for you
4. Note the connection details (you'll need them in the next step)

## Step 4: Configure Environment Variables

In your Railway project, go to the "Variables" tab and add the following:

### Required Variables:

```
ASPNETCORE_ENVIRONMENT=Production
```

### Database Connection String:

Replace with your Railway MySQL connection string:
```
ConnectionStrings__DefaultConnection=Server=<MYSQL_HOST>;Database=<MYSQL_DATABASE>;Uid=<MYSQL_USER>;Pwd=<MYSQL_PASSWORD>;Port=<MYSQL_PORT>
```

**Note:** Railway provides these values in your MySQL service. Click on your MySQL service → "Variables" tab to see:
- `MYSQLHOST`
- `MYSQLDATABASE`
- `MYSQLUSER`
- `MYSQLPASSWORD`
- `MYSQLPORT`

Your connection string should look like:
```
ConnectionStrings__DefaultConnection=Server=${{MySQL.MYSQLHOST}};Database=${{MySQL.MYSQLDATABASE}};Uid=${{MySQL.MYSQLUSER}};Pwd=${{MySQL.MYSQLPASSWORD}};Port=${{MySQL.MYSQLPORT}}
```

### Email Settings (Optional):

```
EmailSettings__FromEmail=your-email@gmail.com
EmailSettings__FromName=QuickClinique
EmailSettings__SmtpServer=smtp.gmail.com
EmailSettings__SmtpPort=587
EmailSettings__SmtpUsername=your-email@gmail.com
EmailSettings__SmtpPassword=your-app-password
```

**Note:** For Gmail, you'll need to use an App Password, not your regular password.

## Step 5: Configure Port

Railway automatically sets the `PORT` environment variable. Make sure your `Program.cs` uses it:

```csharp
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://0.0.0.0:{port}");
```

Or Railway will handle this automatically with the Dockerfile configuration.

## Step 6: Deploy

1. Railway will automatically deploy when you push to your main branch
2. Or click "Deploy" in the Railway dashboard
3. Wait for the build to complete
4. Your app will be available at a Railway-provided URL

## Step 7: Run Migrations

After deployment, your app should automatically run migrations on startup (as configured in `Program.cs`). If you need to run migrations manually:

1. Go to your service in Railway
2. Click "Deployments" → "View Logs"
3. Check if migrations ran successfully
4. If not, you can use Railway's CLI or connect to the container

## Troubleshooting

### Database Connection Issues

- Verify your connection string format
- Make sure the MySQL service is running
- Check that all environment variables are set correctly

### Build Failures

- Check the build logs in Railway
- Ensure all NuGet packages are properly referenced
- Verify the .NET version matches (9.0)

### Application Not Starting

- Check the application logs in Railway
- Verify all required environment variables are set
- Ensure the port configuration is correct

## Railway CLI (Optional)

You can also use Railway CLI for advanced operations:

```bash
# Install Railway CLI
npm i -g @railway/cli

# Login
railway login

# Link your project
railway link

# Deploy
railway up
```

## Environment Variables Reference

| Variable | Description | Example |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Environment name | `Production` |
| `ConnectionStrings__DefaultConnection` | MySQL connection string | See above |
| `EmailSettings__FromEmail` | Sender email | `noreply@example.com` |
| `EmailSettings__SmtpServer` | SMTP server | `smtp.gmail.com` |
| `EmailSettings__SmtpPort` | SMTP port | `587` |
| `EmailSettings__SmtpUsername` | SMTP username | `your-email@gmail.com` |
| `EmailSettings__SmtpPassword` | SMTP password | `your-app-password` |

## Custom Domain (Optional)

1. Go to your service settings
2. Click "Generate Domain" or "Custom Domain"
3. Follow Railway's instructions to configure your domain

## Monitoring

- View logs in the Railway dashboard
- Set up alerts for deployment failures
- Monitor resource usage

## Support

- Railway Docs: https://docs.railway.app
- Railway Discord: https://discord.gg/railway

