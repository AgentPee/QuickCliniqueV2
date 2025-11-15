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

        // Apply migrations
        try
        {
            context.Database.Migrate();
            Console.WriteLine("Migrations applied successfully!");
        }
        catch (Exception migrateEx)
        {
            Console.WriteLine($"Migration failed: {migrateEx.Message}");
            
            // Try to apply the IsActive column directly if migration fails
            try
            {
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'students' 
                    AND COLUMN_NAME = 'IsActive'";
                
                var columnExists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
                
                if (!columnExists)
                {
                    Console.WriteLine("Attempting to add IsActive column directly...");
                    command.CommandText = "ALTER TABLE `students` ADD COLUMN `IsActive` tinyint(1) NOT NULL DEFAULT 1";
                    await command.ExecuteNonQueryAsync();
                    Console.WriteLine("IsActive column added successfully to students table!");
                }
                else
                {
                    Console.WriteLine("IsActive column already exists in students table.");
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
                else
                {
                    Console.WriteLine("IsActive column already exists in clinicstaff table.");
                }
            }
            catch (Exception sqlEx)
            {
                Console.WriteLine($"Failed to add column directly: {sqlEx.Message}");
                throw; // Re-throw the original migration exception
            }
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