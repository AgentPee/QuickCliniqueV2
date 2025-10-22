using Microsoft.EntityFrameworkCore;
using QuickClinique.Models;
using QuickClinique.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add email service
builder.Services.AddScoped<IEmailService, EmailService>();

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
    dbContext.Database.Migrate();
}

// Database initialization
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        // Use Migrate() if you have EF Core migrations
        // Use EnsureCreated() if you don't have migrations yet
        context.Database.EnsureCreated(); // or context.Database.EnsureCreated();

        Console.WriteLine("Database created/migrated successfully!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred creating the database: {ex.Message}");
    }
}

app.Run();