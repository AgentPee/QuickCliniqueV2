# Fix Railway MySQL Connection

## The Problem
Your app can't connect to MySQL because the connection string isn't configured correctly in Railway.

## Solution: Set Environment Variables in Railway

### Step 1: Get Your MySQL Service Name
1. Go to your Railway project dashboard
2. Look at your MySQL service
3. **Note the exact service name** (it might be `MySQL`, `mysql`, `mysql-db`, etc.)

### Step 2: Set Environment Variables in Your Web Service

1. Click on your **Web Service** (the one running your .NET app)
2. Go to the **"Variables"** tab
3. Click **"Raw Editor"** or **"New Variable"**

#### Add These Variables:

**Variable 1:**
```
Name: ASPNETCORE_ENVIRONMENT
Value: Production
```

**Variable 2 (Option A - Using Template Variables):**
```
Name: ConnectionStrings__DefaultConnection
Value: Server=${{MySQL.MYSQLHOST}};Database=${{MySQL.MYSQLDATABASE}};Uid=${{MySQL.MYSQLUSER}};Pwd=${{MySQL.MYSQLPASSWORD}};Port=${{MySQL.MYSQLPORT}};SslMode=Preferred;
```

**IMPORTANT:** Make sure `Database=${{MySQL.MYSQLDATABASE}}` is included! If the Database parameter is missing or empty, the app will fail to connect.

**Important:** Replace `MySQL` with your actual MySQL service name if it's different!

**Variable 2 (Option B - Using Actual Values):**

If template variables don't work:

1. Go to your **MySQL service** → **Variables** tab
2. Copy these actual values:
   - `MYSQLHOST` (e.g., `containers-us-west-xxx.railway.app`)
   - `MYSQLDATABASE` (e.g., `railway`)
   - `MYSQLUSER` (e.g., `root`)
   - `MYSQLPASSWORD` (copy the actual password)
   - `MYSQLPORT` (usually `3306`)

3. Create the connection string (make sure Database parameter is included):
```
Name: ConnectionStrings__DefaultConnection
Value: Server=your-mysql-host;Database=your-database-name;Uid=your-username;Pwd=your-password;Port=3306;SslMode=Preferred;
```

**Note:** Railway MySQL typically uses `railway` as the database name. Check your MySQL service's `MYSQLDATABASE` variable to confirm.

**Example:**
```
ConnectionStrings__DefaultConnection=Server=containers-us-west-123.railway.app;Database=railway;Uid=root;Pwd=abc123xyz;Port=3306;SslMode=Preferred;
```

### Step 3: Verify Service Linking

1. Make sure both services (Web and MySQL) are in the **same Railway project**
2. Railway automatically links services in the same project
3. If they're in different projects, you'll need to use the public connection details

### Step 4: Redeploy

After adding the variables:
1. Railway will automatically redeploy
2. Or manually trigger a redeploy from the **Deployments** tab
3. Check the logs to see if the connection works

## Verify It's Working

After redeploying, check the logs. You should see:
- `[INIT] Connection string: Server=...` (password hidden)
- `[SUCCESS] Migrations applied successfully!`
- `Database seeded successfully!`

If you see connection errors, check:
1. The connection string format is correct
2. All values are filled in (no `${{...}}` placeholders remaining)
3. SSL mode is included (`SslMode=Preferred;`)
4. Both services are running

## Common Issues

### Issue: Template variables not resolving
**Solution:** Use Option B above - copy the actual values from MySQL service variables

### Issue: "Unable to connect to any of the specified MySQL hosts"
**Solutions:**
- Verify the MySQL service is running
- Check the host value is correct (should be a Railway domain, not `localhost`)
- Ensure `SslMode=Preferred;` is in the connection string
- Make sure both services are in the same project

### Issue: "Access denied"
**Solution:** 
- Verify the username and password are correct
- Check you're using the values from the MySQL service's Variables tab

## Need Help?

Check the application logs in Railway to see the exact error message. The logs will show:
- What connection string is being used (password hidden)
- Any connection errors
- Whether migrations ran successfully

