using Microsoft.EntityFrameworkCore;
using QuickClinique.Models;
using QuickClinique.Services;
using QuickClinique.Middleware;
using System.Linq;
using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add services
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IDataSeedingService, DataSeedingService>();

// DB Context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
}

// Add SSL mode if not present (Railway MySQL may require this)
if (!connectionString.Contains("SslMode", StringComparison.OrdinalIgnoreCase))
{
    connectionString += (connectionString.EndsWith(";") ? "" : ";") + "SslMode=Preferred;";
}

// Check for unresolved template variables first
if (connectionString.Contains("${{") || connectionString.Contains("${"))
{
    Console.WriteLine("[ERROR] Connection string contains unresolved template variables!");
    Console.WriteLine("[ERROR] Make sure you're using the actual MySQL service variable names in Railway.");
    Console.WriteLine("[ERROR] Template variables should be resolved by Railway automatically.");
    Console.WriteLine("[ERROR] Falling back to 'railway' as database name.");
    
    // Remove any Database parameter (even if it has template variables)
    connectionString = System.Text.RegularExpressions.Regex.Replace(
        connectionString, 
        @"Database=[^;]*;?", 
        "", 
        System.Text.RegularExpressions.RegexOptions.IgnoreCase
    );
    connectionString = connectionString.TrimEnd(';') + ";Database=railway;";
}

// Ensure database name is present and not empty
var dbNameMatch = System.Text.RegularExpressions.Regex.Match(connectionString, @"Database=([^;]*)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
var dbName = dbNameMatch.Success ? dbNameMatch.Groups[1].Value.Trim() : null;

if (string.IsNullOrWhiteSpace(dbName) || dbName.Contains("$"))
{
    Console.WriteLine("[WARNING] Connection string missing or has empty/invalid Database parameter.");
    Console.WriteLine("[WARNING] Railway MySQL typically uses 'railway' as the database name.");
    
    // Remove existing empty/invalid Database parameter if present
    connectionString = System.Text.RegularExpressions.Regex.Replace(
        connectionString, 
        @"Database=[^;]*;?", 
        "", 
        System.Text.RegularExpressions.RegexOptions.IgnoreCase
    );
    
    // Add the database name (Railway MySQL usually uses 'railway')
    connectionString = connectionString.TrimEnd(';') + ";Database=railway;";
    Console.WriteLine("[INFO] Added Database=railway to connection string.");
    dbName = "railway";
}
else
{
    Console.WriteLine($"[INFO] Database name found in connection string: {dbName}");
}

// Update the configuration with the modified connection string
// This ensures any code that reads from config gets the corrected version
builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;

// Log the final connection string (without password) for debugging
var safeConnectionString = System.Text.RegularExpressions.Regex.Replace(
    connectionString, 
    @"Pwd=[^;]+", 
    "Pwd=***", 
    System.Text.RegularExpressions.RegexOptions.IgnoreCase
);
Console.WriteLine($"[INFO] Final connection string: {safeConnectionString}");

// Debug: Check if password is present and its length (for troubleshooting)
var pwdMatch = System.Text.RegularExpressions.Regex.Match(connectionString, @"Pwd=([^;]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
if (pwdMatch.Success)
{
    var pwdLength = pwdMatch.Groups[1].Value.Length;
    Console.WriteLine($"[DEBUG] Password found in connection string, length: {pwdLength}");
    if (pwdLength == 0)
    {
        Console.WriteLine("[ERROR] Password is empty in connection string!");
    }
}
else
{
    Console.WriteLine("[ERROR] Password parameter not found in connection string!");
}

// Verify database name one more time before using it
var finalDbCheck = System.Text.RegularExpressions.Regex.Match(connectionString, @"Database=([^;]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
if (!finalDbCheck.Success || string.IsNullOrWhiteSpace(finalDbCheck.Groups[1].Value))
{
    throw new InvalidOperationException("Connection string is missing a valid database name after validation!");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        connectionString,
        new MySqlServerVersion(new Version(9, 4, 0)), // Updated to match Railway MySQL 9.4.0
        mySqlOptions => mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null)));

// Add session services (required for ClinicStaffController)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// Add distributed memory cache (required for session)
builder.Services.AddDistributedMemoryCache();

// Configure Data Protection (fixes session/antiforgery token issues)
// Note: In Railway, containers are ephemeral, so keys will regenerate on restart
// This is acceptable - users will just need to log in again after deployments
// Basic configuration is sufficient - ASP.NET Core will handle key management automatically
builder.Services.AddDataProtection();

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Strict;
    options.Secure = CookieSecurePolicy.Always;
});

