var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

var builder = WebApplication.CreateBuilder(args);

// CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins, policy =>
    {
        policy
            .WithOrigins("https://localhost:7178") // ? match client port
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddControllers();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseRouting(); // ? MUST be before CORS
app.UseCors(MyAllowSpecificOrigins); // ? MUST use the named policy
app.UseAuthorization();

app.MapControllers();

app.Run();
