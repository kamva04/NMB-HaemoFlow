using Microsoft.EntityFrameworkCore;
using NMB_HLabSys.Data;
using NMB_HLabSys.Filters;
using NMB_HLabSys.Services;

var builder = WebApplication.CreateBuilder(args);

// MVC with Razor runtime compilation
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<ForcePasswordChangeFilter>();
})
    .AddRazorRuntimeCompilation();

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

builder.Services.AddHttpContextAccessor();

// DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Application services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<PdfService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<IBarcodeService, BarcodeService>();

var app = builder.Build();

// =============================================================
// MIGRATE + SEED  (runs once at startup)
// =============================================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // 1. Apply any pending migrations
    db.Database.Migrate();

    // 2. Seed demo accounts — 5 per role, idempotent
    DatabaseSeeder.SeedRolesOnly(db);
}

// =============================================================
// MIDDLEWARE PIPELINE
// =============================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();