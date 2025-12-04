using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection.Repositories;
using QuickClinique.Models;
using QuickClinique.Services;
using QuickClinique.Middleware;
using QuickClinique.Hubs;
using System.Linq;
using MySqlConnector;
using Amazon.S3;
using Amazon;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        // Configure DateTime serialization to ISO 8601 format with UTC
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        // DateTime will be serialized as ISO 8601 by default in .NET, but ensure UTC is preserved
    });

// Add SignalR
builder.Services.AddSignalR();

// Add HttpClient for email service
builder.Services.AddHttpClient();

// Add services
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IDataSeedingService, DataSeedingService>();
builder.Services.AddScoped<IIdValidationService, IdValidationService>();

// Add background service for queue assignment
builder.Services.AddHostedService<QueueAssignmentService>();

// Configure file storage service
var storageProvider = builder.Configuration["Storage:Provider"] ?? "Local";
if (storageProvider.Equals("S3", StringComparison.OrdinalIgnoreCase))
{
    // Configure AWS S3
    var awsAccessKeyId = builder.Configuration["Storage:S3:AccessKeyId"] 
        ?? Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
    var awsSecretAccessKey = builder.Configuration["Storage:S3:SecretAccessKey"] 
        ?? Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
    var region = builder.Configuration["Storage:S3:Region"] ?? "us-east-1";

    if (string.IsNullOrEmpty(awsAccessKeyId) || string.IsNullOrEmpty(awsSecretAccessKey))
    {
        Console.WriteLine("[WARNING] AWS credentials not found. Falling back to Local storage.");
        builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
    }
    else
    {
        var regionEndpoint = RegionEndpoint.GetBySystemName(region);
        builder.Services.AddSingleton<IAmazonS3>(sp => 
            new AmazonS3Client(awsAccessKeyId, awsSecretAccessKey, regionEndpoint));
        builder.Services.AddScoped<IFileStorageService, S3FileStorageService>();
        Console.WriteLine($"[INFO] Using S3 storage provider with region: {region}");
    }
}
else
{
    // Use local file storage (default)
    builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
    Console.WriteLine("[INFO] Using Local file storage provider");
}

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

// Add connection pool settings to prevent connection exhaustion during concurrent registrations
// These settings help handle multiple simultaneous registrations
if (!connectionString.Contains("MaximumPoolSize", StringComparison.OrdinalIgnoreCase))
{
    connectionString += (connectionString.EndsWith(";") ? "" : ";") + "MaximumPoolSize=100;";
}
if (!connectionString.Contains("MinimumPoolSize", StringComparison.OrdinalIgnoreCase))
{
    connectionString += (connectionString.EndsWith(";") ? "" : ";") + "MinimumPoolSize=5;";
}
if (!connectionString.Contains("ConnectionTimeout", StringComparison.OrdinalIgnoreCase))
{
    connectionString += (connectionString.EndsWith(";") ? "" : ";") + "ConnectionTimeout=30;";
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
    // Use SameAsRequest for Railway - will be secure if HTTPS, but won't fail on HTTP
    // This allows cookies to work behind proxies that handle HTTPS termination
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    // Allow session to work even if old cookies can't be decrypted (after deployments)
    options.Cookie.Name = ".QuickClinique.Session";
});

// Add distributed memory cache (required for session)
builder.Services.AddDistributedMemoryCache();

// Configure Data Protection to persist keys to database
// This ensures keys persist across application restarts and deployments
// Without this, session cookies and antiforgery tokens become invalid after restarts
// Register our custom XML repository that uses Entity Framework Core
builder.Services.AddSingleton<IXmlRepository, EntityFrameworkCoreXmlRepository>();

// Configure Data Protection - it will automatically use the registered IXmlRepository
builder.Services.AddDataProtection()
    .SetApplicationName("QuickClinique")
    // Allow keys to be used for a longer period to handle old cookies gracefully
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Lax; // Changed from Strict to Lax for better compatibility
    // Use SameAsRequest for Railway - will be secure if HTTPS, but won't fail on HTTP
    // This allows cookies to work behind proxies that handle HTTPS termination
    options.Secure = CookieSecurePolicy.SameAsRequest;
});

var app = builder.Build();

// Configure forwarded headers for Railway (to get correct scheme and host from proxy)
// This must be called before UseHttpsRedirection and other middleware
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | 
                       ForwardedHeaders.XForwardedProto | 
                       ForwardedHeaders.XForwardedHost,
    RequireHeaderSymmetry = false
};

// Clear known networks and proxies to allow all (Railway uses various IPs)
forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();

app.UseForwardedHeaders(forwardedHeadersOptions);

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

