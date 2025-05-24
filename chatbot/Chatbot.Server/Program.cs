using Azure.Storage.Blobs;
using Chatbot.Server.Services;
using ChatBot.Server.Services;
using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;
using Microsoft.EntityFrameworkCore;
using Chatbot.Server.Data;
using ChatBot.Server.Data;

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

// ? Add environment variable loading support
var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables(); // ? This ensures launchSettings.json env vars are loaded

// ?? CORS configuration for Blazor client
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins, policy =>
    {
        policy
            .WithOrigins("https://localhost:7178") // Must match Blazor client port
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// ?? Services registration
builder.Services.AddScoped<BlobStorage>();
builder.Services.AddScoped<FaceDetectionService>();
builder.Services.AddHttpClient();
builder.Services.AddControllers();

// ?? Configuration bindings
builder.Services.Configure<EncryptionSettings>(
    builder.Configuration.GetSection("Encryption"));

// ??? Database context setup
builder.Services.AddDbContext<ORCContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("OrcDb")));

builder.Services.AddDbContext<VisionDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("VisionDb")));

// ??? Response compression (optional)
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.NoCompression;
});

// ?? Encryption service
builder.Services.AddSingleton<FileEncryptionService>();

// ?? BlobServiceClient setup (uses connection string from env vars)
builder.Services.AddSingleton<BlobServiceClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connectionString = config.GetConnectionString("AzureBlobStorage")
                            ?? config["AzureBlobStorage:ConnectionString"];

    if (string.IsNullOrEmpty(connectionString))
    {
        throw new Exception("? BlobStorage connection string is null or empty.");
    }

    return new BlobServiceClient(connectionString);
});

// ?? Build the app
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var visionDb = scope.ServiceProvider.GetRequiredService<VisionDbContext>();
    if (visionDb.Database.EnsureCreated())
    {
        Console.WriteLine("? vision.db was (re)created with all necessary tables.");
    }
    else
    {
        Console.WriteLine("?? vision.db already existed; skipped EnsureCreated.");
    }

    var orcDb = scope.ServiceProvider.GetRequiredService<ORCContext>();
    orcDb.Database.EnsureCreated();
}

// ?? HTTPS redirection
app.UseHttpsRedirection();

// ??? Log incoming requests (for debugging)
app.Use(async (context, next) =>
{
    Console.WriteLine($"Request: {context.Request.Method} {context.Request.Path}");
    await next();
});

// ?? Middleware pipeline
app.UseRouting();
app.UseCors(MyAllowSpecificOrigins);
app.UseAuthorization();

// ?? Map API controllers
app.MapControllers();

// ?? Run the app
app.Run();
