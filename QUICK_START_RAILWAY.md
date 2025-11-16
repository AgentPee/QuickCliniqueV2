# Quick Start: Deploy to Railway

## 🚀 Quick Deployment Steps

### 1. Push to GitHub
```bash
git add .
git commit -m "Prepare for Railway deployment"
git push origin main
```

### 2. Create Railway Project
1. Go to https://railway.app
2. Click **"New Project"**
3. Select **"Deploy from GitHub repo"**
4. Choose your repository
5. Railway will auto-detect it's a .NET app

### 3. Add MySQL Database
1. In Railway dashboard, click **"New"**
2. Select **"Database"** → **"Add MySQL"**
3. Wait for it to provision

### 4. Set Environment Variables
Go to your **Web Service** → **Variables** tab and add:

```
ASPNETCORE_ENVIRONMENT=Production
```

Then add your MySQL connection string using Railway's template variables:

```
ConnectionStrings__DefaultConnection=Server=${{MySQL.MYSQLHOST}};Database=${{MySQL.MYSQLDATABASE}};Uid=${{MySQL.MYSQLUSER}};Pwd=${{MySQL.MYSQLPASSWORD}};Port=${{MySQL.MYSQLPORT}}
```

**To get the MySQL variables:**
1. Click on your **MySQL service**
2. Go to **"Variables"** tab
3. Copy the values or use the `${{MySQL.VARIABLE_NAME}}` syntax

### 5. Deploy
Railway will automatically deploy! Check the **"Deployments"** tab to see progress.

### 6. Get Your URL
Once deployed, Railway will provide a URL like:
```
https://your-app-name.up.railway.app
```

## 📝 Environment Variables Checklist

- [ ] `ASPNETCORE_ENVIRONMENT=Production`
- [ ] `ConnectionStrings__DefaultConnection` (with MySQL variables)
- [ ] `EmailSettings__FromEmail` (optional)
- [ ] `EmailSettings__SmtpUsername` (optional)
- [ ] `EmailSettings__SmtpPassword` (optional)

## 🔍 Troubleshooting

**Build fails?**
- Check build logs in Railway dashboard
- Ensure Dockerfile is in `QuickClinique/` directory

**App won't start?**
- Check application logs
- Verify MySQL connection string format
- Ensure all required environment variables are set

**Database connection errors?**
- Verify MySQL service is running
- Check connection string uses correct variable syntax
- Ensure database was created successfully

## 📚 Full Documentation

See `RAILWAY_DEPLOYMENT.md` for detailed instructions.

