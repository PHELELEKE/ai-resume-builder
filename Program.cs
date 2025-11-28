using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using AIResumeBuilder.Data;
using AIResumeBuilder.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// FLEXIBLE DATABASE SETUP - Try SQL Server, fallback to SQLite
if (!string.IsNullOrEmpty(connectionString) && connectionString.Contains("Server="))
{
    // Use SQL Server if connection string is provided
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));
    Console.WriteLine("✅ Using SQL Server database");
}
else
{
    // Fallback to SQLite for development/Railway without database
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite("Data Source=app.db"));
    Console.WriteLine("⚠️ Using SQLite fallback database");
}

// Add Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => 
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

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
app.UseAuthentication();
app.UseAuthorization();

// IMPROVED DATABASE INITIALIZATION
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        
        // Try to apply migrations first (for SQL Server)
        try 
        {
            context.Database.Migrate();
            Console.WriteLine("✅ Database migrations applied successfully!");
        }
        catch (Exception migrateEx)
        {
            // If migrations fail, ensure database exists
            Console.WriteLine($"⚠️ Migrations failed, ensuring database exists: {migrateEx.Message}");
            context.Database.EnsureCreated();
            Console.WriteLine("✅ Database ensured created!");
        }
        
        // Test database connection
        if (context.Database.CanConnect())
        {
            Console.WriteLine("✅ Database connection successful!");
        }
        else
        {
            Console.WriteLine("❌ Database connection failed!");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Database initialization error: {ex.Message}");
        // Don't crash the app - continue without database
    }
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");
app.MapRazorPages();

app.Run();
