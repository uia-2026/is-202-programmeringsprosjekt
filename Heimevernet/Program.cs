// Entry point for the web app: registers services (database, authentication, storage, email
// and application services), sets up the HTTP pipeline, and maps the MVC routes.
// Registration helpers live in Extensions/. Top-level statements cannot have XML docs.

using Heimevernet.Data;
using Heimevernet.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString("heimevernetdb")
    ?? throw new InvalidOperationException("Connection string 'heimevernetdb' not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services
    .AddAppAuthentication()
    .AddPersistence()
    .AddFileStorage()
    .AddEmail()
    .AddApplicationServices();

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