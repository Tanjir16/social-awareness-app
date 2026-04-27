using Microsoft.EntityFrameworkCore;
using SocialMediaApp.Data;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/socialapp-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<SocialDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;


    //----Modified By TANJIR For Security Concerns-------

    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // only send cookie over HTTPS
    options.Cookie.SameSite = SameSiteMode.Strict;           // prevents CSRF attacks
});

builder.Services.AddControllersWithViews();
builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173", "https://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Always use HTTPS redirection----Modified By TANJIR For Security Concerns-------
app.UseHttpsRedirection();




// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    //app.UseHttpsRedirection(); // duplicate No Need
}
app.UseStaticFiles();
app.UseRouting();
app.UseCors();
app.UseSession();


// ----Modified By TANJIR For Security Concerns-------

app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "no-referrer");
    await next();
});

// ----Modified By TANJIR For Security Concerns-------
// Unauthorized access logging
app.Use(async (context, next) =>
{
    if (context.Response.StatusCode == 401 || context.Response.StatusCode == 403)
    {
        Log.Warning("Unauthorized access attempt to {Path}", context.Request.Path);
    }
    await next();
});



app.UseAuthorization();

app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Ensure roles exist on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SocialDbContext>();
    db.Database.EnsureCreated();
    RoleSeeder.SeedRolesAsync(db).GetAwaiter().GetResult();
}

app.MapFallbackToFile("index.html");
app.Run();
