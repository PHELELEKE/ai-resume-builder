using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using AIResumeBuilder.Data;
using AIResumeBuilder.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

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

// Create database and tables automatically (SINGLE VERSION)
// Create database and tables automatically
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        
        // Simple check - if database doesn't exist, create it
        if (!context.Database.CanConnect())
        {
            Console.WriteLine("🔄 Creating database...");
            context.Database.EnsureCreated();
            Console.WriteLine("✅ Database created successfully!");
        }
        else
        {
            Console.WriteLine("✅ Database already exists!");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Database warning: {ex.Message}");
        // Don't stop the app for database issues
    }
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();