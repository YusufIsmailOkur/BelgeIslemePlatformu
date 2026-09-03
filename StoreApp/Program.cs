using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using StoreApp.Authorization;
using StoreApp.Data;
using StoreApp.Services;
using StoreApp.Services.Abstractions;
using StoreApp.Services.Parsing;

// Windows-1254 gibi eski kod sayfalarını (BOM'suz Türkçe CSV tespiti için) kullanılabilir kılar.
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Not: MVP boyunca SQLite kullanılıyor (yerelde PostgreSQL kurulu olmadığından).
// docs/ARCHITECTURE.md PostgreSQL/Npgsql kararını içerir; geçiş sonraki bir haftaya bırakıldı.
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("Default"));
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddSingleton<IFileValidationService, FileValidationService>();
builder.Services.AddSingleton<IFileStorageService, FileStorageService>();
builder.Services.AddSingleton<IDocumentContentParser, PdfDocumentParser>();
builder.Services.AddSingleton<IDocumentContentParser, ExcelDocumentParser>();
builder.Services.AddSingleton<IDocumentContentParser, CsvDocumentParser>();
builder.Services.AddScoped<IDocumentProcessingService, DocumentProcessingService>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.OperatorOrAbove, policy => policy.RequireRole(Policies.OperatorOrAboveRoles));
    options.AddPolicy(Policies.ManagerOrAbove, policy => policy.RequireRole(Policies.ManagerOrAboveRoles));
    options.AddPolicy(Policies.SystemAdminOnly, policy => policy.RequireRole(Policies.SystemAdminOnlyRoles));
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name:"default",
    pattern:"{controller=Home}/{action=Index}/{id?}");

app.Run();
