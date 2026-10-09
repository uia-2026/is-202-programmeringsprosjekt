// Application entry point for the Heimevernet web application.
// Configures DI, DbContext and maps default endpoints before running the web host.
using Heimevernet.Data;
using Heimevernet.Services;
using Heimevernet.Repositories;
using Heimevernet.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Heimevernet.Mappers;
using Heimevernet.Repositories.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Heimevernet.Models;
using Microsoft.AspNetCore.Authorization;
using Heimevernet.Infrastructure.Storage;
using Amazon.S3;
using Microsoft.Extensions.Options;
using Amazon.Runtime;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    var connectionString = builder.Configuration.GetConnectionString("heimevernetdb")
        ?? throw new InvalidOperationException("Connection string 'heimevernetdb' not found.");
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});

// === Authentication and authorization ===
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // Redirect unauthenticated users to the login page.
        options.LoginPath = "/Account/Login";

        // Redirect authenticated users who do not have permission.
        options.AccessDeniedPath = "/Account/AccessDenied";

        // Limit how long an authentication session remains valid.
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = false;

        // Protect the authentication cookie.
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization(options =>
{
    // Require authentication for all endpoints by default.
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IAuthService, AuthService>();
// ====================================

// === Storage infrastructure ===
builder.Services
    .AddOptions<StorageOptions>()
    .BindConfiguration("Storage")
    .Validate(
        options =>
            !string.IsNullOrWhiteSpace(options.Endpoint) &&
            !string.IsNullOrWhiteSpace(options.AccessKeyId) &&
            !string.IsNullOrWhiteSpace(options.SecretAccessKey) &&
            !string.IsNullOrWhiteSpace(options.Bucket),
        "Storage configuration is incomplete.")
    .ValidateOnStart();

builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var o = sp.GetRequiredService<IOptions<StorageOptions>>().Value;
    return new AmazonS3Client(
        new BasicAWSCredentials(o.AccessKeyId, o.SecretAccessKey),
        new AmazonS3Config
        {
            ServiceURL = o.Endpoint,
            AuthenticationRegion = o.Region,
            ForcePathStyle = true
        });
});
builder.Services.AddSingleton<IFileStorage, S3FileStorage>();
builder.Services.AddScoped<IAttachmentRepository, AttachmentRepository>();
builder.Services.AddScoped<IAttachmentService, AttachmentService>();
// ====================================


builder.Services.AddScoped<IResourceService, ResourceService>();
builder.Services.AddScoped<INeedService, NeedService>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IResourceRepository, ResourceRepository>();
builder.Services.AddScoped<INeedRepository, NeedRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddSingleton<INeedMapper, NeedMapper>();
builder.Services.AddSingleton<IResourceMapper, ResourceMapper>();


builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();

app.MapStaticAssets().AllowAnonymous();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
