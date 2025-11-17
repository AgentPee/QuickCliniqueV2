# Fix Railway MySQL Password Authentication Error

## The Problem
You're getting "Access denied for user 'root'" even though the password is set correctly in Railway.

## Common Causes

### 1. Password Has Special Characters
If your password contains special characters, they might need to be URL-encoded in the connection string.

### 2. Environment Variable Format
Railway uses double underscores (`__`) for nested configuration. Make sure you're using:
```
ConnectionStrings__DefaultConnection
```
NOT:
```
ConnectionStrings:DefaultConnection
```

### 3. Password Truncation
The password might be getting cut off if there are spaces or line breaks.

## Solution Steps

### Step 1: Verify Your Environment Variable

In Railway, go to your **Web Service** → **Variables** tab and check:

1. **Variable Name** must be exactly:
   ```
   ConnectionStrings__DefaultConnection
   ```
   (Double underscore, not single underscore or colon)

2. **Variable Value** should be:
   ```
   Server=mysql.railway.internal;Database=railway;Uid=root;Pwd=YOUR_PASSWORD_HERE;Port=3306;SslMode=Preferred;
   ```

3. **Make sure there are NO extra spaces** before or after the value

### Step 2: Get the Exact Password

1. Go to your **MySQL service** → **Variables** tab
2. Find `MYSQL_ROOT_PASSWORD`
3. **Copy the EXACT value** (no extra spaces)
4. Paste it directly into your connection string

### Step 3: Test the Connection String Format

Your connection string should look exactly like this (replace with your actual password):
```
Server=mysql.railway.internal;Database=railway;Uid=root;Pwd=ivJNxaapgcWlGxkCSZkkOKtSRGBWEYjX;Port=3306;SslMode=Preferred;
```

**Important:**
- No spaces around the `=` signs
- No quotes around the value
- Semicolon at the end
- All parameters on one line

### Step 4: Alternative - Use Individual Environment Variables

If the connection string format doesn't work, you can set individual variables:

```
MYSQL_HOST=mysql.railway.internal
MYSQL_DATABASE=railway
MYSQL_USER=root
MYSQL_PASSWORD=ivJNxaapgcWlGxkCSZkkOKtSRGBWEYjX
MYSQL_PORT=3306
```

Then update `Program.cs` to build the connection string from these variables.

### Step 5: Check for Hidden Characters

1. Delete the environment variable
2. Recreate it by typing it fresh (don't copy-paste)
3. Make sure there are no invisible characters

### Step 6: Verify Both Services Are Running

1. Check that your **MySQL service** is running (green status)
2. Check that your **Web service** is running (green status)
3. Both should be in the **same Railway project**

## Debugging

After deploying, check the logs for:
- `[DEBUG] Password found in connection string, length: XX` - This shows if the password is being read
- `[INFO] Final connection string: ...` - This shows the connection string format

If the password length is 0 or wrong, the environment variable isn't being read correctly.

## Still Not Working?

If you've tried everything above:

1. **Delete and recreate** the environment variable in Railway
2. **Redeploy** your web service
3. Check the **exact error message** in the logs
4. Verify the password matches exactly between MySQL service and Web service variables

