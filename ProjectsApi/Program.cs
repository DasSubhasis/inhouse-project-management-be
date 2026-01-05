using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SWCAPI.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ================= LOGGING =================
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// ================= SERVICES =================
builder.Services.AddScoped<OtpEmailService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;

        options.JsonSerializerOptions.DictionaryKeyPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();

// ================= DATA PROTECTION =================
// Persist keys to avoid session cookie errors on app restart
var keysPath = Path.Combine(builder.Environment.ContentRootPath, "DataProtectionKeys");
if (!Directory.Exists(keysPath))
{
    Directory.CreateDirectory(keysPath);
}
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath));

// ================= SESSION =================
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ================= CORS =================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ================= SWAGGER =================
builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Inhouse Project Management API",
        Version = "v1"
    });
        opt.MapType<DateOnly>(() => new OpenApiSchema
    {
        Type = "string",
        Format = "date",
        Example = new Microsoft.OpenApi.Any.OpenApiString("2025-12-17")
    });
});

builder.Services.AddDirectoryBrowser();


// ================= BUILD APP =================
var app = builder.Build();

// ================= MIDDLEWARE =================
app.UseSession();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Inhouse Call Management API V1");
    c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
});

app.UseRouting();
app.UseCors("AllowAll");

// ❌ JWT AUTH REMOVED (THIS WAS CAUSING ERROR)
// app.UseAuthentication();

app.UseAuthorization();

// ================= STATIC FILES =================
var staticFilesPath = Path.Combine(app.Environment.ContentRootPath, "Docs");
if (!Directory.Exists(staticFilesPath))
{
    Directory.CreateDirectory(staticFilesPath);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(staticFilesPath),
    RequestPath = "/Docs"
});

app.UseDirectoryBrowser(new DirectoryBrowserOptions
{
    FileProvider = new PhysicalFileProvider(staticFilesPath),
    RequestPath = "/Docs"
});

// ================= CONTROLLERS =================
app.MapControllers();

app.Run();
