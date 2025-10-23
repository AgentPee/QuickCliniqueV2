using Microsoft.EntityFrameworkCore;
using QuickClinique.Models;
using QuickClinique.Services;
using QuickClinique.Middleware;

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

// Apply pending migrations
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();
}

// Database initialization and seeding
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var seedingService = services.GetRequiredService<IDataSeedingService>();

        // Apply migrations
        context.Database.Migrate();

        // Seed initial data
        await seedingService.SeedDataAsync();

        Console.WriteLine("Database initialized and seeded successfully!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred initializing the database: {ex.Message}");
    }
}

app.Run();