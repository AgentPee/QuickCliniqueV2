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

// Ensure database name is present and not empty
var dbNameMatch = System.Text.RegularExpressions.Regex.Match(connectionString, @"Database=([^;]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
if (!dbNameMatch.Success || string.IsNullOrWhiteSpace(dbNameMatch.Groups[1].Value))
{
    Console.WriteLine("[WARNING] Connection string missing or has empty Database parameter.");
    Console.WriteLine("[WARNING] Railway MySQL typically uses 'railway' as the database name.");
    
    // Remove existing empty Database parameter if present
    connectionString = System.Text.RegularExpressions.Regex.Replace(
        connectionString, 
        @"Database=[^;]*;?", 
        "", 
        System.Text.RegularExpressions.RegexOptions.IgnoreCase
    );
    
    // Add the database name (Railway MySQL usually uses 'railway or the service name)
    connectionString += "Database=railway;";
    Console.WriteLine("[INFO] Added Database=railway to connection string.");
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
            // Use the connection string from the outer scope (already configured with SSL)
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new Exception("Connection string 'DefaultConnection' is not configured.");
            }

            // Log connection string (without password for security)
            var safeConnectionString = System.Text.RegularExpressions.Regex.Replace(
                connectionString, 
                @"Pwd=[^;]+", 
                "Pwd=***", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
            Console.WriteLine($"[INIT] Connection string: {safeConnectionString}");

            // Check if connection string has unresolved template variables
            if (connectionString.Contains("${{") || connectionString.Contains("${"))
            {
                Console.WriteLine("[WARNING] Connection string appears to have unresolved template variables!");
                Console.WriteLine("[WARNING] Make sure you're using the actual MySQL service variable names in Railway.");
                Console.WriteLine("[WARNING] Skipping database creation check - Railway MySQL should already have the database.");
                // Skip database creation - Railway MySQL already creates it
            }
            else
            {
                // Parse database name from connection string (reuse the match from outer scope)
                var dbName = "QuickClinique"; // Default
                if (dbNameMatch.Success && !string.IsNullOrWhiteSpace(dbNameMatch.Groups[1].Value))
                {
                    dbName = dbNameMatch.Groups[1].Value;
                }

                // Create connection string without database, add SSL mode if not present
                var serverConnectionString = connectionString;
                if (dbNameMatch.Success)
                {
                    serverConnectionString = System.Text.RegularExpressions.Regex.Replace(connectionString, @"Database=[^;]+;?", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
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
                dbNameParam.Value = dbName;
                checkDbCommand.Parameters.Add(dbNameParam);

                var dbExists = Convert.ToInt32(await checkDbCommand.ExecuteScalarAsync()) > 0;
                Console.WriteLine($"[INIT] Database '{dbName}' exists: {dbExists}");

                if (!dbExists)
                {
                    Console.WriteLine($"[INIT] Database '{dbName}' does not exist. Creating it...");
                    using var createDbCommand = serverConnection.CreateCommand();
                    createDbCommand.CommandText = $"CREATE DATABASE IF NOT EXISTS `{dbName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci";
                    await createDbCommand.ExecuteNonQueryAsync();
                    Console.WriteLine($"[SUCCESS] Database '{dbName}' created successfully!");
                }
                else
                {
                    Console.WriteLine($"[OK] Database '{dbName}' already exists.");
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
                command.CommandText = @"ALTER TABLE `appointments` ADD COLUMN `TriageNotes` longtext NOT NULL DEFAULT ''";
                
                try
                {
                    await command.ExecuteNonQueryAsync();
                    Console.WriteLine("[SUCCESS] TriageNotes column added successfully to appointments table!");
                    
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
                command.CommandText = @"ALTER TABLE `appointments` ADD COLUMN `Symptoms` longtext CHARACTER SET utf8mb4 NOT NULL DEFAULT ''";
                
                try
                {
                    await command.ExecuteNonQueryAsync();
                    Console.WriteLine("[SUCCESS] Symptoms column added successfully to appointments table!");
                    
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
            command.CommandText = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = DATABASE() 
                AND TABLE_NAME = 'appointments' 
                AND UPPER(COLUMN_NAME) = 'CANCELLATIONREASON'";
            
            var cancellationReasonColumnExists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
            Console.WriteLine($"[INIT] CancellationReason column exists in appointments table: {cancellationReasonColumnExists}");
            
            if (!cancellationReasonColumnExists)
            {
                Console.WriteLine("[CRITICAL] CancellationReason column missing! Adding to appointments table...");
                command.CommandText = @"ALTER TABLE `appointments` ADD COLUMN `CancellationReason` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT ''";
                
                try
                {
                    await command.ExecuteNonQueryAsync();
                    Console.WriteLine("[SUCCESS] CancellationReason column added successfully to appointments table!");
                    
                    // Verify it was added
                    command.CommandText = @"
                        SELECT COUNT(*) 
                        FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_SCHEMA = DATABASE() 
                        AND TABLE_NAME = 'appointments' 
                        AND UPPER(COLUMN_NAME) = 'CANCELLATIONREASON'";
                    var verifyResult = await command.ExecuteScalarAsync();
                    var verified = Convert.ToInt32(verifyResult) > 0;
                    Console.WriteLine($"[VERIFY] CancellationReason column verification: {(verified ? "EXISTS" : "STILL MISSING")}");
                    
                    if (!verified)
                    {
                        throw new Exception("CancellationReason column verification failed - column was not added successfully");
                    }
                }
                catch (Exception addEx)
                {
                    Console.WriteLine($"[CRITICAL ERROR] Failed to add CancellationReason column: {addEx.Message}");
                    Console.WriteLine($"[CRITICAL ERROR] Stack trace: {addEx.StackTrace}");
                    Console.WriteLine("[CRITICAL ERROR] The application may not function correctly without this column!");
                    throw; // Re-throw to be caught by outer catch
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

        // Final verification: Ensure IsActive column exists in clinicstaff before seeding
        try
        {
            var connection = context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }
            
            using var verifyCommand = connection.CreateCommand();
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
                
                // Close connection to refresh EF Core cache
                await connection.CloseAsync();
            }
            else
            {
                Console.WriteLine("[FINAL CHECK] IsActive column verified in clinicstaff table.");
            }
        }
        catch (Exception verifyEx)
        {
            Console.WriteLine($"[FINAL CHECK ERROR] Failed to verify/add IsActive column: {verifyEx.Message}");
            throw; // Don't proceed with seeding if column doesn't exist
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