// Map SignalR Hub
app.MapHub<MessageHub>("/messageHub");

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

        // STEP 2: Run migrations to create tables (AUTO-MIGRATION FOR RAILWAY)
        Console.WriteLine("========================================");
        Console.WriteLine("[INIT] AUTO-MIGRATION: Checking for pending migrations...");
        Console.WriteLine("========================================");
        
        try
        {
            // Ensure database connection is open for migrations
            var connection = context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
            {
                Console.WriteLine("[INIT] Opening database connection for migrations...");
                await connection.OpenAsync();
            }
            
            // Get list of pending migrations before applying
            var pendingMigrations = context.Database.GetPendingMigrations().ToList();
            var appliedMigrations = context.Database.GetAppliedMigrations().ToList();
            
            Console.WriteLine($"[INIT] Currently applied migrations: {appliedMigrations.Count}");
            foreach (var applied in appliedMigrations)
            {
                Console.WriteLine($"  ✓ {applied}");
            }
            
            if (pendingMigrations.Any())
            {
                Console.WriteLine($"[INIT] Found {pendingMigrations.Count} pending migration(s) to apply:");
                foreach (var migration in pendingMigrations)
                {
                    Console.WriteLine($"  → {migration}");
                }
                
                Console.WriteLine("[INIT] Applying migrations automatically...");
                context.Database.Migrate();
                Console.WriteLine("[SUCCESS] All migrations applied successfully!");
                
                // Verify migrations were applied
                var newAppliedMigrations = context.Database.GetAppliedMigrations().ToList();
                Console.WriteLine($"[VERIFY] Total applied migrations after: {newAppliedMigrations.Count}");
            }
            else
            {
                Console.WriteLine("[INFO] No pending migrations. Database is up to date.");
            }
            
            Console.WriteLine("========================================");
        }
        catch (Exception migrateEx)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("[ERROR] AUTO-MIGRATION FAILED!");
            Console.WriteLine("========================================");
            Console.WriteLine($"[ERROR] Error: {migrateEx.Message}");
            Console.WriteLine($"[ERROR] Type: {migrateEx.GetType().Name}");
            
            if (migrateEx.InnerException != null)
            {
                Console.WriteLine($"[ERROR] Inner Exception: {migrateEx.InnerException.Message}");
            }
            
            // Check if the error is about pending model changes (which we handle manually)
            if (migrateEx.Message.Contains("pending changes", StringComparison.OrdinalIgnoreCase) ||
                migrateEx.Message.Contains("The model for context", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("[INFO] Migration system detected model changes.");
                Console.WriteLine("[INFO] These will be handled automatically via column checks in STEP 3.");
                Console.WriteLine("[INFO] Continuing with initialization...");
                Console.WriteLine("========================================");
                // Don't throw - we'll handle the columns manually
            }
            else
            {
                Console.WriteLine($"[ERROR] Stack trace: {migrateEx.StackTrace}");
                Console.WriteLine("[ERROR] Migration errors must be resolved before the application can start.");
                Console.WriteLine("========================================");
                throw; // Re-throw other migration errors - app should not start with failed migrations
            }
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
            
            // Check and add TimeSelected column to appointments table (case-insensitive check)
            Console.WriteLine("[INIT] Checking for TimeSelected column in appointments table...");
            command.CommandText = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = DATABASE() 
                AND TABLE_NAME = 'appointments' 
                AND UPPER(COLUMN_NAME) = 'TIMESELECTED'";
            
            var timeSelectedColumnExists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
            Console.WriteLine($"[INIT] TimeSelected column exists in appointments table: {timeSelectedColumnExists}");
            
            if (!timeSelectedColumnExists)
            {
                Console.WriteLine("[CRITICAL] TimeSelected column missing! Adding to appointments table...");
                
                try
                {
                    command.CommandText = @"ALTER TABLE `appointments` ADD COLUMN `TimeSelected` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP";
                    await command.ExecuteNonQueryAsync();
                    Console.WriteLine("[SUCCESS] TimeSelected column added successfully to appointments table!");
                    
                    // Update existing appointments to set TimeSelected based on Schedule
                    command.CommandText = @"
                        UPDATE `appointments` a
                        INNER JOIN `schedules` s ON a.ScheduleID = s.ScheduleID
                        SET a.TimeSelected = CONCAT(s.Date, ' ', s.StartTime)
                        WHERE a.TimeSelected = '0000-00-00 00:00:00' OR a.TimeSelected IS NULL";
                    await command.ExecuteNonQueryAsync();
                    Console.WriteLine("[SUCCESS] Updated existing appointments with TimeSelected values!");
                    
                    // Verify it was added
                    command.CommandText = @"
                        SELECT COUNT(*) 
                        FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_SCHEMA = DATABASE() 
                        AND TABLE_NAME = 'appointments' 
                        AND UPPER(COLUMN_NAME) = 'TIMESELECTED'";
                    var verifyResult = await command.ExecuteScalarAsync();
                    var verified = Convert.ToInt32(verifyResult) > 0;
                    Console.WriteLine($"[VERIFY] TimeSelected column verification: {(verified ? "EXISTS" : "STILL MISSING")}");
                    
                    if (!verified)
                    {
                        throw new Exception("TimeSelected column verification failed - column was not added successfully");
                    }
                    
                    // Mark migration as applied if both columns will be added
                    timeSelectedColumnExists = true; // Set flag since we just added it
                }
                catch (Exception addEx)
                {
                    // Check if it's a "duplicate column" error (column already exists) - that's OK
                    if (addEx.Message.Contains("Duplicate column name", StringComparison.OrdinalIgnoreCase) ||
                        addEx.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase) ||
                        addEx.Message.Contains("Duplicate", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("[INFO] TimeSelected column already exists (caught duplicate error).");
                        Console.WriteLine("[INFO] This is OK - column was likely added in a previous run.");
                    }
                    else
                    {
                        Console.WriteLine($"[CRITICAL ERROR] Failed to add TimeSelected column: {addEx.Message}");
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
                Console.WriteLine("[OK] TimeSelected column already exists in appointments table.");
            }
            
            // Check and add CreatedAt column to appointments table (case-insensitive check)
            Console.WriteLine("[INIT] Checking for CreatedAt column in appointments table...");
            command.CommandText = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = DATABASE() 
                AND TABLE_NAME = 'appointments' 
                AND UPPER(COLUMN_NAME) = 'CREATEDAT'";
            
            var createdAtColumnExists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
            Console.WriteLine($"[INIT] CreatedAt column exists in appointments table: {createdAtColumnExists}");
            
            if (!createdAtColumnExists)
            {
                Console.WriteLine("[CRITICAL] CreatedAt column missing! Adding to appointments table...");
                
                try
                {
                    command.CommandText = @"ALTER TABLE `appointments` ADD COLUMN `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP";
                    await command.ExecuteNonQueryAsync();
                    Console.WriteLine("[SUCCESS] CreatedAt column added successfully to appointments table!");
                    
                    // Update existing appointments to set CreatedAt to current time
                    command.CommandText = @"
                        UPDATE `appointments`
                        SET CreatedAt = NOW()
                        WHERE CreatedAt = '0000-00-00 00:00:00' OR CreatedAt IS NULL";
                    await command.ExecuteNonQueryAsync();
                    Console.WriteLine("[SUCCESS] Updated existing appointments with CreatedAt values!");
                    
                    // Verify it was added
                    command.CommandText = @"
                        SELECT COUNT(*) 
                        FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_SCHEMA = DATABASE() 
                        AND TABLE_NAME = 'appointments' 
                        AND UPPER(COLUMN_NAME) = 'CREATEDAT'";
                    var verifyResult = await command.ExecuteScalarAsync();
                    var verified = Convert.ToInt32(verifyResult) > 0;
                    Console.WriteLine($"[VERIFY] CreatedAt column verification: {(verified ? "EXISTS" : "STILL MISSING")}");
                    
                    if (!verified)
                    {
                        throw new Exception("CreatedAt column verification failed - column was not added successfully");
                    }
                    
                    // Mark migration as applied if both columns are now present
                    createdAtColumnExists = true; // Set flag since we just added it
                }
                catch (Exception addEx)
                {
                    // Check if it's a "duplicate column" error (column already exists) - that's OK
                    if (addEx.Message.Contains("Duplicate column name", StringComparison.OrdinalIgnoreCase) ||
                        addEx.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase) ||
                        addEx.Message.Contains("Duplicate", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("[INFO] CreatedAt column already exists (caught duplicate error).");
                        Console.WriteLine("[INFO] This is OK - column was likely added in a previous run.");
                    }
                    else
                    {
                        Console.WriteLine($"[CRITICAL ERROR] Failed to add CreatedAt column: {addEx.Message}");
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
                Console.WriteLine("[OK] CreatedAt column already exists in appointments table.");
            }
            
            // Mark the migration as applied in the migration history table if both columns exist
            // Re-check to make sure both columns actually exist now
            command.CommandText = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = DATABASE() 
                AND TABLE_NAME = 'appointments' 
                AND UPPER(COLUMN_NAME) IN ('TIMESELECTED', 'CREATEDAT')";
            var bothColumnsExistCount = Convert.ToInt32(await command.ExecuteScalarAsync());
            var bothColumnsExist = bothColumnsExistCount == 2;
            
            if (bothColumnsExist)
            {
                Console.WriteLine("[INIT] Checking if migration is marked as applied...");
                command.CommandText = @"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = '__EFMigrationsHistory'";
                var historyTableExists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
                
                if (historyTableExists)
                {
                    command.CommandText = @"
                        SELECT COUNT(*) 
                        FROM __EFMigrationsHistory 
                        WHERE MigrationId = '20250125000000_AddTimeSelectedAndCreatedAtToAppointment'";
                    var migrationExists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
                    
                    if (!migrationExists)
                    {
                        Console.WriteLine("[INIT] Marking migration as applied in history table...");
                        command.CommandText = @"
                            INSERT IGNORE INTO __EFMigrationsHistory (MigrationId, ProductVersion)
                            VALUES ('20250125000000_AddTimeSelectedAndCreatedAtToAppointment', '9.0.9')";
                        await command.ExecuteNonQueryAsync();
                        Console.WriteLine("[SUCCESS] Migration marked as applied!");
                    }
                    else
                    {
                        Console.WriteLine("[OK] Migration already marked as applied.");
                    }
                }
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

            // Check and add Birthdate, Gender, and Image columns to clinicstaff table
            Console.WriteLine("[INIT] Checking for Birthdate, Gender, and Image columns in clinicstaff table...");
            
            // Check Birthdate
            command.CommandText = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = DATABASE() 
                AND TABLE_NAME = 'clinicstaff' 
                AND UPPER(COLUMN_NAME) = 'BIRTHDATE'";
            var clinicStaffBirthdateExists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
            if (!clinicStaffBirthdateExists)
            {
                Console.WriteLine("[INIT] Adding Birthdate column to clinicstaff table...");
                try
                {
                    command.CommandText = "ALTER TABLE `clinicstaff` ADD COLUMN `Birthdate` date NULL";
                    await command.ExecuteNonQueryAsync();
                    Console.WriteLine("[SUCCESS] Birthdate column added to clinicstaff table!");
                }
                catch (Exception addEx)
                {
                    if (addEx.Message.Contains("Duplicate column name", StringComparison.OrdinalIgnoreCase) ||
                        addEx.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase) ||
                        addEx.Message.Contains("Duplicate", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("[INFO] Birthdate column already exists (caught duplicate error).");
                    }
                    else
                    {
                        Console.WriteLine($"[ERROR] Failed to add Birthdate column to clinicstaff: {addEx.Message}");
                    }
                }
            }
            else
            {
                Console.WriteLine("[OK] Birthdate column already exists in clinicstaff table.");
            }

            // Check Gender
            command.CommandText = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = DATABASE() 
                AND TABLE_NAME = 'clinicstaff' 
                AND UPPER(COLUMN_NAME) = 'GENDER'";
            var clinicStaffGenderExists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
            if (!clinicStaffGenderExists)
            {
                Console.WriteLine("[INIT] Adding Gender column to clinicstaff table...");
                try
                {
                    command.CommandText = "ALTER TABLE `clinicstaff` ADD COLUMN `Gender` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL";
                    await command.ExecuteNonQueryAsync();
                    Console.WriteLine("[SUCCESS] Gender column added to clinicstaff table!");
                }
                catch (Exception addEx)
                {
                    if (addEx.Message.Contains("Duplicate column name", StringComparison.OrdinalIgnoreCase) ||
                        addEx.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase) ||
                        addEx.Message.Contains("Duplicate", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("[INFO] Gender column already exists (caught duplicate error).");
                    }
                    else
                    {
                        Console.WriteLine($"[ERROR] Failed to add Gender column to clinicstaff: {addEx.Message}");
                    }
                }
            }
            else
            {
                Console.WriteLine("[OK] Gender column already exists in clinicstaff table.");
            }

            // Check Image
            command.CommandText = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = DATABASE() 
                AND TABLE_NAME = 'clinicstaff' 
                AND UPPER(COLUMN_NAME) = 'IMAGE'";
            var clinicStaffImageExists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
            if (!clinicStaffImageExists)
            {
                Console.WriteLine("[INIT] Adding Image column to clinicstaff table...");
                try
                {
                    command.CommandText = "ALTER TABLE `clinicstaff` ADD COLUMN `Image` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL";
                    await command.ExecuteNonQueryAsync();
                    Console.WriteLine("[SUCCESS] Image column added to clinicstaff table!");
                }
                catch (Exception addEx)
                {
                    if (addEx.Message.Contains("Duplicate column name", StringComparison.OrdinalIgnoreCase) ||
                        addEx.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase) ||
                        addEx.Message.Contains("Duplicate", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("[INFO] Image column already exists (caught duplicate error).");
                    }
                    else
                    {
                        Console.WriteLine($"[ERROR] Failed to add Image column to clinicstaff: {addEx.Message}");
                    }
                }
            }
            else
            {
                Console.WriteLine("[OK] Image column already exists in clinicstaff table.");
            }

            // Check and add vitals columns to precords table
            Console.WriteLine("[INIT] Checking for vitals columns in precords table...");
            
            // Check PulseRate
            command.CommandText = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = DATABASE() 
                AND TABLE_NAME = 'precords' 
                AND UPPER(COLUMN_NAME) = 'PULSERATE'";
            var pulseRateExists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
            if (!pulseRateExists)
            {
                Console.WriteLine("[INIT] Adding PulseRate column to precords table...");
                command.CommandText = "ALTER TABLE `precords` ADD COLUMN `PulseRate` int(50) NULL";
                await command.ExecuteNonQueryAsync();
                Console.WriteLine("[SUCCESS] PulseRate column added to precords table!");
            }
            else
            {
                Console.WriteLine("[OK] PulseRate column already exists in precords table.");
            }

            // Check BloodPressure
            command.CommandText = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = DATABASE() 
                AND TABLE_NAME = 'precords' 
                AND UPPER(COLUMN_NAME) = 'BLOODPRESSURE'";
            var bloodPressureExists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
            if (!bloodPressureExists)
            {
                Console.WriteLine("[INIT] Adding BloodPressure column to precords table...");
                command.CommandText = "ALTER TABLE `precords` ADD COLUMN `BloodPressure` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL";
                await command.ExecuteNonQueryAsync();
                Console.WriteLine("[SUCCESS] BloodPressure column added to precords table!");
            }
            else
            {
                Console.WriteLine("[OK] BloodPressure column already exists in precords table.");
            }

            // Check Temperature
            command.CommandText = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = DATABASE() 
                AND TABLE_NAME = 'precords' 
                AND UPPER(COLUMN_NAME) = 'TEMPERATURE'";
            var temperatureExists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
            if (!temperatureExists)
            {
                Console.WriteLine("[INIT] Adding Temperature column to precords table...");
                command.CommandText = "ALTER TABLE `precords` ADD COLUMN `Temperature` decimal(5,2) NULL";
                await command.ExecuteNonQueryAsync();
                Console.WriteLine("[SUCCESS] Temperature column added to precords table!");
            }
            else
            {
                Console.WriteLine("[OK] Temperature column already exists in precords table.");
            }

            // Check RespiratoryRate
            command.CommandText = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = DATABASE() 
                AND TABLE_NAME = 'precords' 
                AND UPPER(COLUMN_NAME) = 'RESPIRATORYRATE'";
            var respiratoryRateExists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
            if (!respiratoryRateExists)
            {
                Console.WriteLine("[INIT] Adding RespiratoryRate column to precords table...");
                command.CommandText = "ALTER TABLE `precords` ADD COLUMN `RespiratoryRate` int(50) NULL";
                await command.ExecuteNonQueryAsync();
                Console.WriteLine("[SUCCESS] RespiratoryRate column added to precords table!");
            }
            else
            {
                Console.WriteLine("[OK] RespiratoryRate column already exists in precords table.");
            }

            // Check OxygenSaturation
            command.CommandText = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = DATABASE() 
                AND TABLE_NAME = 'precords' 
                AND UPPER(COLUMN_NAME) = 'OXYGENSATURATION'";
            var oxygenSaturationExists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
            if (!oxygenSaturationExists)
            {
                Console.WriteLine("[INIT] Adding OxygenSaturation column to precords table...");
                command.CommandText = "ALTER TABLE `precords` ADD COLUMN `OxygenSaturation` int(50) NULL";
                await command.ExecuteNonQueryAsync();
                Console.WriteLine("[SUCCESS] OxygenSaturation column added to precords table!");
            }
            else
            {
                Console.WriteLine("[OK] OxygenSaturation column already exists in precords table.");
            }

            // Check and add Birthdate, Gender, and Image columns to students table
            Console.WriteLine("[INIT] Checking for Birthdate, Gender, and Image columns in students table...");
            
            // Check Birthdate
            command.CommandText = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = DATABASE() 
                AND TABLE_NAME = 'students' 
                AND UPPER(COLUMN_NAME) = 'BIRTHDATE'";
            var birthdateExists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
            if (!birthdateExists)
            {
                Console.WriteLine("[INIT] Adding Birthdate column to students table...");
                command.CommandText = "ALTER TABLE `students` ADD COLUMN `Birthdate` date NULL";
                await command.ExecuteNonQueryAsync();
                Console.WriteLine("[SUCCESS] Birthdate column added to students table!");
            }
            else
            {
                Console.WriteLine("[OK] Birthdate column already exists in students table.");
            }

            // Check Gender
            command.CommandText = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = DATABASE() 
                AND TABLE_NAME = 'students' 
                AND UPPER(COLUMN_NAME) = 'GENDER'";
            var genderExists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
            if (!genderExists)
            {
                Console.WriteLine("[INIT] Adding Gender column to students table...");
                command.CommandText = "ALTER TABLE `students` ADD COLUMN `Gender` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL";
                await command.ExecuteNonQueryAsync();
                Console.WriteLine("[SUCCESS] Gender column added to students table!");
            }
            else
            {
                Console.WriteLine("[OK] Gender column already exists in students table.");
            }

            // Check Image
            command.CommandText = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = DATABASE() 
                AND TABLE_NAME = 'students' 
                AND UPPER(COLUMN_NAME) = 'IMAGE'";
            var imageExists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
            if (!imageExists)
            {
                Console.WriteLine("[INIT] Adding Image column to students table...");
                command.CommandText = "ALTER TABLE `students` ADD COLUMN `Image` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL";
                await command.ExecuteNonQueryAsync();
                Console.WriteLine("[SUCCESS] Image column added to students table!");
            }
            else
            {
                Console.WriteLine("[OK] Image column already exists in students table.");
            }

            // Check InsuranceReceipt
            command.CommandText = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = DATABASE() 
                AND TABLE_NAME = 'students' 
                AND UPPER(COLUMN_NAME) = 'INSURANCERECEIPT'";
            var insuranceReceiptExists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
            if (!insuranceReceiptExists)
            {
                Console.WriteLine("[INIT] Adding InsuranceReceipt column to students table...");
                command.CommandText = "ALTER TABLE `students` ADD COLUMN `InsuranceReceipt` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL";
                await command.ExecuteNonQueryAsync();
                Console.WriteLine("[SUCCESS] InsuranceReceipt column added to students table!");
            }
            else
            {
                Console.WriteLine("[OK] InsuranceReceipt column already exists in students table.");
            }

            // Check EmergencyContactName
            command.CommandText = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = DATABASE() 
                AND TABLE_NAME = 'students' 
                AND UPPER(COLUMN_NAME) = 'EMERGENCYCONTACTNAME'";
            var emergencyContactNameExists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
            if (!emergencyContactNameExists)
            {
                Console.WriteLine("[INIT] Adding EmergencyContactName column to students table...");
                command.CommandText = "ALTER TABLE `students` ADD COLUMN `EmergencyContactName` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL";
                await command.ExecuteNonQueryAsync();
                Console.WriteLine("[SUCCESS] EmergencyContactName column added to students table!");
            }
            else
            {
                Console.WriteLine("[OK] EmergencyContactName column already exists in students table.");
            }

            // Check EmergencyContactRelationship
            command.CommandText = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = DATABASE() 
                AND TABLE_NAME = 'students' 
                AND UPPER(COLUMN_NAME) = 'EMERGENCYCONTACTRELATIONSHIP'";
            var emergencyContactRelationshipExists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
            if (!emergencyContactRelationshipExists)
            {
                Console.WriteLine("[INIT] Adding EmergencyContactRelationship column to students table...");
                command.CommandText = "ALTER TABLE `students` ADD COLUMN `EmergencyContactRelationship` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL";
                await command.ExecuteNonQueryAsync();
                Console.WriteLine("[SUCCESS] EmergencyContactRelationship column added to students table!");
            }
            else
            {
                Console.WriteLine("[OK] EmergencyContactRelationship column already exists in students table.");
            }

            // Check EmergencyContactPhoneNumber
            command.CommandText = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = DATABASE() 
                AND TABLE_NAME = 'students' 
                AND UPPER(COLUMN_NAME) = 'EMERGENCYCONTACTPHONENUMBER'";
            var emergencyContactPhoneNumberExists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
            if (!emergencyContactPhoneNumberExists)
            {
                Console.WriteLine("[INIT] Adding EmergencyContactPhoneNumber column to students table...");
                command.CommandText = "ALTER TABLE `students` ADD COLUMN `EmergencyContactPhoneNumber` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL";
                await command.ExecuteNonQueryAsync();
                Console.WriteLine("[SUCCESS] EmergencyContactPhoneNumber column added to students table!");
            }
            else
            {
                Console.WriteLine("[OK] EmergencyContactPhoneNumber column already exists in students table.");
            }

            // Update precords table with age and gender from students table
            Console.WriteLine("[INIT] Updating precords table with age and gender from students table...");
            try
            {
                // Update Gender from students table where PatientID matches StudentID
                command.CommandText = @"
                    UPDATE precords p
                    INNER JOIN students s ON p.PatientID = s.StudentID
                    SET p.Gender = s.Gender
                    WHERE s.Gender IS NOT NULL 
                    AND s.Gender != '' 
                    AND (p.Gender IS NULL OR p.Gender = '' OR p.Gender = 'Not specified')";
                var genderUpdated = await command.ExecuteNonQueryAsync();
                Console.WriteLine($"[SUCCESS] Updated Gender for {genderUpdated} precords from students table!");

                // Update Age from students table (calculate from Birthdate)
                // Calculate age correctly: subtract years, then subtract 1 if birthday hasn't occurred this year
                // Also fix any records with -1 or invalid ages
                command.CommandText = @"
                    UPDATE precords p
                    INNER JOIN students s ON p.PatientID = s.StudentID
                    SET p.Age = GREATEST(0, 
                        YEAR(CURDATE()) - YEAR(s.Birthdate) - 
                        (DATE_FORMAT(CURDATE(), '%m%d') < DATE_FORMAT(s.Birthdate, '%m%d'))
                    )
                    WHERE s.Birthdate IS NOT NULL 
                    AND s.Birthdate <= CURDATE()
                    AND (p.Age = 0 OR p.Age IS NULL OR p.Age < 0)";
                var ageUpdated = await command.ExecuteNonQueryAsync();
                Console.WriteLine($"[SUCCESS] Updated Age for {ageUpdated} precords from students table (calculated from Birthdate)!");
                
                // Fix any remaining records with negative age (safety check)
                command.CommandText = @"
                    UPDATE precords p
                    INNER JOIN students s ON p.PatientID = s.StudentID
                    SET p.Age = GREATEST(0, 
                        YEAR(CURDATE()) - YEAR(s.Birthdate) - 
                        (DATE_FORMAT(CURDATE(), '%m%d') < DATE_FORMAT(s.Birthdate, '%m%d'))
                    )
                    WHERE s.Birthdate IS NOT NULL 
                    AND s.Birthdate <= CURDATE()
                    AND p.Age < 0";
                var negativeAgeFixed = await command.ExecuteNonQueryAsync();
                if (negativeAgeFixed > 0)
                {
                    Console.WriteLine($"[FIXED] Corrected {negativeAgeFixed} precords with negative age values!");
                }
            }
            catch (Exception updateEx)
            {
                Console.WriteLine($"[WARNING] Failed to update precords with age/gender from students: {updateEx.Message}");
                // Don't throw - this is not critical for app startup
            }

            // Check and create DataProtectionKeys table if it doesn't exist
            Console.WriteLine("[INIT] Checking for DataProtectionKeys table...");
            command.CommandText = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_SCHEMA = DATABASE() 
                AND TABLE_NAME = 'DataProtectionKeys'";
            var dataProtectionKeysTableExists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
            
            if (!dataProtectionKeysTableExists)
            {
                Console.WriteLine("[INIT] DataProtectionKeys table does not exist. Creating it...");
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS `DataProtectionKeys` (
                        `Id` int NOT NULL AUTO_INCREMENT,
                        `FriendlyName` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
                        `Xml` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
                        CONSTRAINT `PRIMARY` PRIMARY KEY (`Id`)
                    ) CHARACTER SET=utf8mb4 COLLATE=utf8mb4_general_ci";
                
                try
                {
                    await command.ExecuteNonQueryAsync();
                    Console.WriteLine("[SUCCESS] DataProtectionKeys table created successfully!");
                }
                catch (Exception createEx)
                {
                    Console.WriteLine($"[ERROR] Failed to create DataProtectionKeys table: {createEx.Message}");
                    Console.WriteLine($"[ERROR] Stack trace: {createEx.StackTrace}");
                    // Don't throw - migrations may create it
                }
            }
            else
            {
                Console.WriteLine("[OK] DataProtectionKeys table already exists.");
            }

            // Ensure at least one Data Protection key exists (will be auto-generated by Data Protection if none exist)
            // This helps prevent "key not found" errors after deployments
            try
            {
                command.CommandText = @"SELECT COUNT(*) FROM `DataProtectionKeys`";
                var keyCount = Convert.ToInt32(await command.ExecuteScalarAsync());
                Console.WriteLine($"[INIT] DataProtectionKeys count: {keyCount}");
                if (keyCount == 0)
                {
                    Console.WriteLine("[INFO] No Data Protection keys found. Keys will be auto-generated on first use.");
                }
            }
            catch (Exception keyCheckEx)
            {
                Console.WriteLine($"[WARNING] Could not check Data Protection keys: {keyCheckEx.Message}");
            }

            // Check and create emergencies table if it doesn't exist
            Console.WriteLine("[INIT] Checking for emergencies table...");
            command.CommandText = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_SCHEMA = DATABASE() 
                AND TABLE_NAME = 'emergencies'";
            var emergenciesTableExists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
            
            if (!emergenciesTableExists)
            {
                Console.WriteLine("[INIT] emergencies table does not exist. Creating it...");
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS `emergencies` (
                        `EmergencyID` int(100) NOT NULL AUTO_INCREMENT,
                        `StudentID` int(100) NULL,
                        `StudentName` text CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
                        `StudentIdNumber` int(100) NOT NULL,
                        `Location` text CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
                        `Needs` text CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
                        `IsResolved` tinyint(1) NOT NULL DEFAULT 0,
                        `CreatedAt` timestamp(6) NOT NULL DEFAULT current_timestamp(6),
                        CONSTRAINT `PRIMARY` PRIMARY KEY (`EmergencyID`),
                        KEY `StudentID` (`StudentID`),
                        CONSTRAINT `emergencies_ibfk_1` FOREIGN KEY (`StudentID`) REFERENCES `students` (`StudentID`)
                    ) CHARACTER SET=utf8mb4 COLLATE=utf8mb4_general_ci";
                
                try
                {
                    await command.ExecuteNonQueryAsync();
                    Console.WriteLine("[SUCCESS] emergencies table created successfully!");
                    
                    // Verify it was created
                    command.CommandText = @"
                        SELECT COUNT(*) 
                        FROM INFORMATION_SCHEMA.TABLES 
                        WHERE TABLE_SCHEMA = DATABASE() 
                        AND TABLE_NAME = 'emergencies'";
                    var verifyResult = await command.ExecuteScalarAsync();
                    var verified = Convert.ToInt32(verifyResult) > 0;
                    Console.WriteLine($"[VERIFY] emergencies table verification: {(verified ? "EXISTS" : "STILL MISSING")}");
                    
                    if (!verified)
                    {
                        throw new Exception("emergencies table verification failed - table was not created successfully");
                    }
                }
                catch (Exception createEx)
                {
                    Console.WriteLine($"[ERROR] Failed to create emergencies table: {createEx.Message}");
                    Console.WriteLine($"[ERROR] Stack trace: {createEx.StackTrace}");
                    throw;
                }
            }
            else
            {
                Console.WriteLine("[OK] emergencies table already exists.");
            }

            // Check and add new columns to emergencies table if they don't exist
            Console.WriteLine("[INIT] Checking for new columns in emergencies table...");
            
            // Check StudentID
            command.CommandText = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = DATABASE() 
                AND TABLE_NAME = 'emergencies' 
                AND UPPER(COLUMN_NAME) = 'STUDENTID'";
            var studentIdExists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
            if (!studentIdExists)
            {
                Console.WriteLine("[INIT] Adding StudentID column to emergencies table...");
                command.CommandText = "ALTER TABLE `emergencies` ADD COLUMN `StudentID` int(100) NULL";
                await command.ExecuteNonQueryAsync();
                Console.WriteLine("[SUCCESS] StudentID column added to emergencies table!");
            }
            else
            {
                Console.WriteLine("[OK] StudentID column already exists in emergencies table.");
            }

            // Check StudentName
            command.CommandText = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = DATABASE() 
                AND TABLE_NAME = 'emergencies' 
                AND UPPER(COLUMN_NAME) = 'STUDENTNAME'";
            var studentNameExists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
            if (!studentNameExists)
            {
                Console.WriteLine("[INIT] Adding StudentName column to emergencies table...");
                command.CommandText = "ALTER TABLE `emergencies` ADD COLUMN `StudentName` text CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL";
                await command.ExecuteNonQueryAsync();
                Console.WriteLine("[SUCCESS] StudentName column added to emergencies table!");
            }
            else
            {
                Console.WriteLine("[OK] StudentName column already exists in emergencies table.");
            }

            // Check StudentIdNumber
            command.CommandText = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = DATABASE() 
                AND TABLE_NAME = 'emergencies' 
                AND UPPER(COLUMN_NAME) = 'STUDENTIDNUMBER'";
            var studentIdNumberExists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
            if (!studentIdNumberExists)
            {
                Console.WriteLine("[INIT] Adding StudentIdNumber column to emergencies table...");
                command.CommandText = "ALTER TABLE `emergencies` ADD COLUMN `StudentIdNumber` int(100) NOT NULL";
                await command.ExecuteNonQueryAsync();
                Console.WriteLine("[SUCCESS] StudentIdNumber column added to emergencies table!");
            }
            else
            {
                Console.WriteLine("[OK] StudentIdNumber column already exists in emergencies table.");
            }

            // Check IsResolved
            command.CommandText = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = DATABASE() 
                AND TABLE_NAME = 'emergencies' 
                AND UPPER(COLUMN_NAME) = 'ISRESOLVED'";
            var isResolvedExists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
            if (!isResolvedExists)
            {
                Console.WriteLine("[INIT] Adding IsResolved column to emergencies table...");
                command.CommandText = "ALTER TABLE `emergencies` ADD COLUMN `IsResolved` tinyint(1) NOT NULL DEFAULT 0";
                await command.ExecuteNonQueryAsync();
                Console.WriteLine("[SUCCESS] IsResolved column added to emergencies table!");
            }
            else
            {
                Console.WriteLine("[OK] IsResolved column already exists in emergencies table.");
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