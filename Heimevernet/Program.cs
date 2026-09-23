using Heimevernet.Data;
using Heimevernet.Services;
using Heimevernet.Repositories;
using Heimevernet.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Heimevernet.Mappers;
using Heimevernet.Repositories.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    var connectionString = builder.Configuration.GetConnectionString("heimevernetdb")
        ?? throw new InvalidOperationException("Connection string 'heimevernetdb' not found.");
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});
// Add services to the container.
builder.Services.AddScoped<IResourceService, ResourceService>();
builder.Services.AddScoped<INeedService, NeedService>();

builder.Services.AddScoped<IResourceRepository, ResourceRepository>();
builder.Services.AddScoped<INeedRepository, NeedRepository>();


builder.Services.AddSingleton<NeedMapper>();
builder.Services.AddSingleton<ResourceMapper>();


builder.Services.AddControllersWithViews();

var app = builder.Build();

//seed the database with example needs on startup. 


app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
