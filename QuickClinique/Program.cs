using Microsoft.EntityFrameworkCore;
using QuickClinique.Models;
using QuickClinique.Services;
using QuickClinique.Middleware;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add services
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IDataSeedingService, DataSeedingService>();

// DB Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 21))));

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

        // CRITICAL: Ensure required columns exist BEFORE any EF Core operations
        // This must run first to prevent "Unknown column" errors
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

            // Check and add IsActive column to clinicstaff table
            command.CommandText = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = DATABASE() 
                AND TABLE_NAME = 'clinicstaff' 
                AND COLUMN_NAME = 'IsActive'";
            
            var clinicStaffColumnExists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
            
            if (!clinicStaffColumnExists)
            {
                Console.WriteLine("Attempting to add IsActive column to clinicstaff table...");
                command.CommandText = "ALTER TABLE `clinicstaff` ADD COLUMN `IsActive` tinyint(1) NOT NULL DEFAULT 1";
                await command.ExecuteNonQueryAsync();
                Console.WriteLine("IsActive column added successfully to clinicstaff table!");
            }

        }
        catch (Exception columnEx)
        {
            Console.WriteLine($"[ERROR] Failed to check/add required columns: {columnEx.Message}");
            Console.WriteLine($"[ERROR] Stack trace: {columnEx.StackTrace}");
            // Don't throw - allow app to start, but log the error
        }

        // Check for pending migrations
        var pendingMigrations = context.Database.GetPendingMigrations().ToList();
        if (pendingMigrations.Any())
        {
            Console.WriteLine($"Applying {pendingMigrations.Count} pending migration(s):");
            foreach (var migration in pendingMigrations)
            {
                Console.WriteLine($"  - {migration}");
            }
        }

        // Apply migrations (after columns are ensured)
        try
        {
            context.Database.Migrate();
            Console.WriteLine("Migrations applied successfully!");
        }
        catch (Exception migrateEx)
        {
            Console.WriteLine($"Migration failed: {migrateEx.Message}");
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
app.Run();