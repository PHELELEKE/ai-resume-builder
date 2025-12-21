using MGSINTERNETS.Models;
using MGSINTERNETS.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore; // Make sure this is included
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add PDF service
builder.Services.AddScoped<PdfService>();

// Add localization for South Africa (ZAR)
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[]
    {
        new CultureInfo("en-ZA")
    };
    options.DefaultRequestCulture = new RequestCulture("en-ZA");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

// Add Query Performance Service
builder.Services.AddSingleton<QueryPerformanceService>();
builder.Services.AddSingleton<IObserver<KeyValuePair<string, object>>>(sp =>
    sp.GetRequiredService<QueryPerformanceService>());

// Add Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add SMS Service
builder.Services.AddScoped<ISmsService, MockSmsService>();

// ✅ SIMPLE DATABASE SETUP - ONLY SQLite (Like your working app)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=mgsinternets.db"));
Console.WriteLine("✅ Using SQLite database - no connection issues!");

// Add HttpContextAccessor
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.UseSession();
app.UseRequestLocalization();

// ✅ SIMPLE DATABASE SETUP (Like your working app)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();
        Console.WriteLine("✅ Database ready!");
        
        // Optional: Seed data if needed
        // DataSeeder.Initialize(context);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Database warning: {ex.Message}");
    }
}

// Routes
app.MapControllerRoute(
    name: "reset",
    pattern: "reset/{token}",
    defaults: new { controller = "Users", action = "ResetPassword" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
