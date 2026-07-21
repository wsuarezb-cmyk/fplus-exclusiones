using BlazorS7Upload.Authentication;
using BlazorS7Upload.Data;
using BlazorS7Upload.Interfaces;
using BlazorS7Upload.Models;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.WebHost.UseUrls("http://localhost:5051", "https://localhost:444");
}

// En Cloud Run los secretos se montan fuera de /app para no ocultar la DLL publicada.
var appSettingsPath = Environment.GetEnvironmentVariable("AppSettingsPath") ?? "/config/appsettings.json";
builder.Configuration.AddJsonFile(appSettingsPath, optional: true, reloadOnChange: false);

// Add services to the container.
builder.Services.AddResponseCompression();
builder.Services.AddAuthenticationCore();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor().AddCircuitOptions(options =>
{
    options.DetailedErrors = true;
});

// Necesario para leer headers HTTP en Blazor Server (IAP)
builder.Services.AddHttpContextAccessor();

builder.Services.AddBlazorBootstrap();
builder.Services.AddScoped<AuthenticationStateProvider, IapAuthenticationStateProvider>();
builder.Services.AddScoped<IUserRolesService, UserRolesService>();
builder.Services.AddScoped<HelperModel>();
builder.Services.AddScoped<IExclusionesService, ExclusionesService>();
builder.Services.AddHttpClient();

builder.Services.AddSignalR(e => {
    e.MaximumReceiveMessageSize = 10 * 1024 * 1024; // 10MB
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