var app = builder.Build();

// Configure port for Railway (Railway sets PORT environment variable)
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    app.Urls.Add($"http://0.0.0.0:{port}");
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Add error handling middleware
app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseCookiePolicy();

// Add session middleware (must come before UseAuthentication/UseAuthorization)
app.UseSession();

// Add authentication if you're using identity/authentication
// Note: If you're using ASP.NET Core Identity, you need to add authentication services above
app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Student}/{action=Login}/{id?}");

// Database initialization and seeding
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var seedingService = services.GetRequiredService<IDataSeedingService>();

        // STEP 1: Ensure database exists (optional - Railway MySQL already creates the database)
        Console.WriteLine("[INIT] Checking if database exists...");
        try
        {
            // Re-read connection string from configuration to ensure we have the latest
            var initConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(initConnectionString))
            {
                // Fall back to the one we configured
                initConnectionString = connectionString;
            }
            
            if (string.IsNullOrEmpty(initConnectionString))
            {
                throw new Exception("Connection string 'DefaultConnection' is not configured.");
            }

            // Log connection string (without password for security)
            var initSafeConnectionString = System.Text.RegularExpressions.Regex.Replace(
                initConnectionString, 
                @"Pwd=[^;]+", 
                "Pwd=***", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
            Console.WriteLine($"[INIT] Connection string: {initSafeConnectionString}");

            // Check if connection string has unresolved template variables
            if (initConnectionString.Contains("${{") || initConnectionString.Contains("${"))
            {
                Console.WriteLine("[WARNING] Connection string appears to have unresolved template variables!");
                Console.WriteLine("[WARNING] Make sure you're using the actual MySQL service variable names in Railway.");
                Console.WriteLine("[WARNING] Skipping database creation check - Railway MySQL should already have the database.");
                // Skip database creation - Railway MySQL already creates it
            }
            else
            {
                // Parse database name from connection string
                var initDbNameMatch = System.Text.RegularExpressions.Regex.Match(initConnectionString, @"Database=([^;]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                var targetDbName = "railway"; // Default to Railway's default database name
                if (initDbNameMatch.Success && !string.IsNullOrWhiteSpace(initDbNameMatch.Groups[1].Value))
                {
                    targetDbName = initDbNameMatch.Groups[1].Value.Trim();
                }

                // Create connection string without database, add SSL mode if not present
                var serverConnectionString = initConnectionString;
                if (initDbNameMatch.Success)
                {
                    serverConnectionString = System.Text.RegularExpressions.Regex.Replace(initConnectionString, @"Database=[^;]+;?", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                }
                
                // Add SSL mode if not present (Railway MySQL may require this)
                if (!serverConnectionString.Contains("SslMode", StringComparison.OrdinalIgnoreCase))
                {
                    serverConnectionString += (serverConnectionString.EndsWith(";") ? "" : ";") + "SslMode=Preferred;";
                }

                // Connect to MySQL server (without database)
                using var serverConnection = new MySqlConnection(serverConnectionString);
                await serverConnection.OpenAsync();
                Console.WriteLine("[INIT] Connected to MySQL server.");

                // Check if database exists
                using var checkDbCommand = serverConnection.CreateCommand();
                checkDbCommand.CommandText = $@"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.SCHEMATA 
                    WHERE SCHEMA_NAME = @dbName";
                
                var dbNameParam = checkDbCommand.CreateParameter();
                dbNameParam.ParameterName = "@dbName";
                dbNameParam.Value = targetDbName;
                checkDbCommand.Parameters.Add(dbNameParam);

                var dbExists = Convert.ToInt32(await checkDbCommand.ExecuteScalarAsync()) > 0;
                Console.WriteLine($"[INIT] Database '{targetDbName}' exists: {dbExists}");

                if (!dbExists)
                {
                    Console.WriteLine($"[INIT] Database '{targetDbName}' does not exist. Creating it...");
                    using var createDbCommand = serverConnection.CreateCommand();
                    createDbCommand.CommandText = $"CREATE DATABASE IF NOT EXISTS `{targetDbName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci";
                    await createDbCommand.ExecuteNonQueryAsync();
                    Console.WriteLine($"[SUCCESS] Database '{targetDbName}' created successfully!");
                }
                else
                {
                    Console.WriteLine($"[OK] Database '{targetDbName}' already exists.");
                }

                await serverConnection.CloseAsync();
            }
        }
        catch (Exception dbEx)
        {
            Console.WriteLine($"[WARNING] Failed to check/create database: {dbEx.Message}");
            Console.WriteLine($"[WARNING] This is usually OK on Railway - MySQL database is already created.");
            Console.WriteLine($"[WARNING] Continuing with migrations...");
            // Don't throw - Railway MySQL already creates the database, so we can continue
        }

        // STEP 2: Run migrations to create tables
        Console.WriteLine("[INIT] Checking for pending migrations...");
        var pendingMigrations = context.Database.GetPendingMigrations().ToList();
        if (pendingMigrations.Any())
        {
            Console.WriteLine($"Applying {pendingMigrations.Count} pending migration(s):");
            foreach (var migration in pendingMigrations)
            {
                Console.WriteLine($"  - {migration}");
            }
        }

        try
        {
            context.Database.Migrate();
            Console.WriteLine("[SUCCESS] Migrations applied successfully!");
        }
        catch (Exception migrateEx)
        {
            Console.WriteLine($"[ERROR] Migration failed: {migrateEx.Message}");
            Console.WriteLine($"[ERROR] Stack trace: {migrateEx.StackTrace}");
            throw; // Re-throw to prevent continuing without tables
        }

        // STEP 3: Ensure required columns exist AFTER tables are created
        // This handles cases where columns might be missing due to migration issues
        Console.WriteLine("[INIT] Starting database column check...");
        try
        {
            var connection = context.Database.GetDbConnection();
            Console.WriteLine($"[INIT] Database connection: {connection.ConnectionString}");
            
            if (connection.State != System.Data.ConnectionState.Open)
            {
                Console.WriteLine("[INIT] Opening database connection...");
                await connection.OpenAsync();
                Console.WriteLine($"[INIT] Connection state: {connection.State}");
            }
            
            using var command = connection.CreateCommand();
            
            // First, check if tables exist before checking columns
            command.CommandText = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_SCHEMA = DATABASE() 
                AND TABLE_NAME = 'appointments'";
            var appointmentsTableExists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
            
            if (!appointmentsTableExists)
            {
                Console.WriteLine("[WARNING] Appointments table does not exist yet. This should not happen after migrations.");
                Console.WriteLine("[WARNING] Skipping column checks - tables may not have been created properly.");
                await connection.CloseAsync();
                // Continue to data seeding - migrations should have created tables
            }
            else
            {
                // Tables exist, proceed with column checks
                
                // Check and add TriageNotes column to appointments table FIRST
            Console.WriteLine("[INIT] Checking for TriageNotes column in appointments table...");
            command.CommandText = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = DATABASE() 
                AND TABLE_NAME = 'appointments' 
                AND COLUMN_NAME = 'TriageNotes'";
            
            var result = await command.ExecuteScalarAsync();
            var triageNotesColumnExists = Convert.ToInt32(result) > 0;
            Console.WriteLine($"[INIT] TriageNotes column exists: {triageNotesColumnExists}");
            
            if (!triageNotesColumnExists)
            {
                Console.WriteLine("[CRITICAL] TriageNotes column missing! Adding to appointments table...");
                // MySQL doesn't allow DEFAULT for TEXT/BLOB columns, so we add as nullable
                // Then update existing rows and set to NOT NULL
                command.CommandText = @"ALTER TABLE `appointments` ADD COLUMN `TriageNotes` longtext NULL";
                
                try
                {
                    await command.ExecuteNonQueryAsync();
                    Console.WriteLine("[SUCCESS] TriageNotes column added successfully to appointments table!");
                    
                    // Update existing NULL values to empty string
                    command.CommandText = @"UPDATE `appointments` SET `TriageNotes` = '' WHERE `TriageNotes` IS NULL";
                    await command.ExecuteNonQueryAsync();
                    
                    // Now alter to NOT NULL
                    command.CommandText = @"ALTER TABLE `appointments` MODIFY COLUMN `TriageNotes` longtext NOT NULL";
                    await command.ExecuteNonQueryAsync();
                    Console.WriteLine("[SUCCESS] TriageNotes column set to NOT NULL!");
                    
                    // Verify it was added
                    command.CommandText = @"
                        SELECT COUNT(*) 
                        FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_SCHEMA = DATABASE() 
                        AND TABLE_NAME = 'appointments' 
                        AND COLUMN_NAME = 'TriageNotes'";
                    var verifyResult = await command.ExecuteScalarAsync();
                    var verified = Convert.ToInt32(verifyResult) > 0;
                    Console.WriteLine($"[VERIFY] TriageNotes column verification: {(verified ? "EXISTS" : "STILL MISSING")}");
                }
                catch (Exception addEx)
                {
                    Console.WriteLine($"[ERROR] Failed to add TriageNotes column: {addEx.Message}");
                    Console.WriteLine($"[ERROR] Stack trace: {addEx.StackTrace}");
                    throw; // Re-throw to be caught by outer catch
                }
            }
            else
            {
                Console.WriteLine("[OK] TriageNotes column already exists in appointments table.");
            }
            
            // Check and add Symptoms column to appointments table (case-insensitive check)
            Console.WriteLine("[INIT] Checking for Symptoms column in appointments table...");
            command.CommandText = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = DATABASE() 
                AND TABLE_NAME = 'appointments' 
                AND UPPER(COLUMN_NAME) = 'SYMPTOMS'";
            
            var symptomsColumnExists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
            Console.WriteLine($"[INIT] Symptoms column exists in appointments table: {symptomsColumnExists}");
            
            if (!symptomsColumnExists)
            {
                Console.WriteLine("[CRITICAL] Symptoms column missing! Adding to appointments table...");
                // MySQL doesn't allow DEFAULT for TEXT/BLOB columns, so we add as nullable
                // Then update existing rows and set to NOT NULL
                command.CommandText = @"ALTER TABLE `appointments` ADD COLUMN `Symptoms` longtext CHARACTER SET utf8mb4 NULL";
                
                try
                {
                    await command.ExecuteNonQueryAsync();
                    Console.WriteLine("[SUCCESS] Symptoms column added successfully to appointments table!");
                    
                    // Update existing NULL values to empty string
                    command.CommandText = @"UPDATE `appointments` SET `Symptoms` = '' WHERE `Symptoms` IS NULL";
                    await command.ExecuteNonQueryAsync();
                    
                    // Now alter to NOT NULL
                    command.CommandText = @"ALTER TABLE `appointments` MODIFY COLUMN `Symptoms` longtext CHARACTER SET utf8mb4 NOT NULL";
                    await command.ExecuteNonQueryAsync();
                    Console.WriteLine("[SUCCESS] Symptoms column set to NOT NULL!");
                    
                    // Verify it was added
                    command.CommandText = @"
                        SELECT COUNT(*) 
                        FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_SCHEMA = DATABASE() 
                        AND TABLE_NAME = 'appointments' 
                        AND UPPER(COLUMN_NAME) = 'SYMPTOMS'";
                    var verifyResult = await command.ExecuteScalarAsync();
                    var verified = Convert.ToInt32(verifyResult) > 0;
                    Console.WriteLine($"[VERIFY] Symptoms column verification: {(verified ? "EXISTS" : "STILL MISSING")}");
                    
                    if (!verified)
                    {
                        throw new Exception("Symptoms column verification failed - column was not added successfully");
                    }
                }
                catch (Exception addEx)
                {
                    Console.WriteLine($"[CRITICAL ERROR] Failed to add Symptoms column: {addEx.Message}");
                    Console.WriteLine($"[CRITICAL ERROR] Stack trace: {addEx.StackTrace}");
                    Console.WriteLine("[CRITICAL ERROR] The application may not function correctly without this column!");
                    throw; // Re-throw to be caught by outer catch
                }
            }
            else
            {
                Console.WriteLine("[OK] Symptoms column already exists in appointments table.");
            }
            
            // Check and add CancellationReason column to appointments table (case-insensitive check)
            Console.WriteLine("[INIT] Checking for CancellationReason column in appointments table...");
            
            // First, list all columns to debug
            command.CommandText = @"
                SELECT COLUMN_NAME 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = DATABASE() 
                AND TABLE_NAME = 'appointments'
                ORDER BY COLUMN_NAME";
            var allColumns = new List<string>();
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    allColumns.Add(reader.GetString(0));
                }
            }
            Console.WriteLine($"[DEBUG] Existing columns in appointments table: {string.Join(", ", allColumns)}");
            
            // Check for CancellationReason (case-insensitive)
            command.CommandText = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = DATABASE() 
                AND TABLE_NAME = 'appointments' 
                AND (COLUMN_NAME = 'CancellationReason' OR COLUMN_NAME = 'cancellationreason' OR UPPER(COLUMN_NAME) = 'CANCELLATIONREASON')";
            
            var cancellationReasonColumnExists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
            Console.WriteLine($"[INIT] CancellationReason column exists in appointments table: {cancellationReasonColumnExists}");
            
            if (!cancellationReasonColumnExists)
            {
                Console.WriteLine("[CRITICAL] CancellationReason column missing! Adding to appointments table...");
                
                try
                {
                    // MySQL doesn't allow DEFAULT for TEXT/BLOB columns, so we add as nullable
                    // Then update existing rows and set to NOT NULL
                    command.CommandText = @"ALTER TABLE `appointments` ADD COLUMN `CancellationReason` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL";
                    
                    await command.ExecuteNonQueryAsync();
                    Console.WriteLine("[SUCCESS] CancellationReason column added successfully to appointments table!");
                    
                    // Update existing NULL values to empty string
                    command.CommandText = @"UPDATE `appointments` SET `CancellationReason` = '' WHERE `CancellationReason` IS NULL";
                    await command.ExecuteNonQueryAsync();
                    
                    // Now alter to NOT NULL
                    command.CommandText = @"ALTER TABLE `appointments` MODIFY COLUMN `CancellationReason` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL";
                    await command.ExecuteNonQueryAsync();
                    Console.WriteLine("[SUCCESS] CancellationReason column set to NOT NULL!");
                    
                    // Wait a moment for MySQL to commit
                    await Task.Delay(200);
                    
                    // Verify it was added
                    command.CommandText = @"
                        SELECT COUNT(*) 
                        FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_SCHEMA = DATABASE() 
                        AND TABLE_NAME = 'appointments' 
                        AND (COLUMN_NAME = 'CancellationReason' OR UPPER(COLUMN_NAME) = 'CANCELLATIONREASON')";
                    var verifyResult = await command.ExecuteScalarAsync();
                    var verified = Convert.ToInt32(verifyResult) > 0;
                    Console.WriteLine($"[VERIFY] CancellationReason column verification: {(verified ? "EXISTS" : "STILL MISSING")}");
                    
                    if (!verified)
                    {
                        // List columns again for debugging
                        command.CommandText = @"
                            SELECT COLUMN_NAME 
                            FROM INFORMATION_SCHEMA.COLUMNS 
                            WHERE TABLE_SCHEMA = DATABASE() 
                            AND TABLE_NAME = 'appointments'
                            ORDER BY COLUMN_NAME";
                        var columnsAfter = new List<string>();
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                columnsAfter.Add(reader.GetString(0));
                            }
                        }
                        Console.WriteLine($"[DEBUG] Columns after add attempt: {string.Join(", ", columnsAfter)}");
                        throw new Exception("CancellationReason column verification failed - column was not added successfully");
                    }
                }
                catch (Exception addEx)
                {
                    // Check if it's a "duplicate column" error (column already exists) - that's OK
                    if (addEx.Message.Contains("Duplicate column name", StringComparison.OrdinalIgnoreCase) ||
                        addEx.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase) ||
                        addEx.Message.Contains("Duplicate", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("[INFO] CancellationReason column already exists (caught duplicate error).");
                        Console.WriteLine("[INFO] This is OK - column was likely added in a previous run.");
                    }
                    else
                    {
                        Console.WriteLine($"[CRITICAL ERROR] Failed to add CancellationReason column: {addEx.Message}");
                        Console.WriteLine($"[CRITICAL ERROR] Error type: {addEx.GetType().Name}");
                        Console.WriteLine($"[CRITICAL ERROR] Stack trace: {addEx.StackTrace}");
                        if (addEx.InnerException != null)
                        {
                            Console.WriteLine($"[CRITICAL ERROR] Inner exception: {addEx.InnerException.Message}");
                        }
                        Console.WriteLine("[CRITICAL ERROR] The application may not function correctly without this column!");
                        throw; // Re-throw to be caught by outer catch
                    }
                }
            }
            else
            {
                Console.WriteLine("[OK] CancellationReason column already exists in appointments table.");
            }
            
            // Check and add IsActive column to students table (case-insensitive check)
            Console.WriteLine("[INIT] Checking for IsActive column in students table...");
            command.CommandText = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = DATABASE() 
                AND TABLE_NAME = 'students' 
                AND UPPER(COLUMN_NAME) = 'ISACTIVE'";
            
            var columnExists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
            Console.WriteLine($"[INIT] IsActive column exists in students table: {columnExists}");
            
            if (!columnExists)
            {
                Console.WriteLine("[CRITICAL] IsActive column missing! Adding to students table...");
                command.CommandText = "ALTER TABLE `students` ADD COLUMN `IsActive` tinyint(1) NOT NULL DEFAULT 1";
                
                try
                {
                    await command.ExecuteNonQueryAsync();
                    Console.WriteLine("[SUCCESS] IsActive column added successfully to students table!");
                    
                    // Verify it was added
                    command.CommandText = @"
                        SELECT COUNT(*) 
                        FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_SCHEMA = DATABASE() 
                        AND TABLE_NAME = 'students' 
                        AND UPPER(COLUMN_NAME) = 'ISACTIVE'";
                    var verifyResult = await command.ExecuteScalarAsync();
                    var verified = Convert.ToInt32(verifyResult) > 0;
                    Console.WriteLine($"[VERIFY] IsActive column verification: {(verified ? "EXISTS" : "STILL MISSING")}");
                    
                    if (!verified)
                    {
                        throw new Exception("IsActive column verification failed - column was not added successfully");
                    }
                }
                catch (Exception addEx)
                {
                    Console.WriteLine($"[CRITICAL ERROR] Failed to add IsActive column: {addEx.Message}");
                    Console.WriteLine($"[CRITICAL ERROR] Stack trace: {addEx.StackTrace}");
                    Console.WriteLine("[CRITICAL ERROR] The application may not function correctly without this column!");
                    Console.WriteLine("[CRITICAL ERROR] Please run FIX_IsActive_Column.sql manually or check database permissions.");
                    throw; // Re-throw to be caught by outer catch
                }
            }
            else
            {
                Console.WriteLine("[OK] IsActive column already exists in students table.");
            }

            // Check and add IsActive column to clinicstaff table (case-insensitive check)
            Console.WriteLine("[INIT] Checking for IsActive column in clinicstaff table...");
            command.CommandText = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = DATABASE() 
                AND TABLE_NAME = 'clinicstaff' 
                AND UPPER(COLUMN_NAME) = 'ISACTIVE'";
            
            var clinicStaffColumnExists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
            Console.WriteLine($"[INIT] IsActive column exists in clinicstaff table: {clinicStaffColumnExists}");
            
            if (!clinicStaffColumnExists)
            {
                Console.WriteLine("[CRITICAL] IsActive column missing! Adding to clinicstaff table...");
                command.CommandText = "ALTER TABLE `clinicstaff` ADD COLUMN `IsActive` tinyint(1) NOT NULL DEFAULT 1";
                
                try
                {
                    await command.ExecuteNonQueryAsync();
                    Console.WriteLine("[SUCCESS] IsActive column added successfully to clinicstaff table!");
                    
                    // Verify it was added
                    command.CommandText = @"
                        SELECT COUNT(*) 
                        FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_SCHEMA = DATABASE() 
                        AND TABLE_NAME = 'clinicstaff' 
                        AND UPPER(COLUMN_NAME) = 'ISACTIVE'";
                    var verifyResult = await command.ExecuteScalarAsync();
                    var verified = Convert.ToInt32(verifyResult) > 0;
                    Console.WriteLine($"[VERIFY] IsActive column verification: {(verified ? "EXISTS" : "STILL MISSING")}");
                    
                    if (!verified)
                    {
                        throw new Exception("IsActive column verification failed - column was not added successfully");
                    }
                }
                catch (Exception addEx)
                {
                    Console.WriteLine($"[CRITICAL ERROR] Failed to add IsActive column to clinicstaff: {addEx.Message}");
                    Console.WriteLine($"[CRITICAL ERROR] Stack trace: {addEx.StackTrace}");
                    Console.WriteLine("[CRITICAL ERROR] The application may not function correctly without this column!");
                    Console.WriteLine("[CRITICAL ERROR] Please run FIX_IsActive_Clinicstaff_Column.sql manually or check database permissions.");
                    throw; // Re-throw to be caught by outer catch
                }
            }
            else
            {
                Console.WriteLine("[OK] IsActive column already exists in clinicstaff table.");
            }

                // Close the connection to ensure EF Core picks up schema changes
                if (connection.State == System.Data.ConnectionState.Open)
                {
                    Console.WriteLine("[INIT] Closing connection to refresh schema cache...");
                    await connection.CloseAsync();
                }
            } // End of else block for table existence check

        }
        catch (Exception columnEx)
        {
            Console.WriteLine($"[ERROR] Failed to check/add required columns: {columnEx.Message}");
            Console.WriteLine($"[ERROR] Stack trace: {columnEx.StackTrace}");
            // Don't throw - column checks are optional if migrations handled everything
            Console.WriteLine("[WARNING] Continuing despite column check errors - migrations may have already created all columns.");
        }

        // Final verification: Ensure critical columns exist before seeding
        try
        {
            var connection = context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }
            
            using var verifyCommand = connection.CreateCommand();
            
            // Check CancellationReason in appointments
            verifyCommand.CommandText = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = DATABASE() 
                AND TABLE_NAME = 'appointments' 
                AND (COLUMN_NAME = 'CancellationReason' OR UPPER(COLUMN_NAME) = 'CANCELLATIONREASON')";
            
            var cancellationReasonExists = Convert.ToInt32(await verifyCommand.ExecuteScalarAsync()) > 0;
            
            if (!cancellationReasonExists)
            {
                Console.WriteLine("[FINAL CHECK] CancellationReason column missing in appointments! Adding now...");
                try
                {
                    // MySQL doesn't allow DEFAULT for TEXT/BLOB columns, so we add as nullable first
                    verifyCommand.CommandText = @"ALTER TABLE `appointments` ADD COLUMN `CancellationReason` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL";
                    await verifyCommand.ExecuteNonQueryAsync();
                    
                    // Update existing NULL values to empty string
                    verifyCommand.CommandText = @"UPDATE `appointments` SET `CancellationReason` = '' WHERE `CancellationReason` IS NULL";
                    await verifyCommand.ExecuteNonQueryAsync();
                    
                    // Now alter to NOT NULL
                    verifyCommand.CommandText = @"ALTER TABLE `appointments` MODIFY COLUMN `CancellationReason` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL";
                    await verifyCommand.ExecuteNonQueryAsync();
                    Console.WriteLine("[FINAL CHECK] CancellationReason column added successfully!");
                }
                catch (Exception addEx)
                {
                    if (addEx.Message.Contains("Duplicate", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("[FINAL CHECK] CancellationReason column already exists.");
                    }
                    else
                    {
                        Console.WriteLine($"[FINAL CHECK ERROR] Failed to add CancellationReason: {addEx.Message}");
                        throw; // Re-throw if it's not a duplicate error
                    }
                }
            }
            else
            {
                Console.WriteLine("[FINAL CHECK] CancellationReason column verified in appointments table.");
            }
            
            // Check IsActive in clinicstaff
            verifyCommand.CommandText = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = DATABASE() 
                AND TABLE_NAME = 'clinicstaff' 
                AND UPPER(COLUMN_NAME) = 'ISACTIVE'";
            
            var columnExists = Convert.ToInt32(await verifyCommand.ExecuteScalarAsync()) > 0;
            
            if (!columnExists)
            {
                Console.WriteLine("[FINAL CHECK] IsActive column missing in clinicstaff! Adding now...");
                verifyCommand.CommandText = "ALTER TABLE `clinicstaff` ADD COLUMN `IsActive` tinyint(1) NOT NULL DEFAULT 1";
                await verifyCommand.ExecuteNonQueryAsync();
                Console.WriteLine("[FINAL CHECK] IsActive column added successfully!");
            }
            else
            {
                Console.WriteLine("[FINAL CHECK] IsActive column verified in clinicstaff table.");
            }
            
            // Close connection to refresh EF Core cache
            await connection.CloseAsync();
        }
        catch (Exception verifyEx)
        {
            Console.WriteLine($"[FINAL CHECK ERROR] Failed to verify/add required columns: {verifyEx.Message}");
            Console.WriteLine($"[FINAL CHECK ERROR] Stack trace: {verifyEx.StackTrace}");
            throw; // Don't proceed with seeding if critical columns don't exist
        }

        // Seed initial data
        await seedingService.SeedDataAsync();

        Console.WriteLine("Database initialized and seeded successfully!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred initializing the database: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        // Don't throw - allow app to start even if migration fails
        // The error will be visible in logs
    }
}

app.Run();